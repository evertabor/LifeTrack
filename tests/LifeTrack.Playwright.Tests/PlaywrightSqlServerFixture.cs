using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using LifeTrack.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace LifeTrack.Playwright.Tests;

public sealed class PlaywrightSqlServerFixture : IAsyncLifetime
{
    private const string WebProjectRelativePath = "src/LifeTrack.Web/LifeTrack.Web.csproj";
    private const int MaxProcessOutputCharacters = 4_000;
    private const int MaxReadinessDetailCharacters = 512;
    private const string RedactedConnectionString = "[redacted SQL connection string]";
    private const string RedactedCredential = "[redacted SQL credential]";
    private const string TruncationMarker = "…";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan ReadinessPollInterval = TimeSpan.FromMilliseconds(250);
    private readonly string _databaseName = $"LifeTrackUxTests_{Guid.NewGuid():N}";
    private readonly object _processOutputLock = new();
    private readonly StringBuilder _standardOutput = new();
    private readonly StringBuilder _standardError = new();
    private Process? _webProcess;
    private string? _masterConnectionString;
    private string? _databaseConnectionString;
    private string? _connectionStringToRedact;
    private string? _passwordToRedact;
    private string? _userIdToRedact;
    private Uri? _baseAddress;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _databaseCreated;

    public Uri BaseAddress => _baseAddress
        ?? throw new InvalidOperationException("The Kestrel test host has not been started.");

    public IBrowser Browser => _browser
        ?? throw new InvalidOperationException("The Playwright browser has not been started.");

    public async Task InitializeAsync()
    {
        try
        {
            var configuredConnectionString = GetConfiguredConnectionString();
            var masterConnectionString = BuildLocalMasterConnectionString(configuredConnectionString);
            _masterConnectionString = masterConnectionString;
            var databaseConnectionString = await CreateDatabaseAsync(masterConnectionString);
            _databaseConnectionString = databaseConnectionString;
            try
            {
                await MigrateDatabaseAsync(databaseConnectionString);
            }
            catch (SqlException exception)
            {
                throw new InvalidOperationException(
                    "Unable to apply the existing EF migrations to the disposable SQL Server database. " +
                    "Verify that local SQL Server is available and the configured account has database access.",
                    exception);
            }

            await StartWebHostAsync(databaseConnectionString);
            await StartBrowserAsync();
        }
        catch (Exception initializationFailure)
        {
            try
            {
                await DisposeAsync();
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException(
                    "Playwright test setup failed and cleanup also failed.",
                    initializationFailure,
                    cleanupFailure);
            }

            throw;
        }
    }

    public async Task DisposeAsync()
    {
        var cleanupFailures = new List<Exception>();
        var browser = _browser;
        _browser = null;
        if (browser is not null)
            await CaptureCleanupFailureAsync(async () => await browser.DisposeAsync(), cleanupFailures);

        var playwright = _playwright;
        _playwright = null;
        if (playwright is not null)
            CaptureCleanupFailure(() => playwright.Dispose(), cleanupFailures);

        await CaptureCleanupFailureAsync(StopWebHostAsync, cleanupFailures);
        await CaptureCleanupFailureAsync(DropDatabaseAsync, cleanupFailures);

        if (cleanupFailures.Count > 0)
            throw new AggregateException("One or more Playwright test fixture cleanup operations failed.", cleanupFailures);
    }

