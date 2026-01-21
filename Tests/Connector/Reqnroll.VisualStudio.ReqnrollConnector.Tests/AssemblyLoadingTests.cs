using ReqnrollConnector.AssemblyLoading;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests;

public class AssemblyLoadingTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public AssemblyLoadingTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
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
            {Version = new Version(8, 0)});

        //assert
        loadedAssembly.GetName().Name.Should().Be("Microsoft.AspNetCore.Antiforgery");
    }
}
