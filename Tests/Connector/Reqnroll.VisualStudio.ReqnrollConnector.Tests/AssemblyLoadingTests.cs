using ReqnrollConnector.AssemblyLoading;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests;

public class AssemblyLoadingTests
{
    private readonly List<Assembly> _assemblies =
        AssembliesInDir("..\\..\\..\\..\\..\\Reqnroll.VisualStudio.Specs\\bin", "Spec*.dll")
            .Union(AssembliesInDir(".", "Spec*.dll"))
            .Union(new[] {typeof(AssemblyLoadingTests).Assembly})
            .ToList();

    private readonly ITestOutputHelper _testOutputHelper;

    public AssemblyLoadingTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private static IEnumerable<Assembly> AssembliesInDir(string path, string searchPattern) =>
        Directory
            .EnumerateFiles(path, searchPattern, SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .Select(TryLoad)
            .Where(a => a is Some<Assembly>)
            .SelectOptional(a => a);

    private static Option<Assembly> TryLoad(string path)
    {
        try
        {
            return Assembly.LoadFrom(path);
        }
        catch (Exception)
        {
            return None.Value;
        }
    }

    [Fact]
    public void Load_Assemblies()
    {
        //act
        var log = new TestOutputHelperLogger(_testOutputHelper);
        var loadContexts = _assemblies.Select(assembly => new TestAssemblyLoadContext(
                assembly.Location,
                (assemblyLoadContext, path) => assemblyLoadContext.LoadFromAssemblyPath(path),
                log))
            .ToList();

        var loadedAssemblies = loadContexts
            .SelectMany(lc => _assemblies.Select(a => lc.LoadFromAssemblyName(a.GetName())))
            .ToList();

        //assert
        loadedAssemblies.Should().HaveCountGreaterThan(1);
        loadContexts.Select(lc => lc.TestAssembly.Location).Should()
            .BeEquivalentTo(_assemblies.Select(a => a.Location), "same dlls are loaded");
        loadContexts.Select(lc => lc.TestAssembly).Should()
            .NotBeEquivalentTo(_assemblies, "assemblies re loaded into different context");
    }

    [Fact]
    public void Loads_From_AspNetCoreAssembly()
    {
        //arrange
        var log = new TestOutputHelperLogger(_testOutputHelper);

        var loadContext = new TestAssemblyLoadContext(
            GetType().Assembly.Location,
            (assemblyLoadContext, path) => assemblyLoadContext.LoadFromAssemblyPath(path),
            log);

        //act
        var loadedAssembly = loadContext.LoadFromAssemblyName(new AssemblyName("Microsoft.AspNetCore.Antiforgery")
            {Version = new Version(6, 0)});

        //assert
        loadedAssembly.GetName().Name.Should().Be("Microsoft.AspNetCore.Antiforgery");
    }
}