    private static string GetConfiguredConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            LocalSqlServerFactAttribute.SqlServerConnectionStringEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        throw new InvalidOperationException(
            $"Set {LocalSqlServerFactAttribute.SqlServerConnectionStringEnvironmentVariable} to a connection string for a local SQL Server. " +
            "The connection must allow creating and dropping databases; Azure and development databases are not supported.");
    }

    private static string BuildLocalMasterConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrWhiteSpace(builder.FailoverPartner))
        {
            throw new InvalidOperationException(
                $"The {LocalSqlServerFactAttribute.SqlServerConnectionStringEnvironmentVariable} connection string cannot set Failover Partner.");
        }

        EnsureLocalSqlServer(builder.DataSource);
        builder.InitialCatalog = "master";
        builder.AttachDBFilename = string.Empty;
        builder.UserInstance = false;
        return builder.ConnectionString;
    }

    private static void EnsureLocalSqlServer(string dataSource)
    {
        var normalizedDataSource = dataSource.Trim();
        if (normalizedDataSource.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
            normalizedDataSource = normalizedDataSource[4..];
        if (normalizedDataSource.StartsWith("np:", StringComparison.OrdinalIgnoreCase))
            normalizedDataSource = normalizedDataSource[3..];

        var host = normalizedDataSource
            .Split([',', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault()
            ?.Trim('[', ']');

        if (host is null || !IsLocalHost(host))
        {
            throw new InvalidOperationException(
                $"The {LocalSqlServerFactAttribute.SqlServerConnectionStringEnvironmentVariable} server must be local. " +
                "Remote and Azure SQL hosts are blocked.");
        }
    }

    private static bool IsLocalHost(string host) =>
        host.Equals(".", StringComparison.OrdinalIgnoreCase)
        || host.Equals("(local)", StringComparison.OrdinalIgnoreCase)
        || host.Equals("(localdb)", StringComparison.OrdinalIgnoreCase)
        || host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("::1", StringComparison.OrdinalIgnoreCase);

    private async Task<string> CreateDatabaseAsync(string masterConnectionString)
    {
        try
        {
            await using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync(CancellationToken.None);
            await using var command = connection.CreateCommand();
            command.CommandText =
                "DECLARE @statement nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@databaseName); " +
                "EXEC sys.sp_executesql @statement;";
            command.Parameters.Add("@databaseName", SqlDbType.NVarChar, 128).Value = _databaseName;
            await command.ExecuteNonQueryAsync(CancellationToken.None);
            _databaseCreated = true;
        }
        catch (SqlException exception)
        {
            throw new InvalidOperationException(
                "Unable to create the disposable SQL Server database. Verify that local SQL Server is running " +
                "and the configured account can create and drop databases.",
                exception);
        }

        var databaseBuilder = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = _databaseName
        };
        return databaseBuilder.ConnectionString;
    }

    private static async Task MigrateDatabaseAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure())
            .Options;
        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.MigrateAsync(CancellationToken.None);
    }

    private async Task StartWebHostAsync(string connectionString)
    {
        var projectPath = FindWebProjectPath();
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException("Could not determine the LifeTrack.Web project directory.");
        var outputDirectory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        var buildConfiguration = Directory.GetParent(outputDirectory)?.Name
            ?? throw new InvalidOperationException("Could not determine the UX test build configuration.");
        var port = ReserveLocalPort();
        var baseAddress = new Uri($"http://127.0.0.1:{port}");
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = projectDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--no-restore");
        startInfo.ArgumentList.Add("--no-launch-profile");
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add(buildConfiguration);
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Testing";
        startInfo.Environment["ASPNETCORE_URLS"] = baseAddress.ToString();
        startInfo.Environment["ConnectionStrings__DefaultConnection"] = connectionString;
        startInfo.Environment.Remove(LocalSqlServerFactAttribute.SqlServerConnectionStringEnvironmentVariable);
        var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
        _connectionStringToRedact = connectionStringBuilder.ConnectionString;
        _passwordToRedact = connectionStringBuilder.Password;
        _userIdToRedact = connectionStringBuilder.UserID;

        var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException("Could not start the LifeTrack.Web Kestrel process.");

        _webProcess = process;
        _connectionStringToRedact = connectionString;
        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
                AppendProcessOutput(_standardOutput, eventArgs.Data);
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
                AppendProcessOutput(_standardError, eventArgs.Data);
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _baseAddress = baseAddress;
        await WaitForHostReadyAsync(baseAddress, process);
    }

    private static string FindWebProjectPath()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var projectPath = Path.Combine(directory.FullName, WebProjectRelativePath);
            if (File.Exists(projectPath))
                return projectPath;
        }

        throw new InvalidOperationException(
            $"Could not find {WebProjectRelativePath}. Run the UX tests from a LifeTrack solution checkout.");
    }

    private static int ReserveLocalPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private async Task WaitForHostReadyAsync(Uri baseAddress, Process process)
    {
        using var timeout = new CancellationTokenSource(StartupTimeout);
        using var client = new HttpClient(new HttpClientHandler { UseProxy = false })
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        var healthUri = new Uri(baseAddress, "/health");
        var lastReadinessDiagnostic = "No readiness response has been received.";

        try
        {
            while (true)
            {
                if (process.HasExited)
                {
                    process.WaitForExit();
                    throw new InvalidOperationException(
                        $"LifeTrack.Web exited with code {process.ExitCode} before Kestrel became ready. " +
                        "Check the local SQL Server configuration and rerun the focused UX test." +
                        Environment.NewLine + $"Last readiness result: {lastReadinessDiagnostic}" +
                        Environment.NewLine + GetProcessOutputDiagnostics());
                }

                var readinessResult = await IsHealthyAsync(client, healthUri, timeout.Token);
                lastReadinessDiagnostic = readinessResult.Diagnostic;
                if (readinessResult.IsHealthy)
                    return;

                await Task.Delay(ReadinessPollInterval, timeout.Token);
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"LifeTrack.Web did not become healthy within {StartupTimeout.TotalSeconds} seconds. " +
                "Check that local SQL Server is available and rerun the focused UX test." +
                Environment.NewLine + $"Last readiness result: {lastReadinessDiagnostic}" +
                Environment.NewLine + GetProcessOutputDiagnostics());
        }
    }

    private async Task<(bool IsHealthy, string Diagnostic)> IsHealthyAsync(
        HttpClient client,
        Uri healthUri,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(
                healthUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (response.IsSuccessStatusCode)
                return (true, $"HTTP {(int)response.StatusCode} {response.StatusCode}.");

            return (false,
                $"HTTP {(int)response.StatusCode} {response.StatusCode}; " +
                "the health endpoint returned a non-success status.");
        }
        catch (HttpRequestException exception)
        {
            return (false, $"HttpRequestException: {SanitizeReadinessDetail(exception.Message)}");
        }
    }

    private string SanitizeReadinessDetail(string detail)
    {
        var safeDetail = ReplaceSensitiveValue(detail, _connectionStringToRedact, RedactedConnectionString);
        safeDetail = ReplaceSensitiveValue(safeDetail, _passwordToRedact, RedactedCredential);
        safeDetail = ReplaceSensitiveValue(safeDetail, _userIdToRedact, RedactedCredential);
        return safeDetail.Length > MaxReadinessDetailCharacters
            ? safeDetail[..(MaxReadinessDetailCharacters - 1)] + TruncationMarker
            : safeDetail;
    }

    private async Task StartBrowserAsync()
    {
        try
        {
            var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _playwright = playwright;
            _browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }
        catch (PlaywrightException exception)
        {
            var playwrightScriptPath = Path.Combine(AppContext.BaseDirectory, "playwright.ps1");
            throw new InvalidOperationException(
                "Playwright could not initialize its driver or launch Chromium. " +
                "If the browser binary is missing, install Chromium with " +
                $"`pwsh \"{playwrightScriptPath}\" install chromium`.",
                exception);
        }
    }

    private async Task StopWebHostAsync()
    {
        var process = _webProcess;
        _webProcess = null;
        if (process is null)
            return;

        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            process.WaitForExit();
        }
        finally
        {
            process.Dispose();
        }
    }

    private void AppendProcessOutput(StringBuilder output, string line)
    {
        var safeLine = ReplaceSensitiveValue(line, _connectionStringToRedact, RedactedConnectionString);
        safeLine = ReplaceSensitiveValue(safeLine, _passwordToRedact, RedactedCredential);
        safeLine = ReplaceSensitiveValue(safeLine, _userIdToRedact, RedactedCredential);

        if (safeLine.Length > MaxProcessOutputCharacters)
            safeLine = safeLine[^MaxProcessOutputCharacters..];

        lock (_processOutputLock)
        {
            output.AppendLine(safeLine);
            if (output.Length > MaxProcessOutputCharacters)
                output.Remove(0, output.Length - MaxProcessOutputCharacters);
        }
    }

    private static string ReplaceSensitiveValue(string output, string? sensitiveValue, string replacement) =>
        string.IsNullOrEmpty(sensitiveValue)
            ? output
            : output.Replace(sensitiveValue, replacement, StringComparison.OrdinalIgnoreCase);

    private static async Task CaptureCleanupFailureAsync(Func<Task> cleanup, List<Exception> cleanupFailures)
    {
        try
        {
            await cleanup();
        }
        catch (Exception exception)
        {
            cleanupFailures.Add(exception);
        }
    }

    private static void CaptureCleanupFailure(Action cleanup, List<Exception> cleanupFailures)
    {
        try
        {
            cleanup();
        }
        catch (Exception exception)
        {
            cleanupFailures.Add(exception);
        }
    }

    private string GetProcessOutputDiagnostics()
    {
        lock (_processOutputLock)
        {
            return $"Kestrel stdout:{Environment.NewLine}{_standardOutput}" +
                $"{Environment.NewLine}Kestrel stderr:{Environment.NewLine}{_standardError}";
        }
    }

    private async Task DropDatabaseAsync()
    {
        if (!_databaseCreated)
            return;

        var databaseConnectionString = _databaseConnectionString
            ?? throw new InvalidOperationException("The disposable SQL Server database connection was not initialized.");
        using var databaseConnection = new SqlConnection(databaseConnectionString);
        SqlConnection.ClearPool(databaseConnection);

        var connectionString = _masterConnectionString
            ?? throw new InvalidOperationException("The SQL Server master connection was not initialized.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "IF DB_ID(@databaseName) IS NOT NULL BEGIN " +
            "DECLARE @alterStatement nvarchar(max) = N'ALTER DATABASE ' + QUOTENAME(@databaseName) + " +
            "N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE'; " +
            "EXEC sys.sp_executesql @alterStatement; " +
            "DECLARE @dropStatement nvarchar(max) = N'DROP DATABASE ' + QUOTENAME(@databaseName); " +
            "EXEC sys.sp_executesql @dropStatement; END";
        command.Parameters.Add("@databaseName", SqlDbType.NVarChar, 128).Value = _databaseName;
        await command.ExecuteNonQueryAsync(CancellationToken.None);
        _databaseCreated = false;
    }
}
