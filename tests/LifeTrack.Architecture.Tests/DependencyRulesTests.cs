using System.Reflection;
using LifeTrack.Application.Common;
using NetArchTest.Rules;

namespace LifeTrack.Architecture.Tests;

public class DependencyRulesTests
{
    private const string Application = "LifeTrack.Application";
    private const string Domain = "LifeTrack.Domain";
    private const string Infrastructure = "LifeTrack.Infrastructure";
    private const string Web = "LifeTrack.Web";

    private static readonly Assembly DomainAssembly = LoadAssembly(Domain);
    private static readonly Assembly ApplicationAssembly = typeof(ApplicationAssemblyAnchor).Assembly;
    private static readonly Assembly InfrastructureAssembly = LoadAssembly(Infrastructure);
    private static readonly Assembly WebAssembly = LoadAssembly(Web);

    private static Assembly LoadAssembly(string assemblyName) =>
        Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, assemblyName + ".dll"));

    private static string ReadProgramSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "LifeTrack.sln")))
        {
            directory = directory.Parent;
        }

        var webProjectPath = directory is null
            ? Path.Combine(AppContext.BaseDirectory, "src", "LifeTrack.Web")
            : Path.Combine(directory.FullName, "src", "LifeTrack.Web");

        var programPath = Path.Combine(webProjectPath, "Program.cs");
        Assert.True(File.Exists(programPath), $"Program.cs not found at {programPath}. Expected a repository root with LifeTrack.sln above {AppContext.BaseDirectory}.");
        return File.ReadAllText(programPath);
    }

    private static void AssertSuccessful(TestResult result)
    {
        var offenders = string.Join(", ", result.FailingTypeNames ?? []);
        Assert.True(result.IsSuccessful, $"Rule violated by: {offenders}");
    }

    [Fact]
    public void Domain_DoesNotDependOnAnyOtherLayer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Application, Infrastructure, Web)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Domain_DoesNotDependOnFrameworks()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "System.Data")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrWeb()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Web, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnWeb()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(Web)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Controllers_TalkToApplication_NotToInfrastructureOrDomain()
    {
        var result = Types.InAssembly(WebAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Domain)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Controllers_DoNotInjectConcreteHandlers()
    {
        var offenders = WebAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .SelectMany(t => t.GetConstructors().SelectMany(c => c.GetParameters()))
            .Where(p => p.ParameterType.Name.EndsWith("Handler", StringComparison.Ordinal)
                || p.ParameterType.Name.EndsWith("Repository", StringComparison.Ordinal)
                || p.ParameterType.Name.EndsWith("DbContext", StringComparison.Ordinal))
            .Select(p => $"{p.Member.DeclaringType?.Name}({p.ParameterType.Name} {p.Name})")
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void ViewModels_DoNotInheritFromDomainEntities()
    {
        var result = Types.InAssembly(WebAssembly)
            .That()
            .HaveNameEndingWith("ViewModel")
            .ShouldNot()
            .HaveDependencyOn(Domain)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Handlers_AreSealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .BeSealed()
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Antiforgery_GlobalFilter_IsRegistered()
    {
        var programSource = ReadProgramSource();

        Assert.Contains("AutoValidateAntiforgeryToken", programSource, StringComparison.Ordinal);
    }

    [Fact]
    public void MediatR_IsRegisteredScanningTheApplicationAssembly()
    {
        var programSource = ReadProgramSource();

        Assert.Contains("AddMediatR", programSource, StringComparison.Ordinal);
        Assert.Contains("typeof(ApplicationAssemblyAnchor).Assembly", programSource, StringComparison.Ordinal);
        Assert.DoesNotContain("typeof(Program).Assembly", programSource, StringComparison.Ordinal);
    }

    [Fact]
    public void FallbackPolicy_IsRequireAuthenticatedUser()
    {
        var programSource = ReadProgramSource();

        Assert.Contains("FallbackPolicy", programSource, StringComparison.Ordinal);
        Assert.Contains("RequireAuthenticatedUser", programSource, StringComparison.Ordinal);
    }
}
