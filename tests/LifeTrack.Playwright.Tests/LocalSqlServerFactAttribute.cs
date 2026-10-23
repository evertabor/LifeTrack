namespace LifeTrack.Playwright.Tests;

public sealed class LocalSqlServerFactAttribute : FactAttribute
{
    internal const string SqlServerConnectionStringEnvironmentVariable = "LIFETRACK_UX_TESTS_SQLSERVER_CONNECTION_STRING";

    public LocalSqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(SqlServerConnectionStringEnvironmentVariable)))
        {
            Skip = $"{SqlServerConnectionStringEnvironmentVariable} is unset or blank. " +
                "Set it to a local SQL Server connection string to run this browser smoke test.";
        }
    }
}
