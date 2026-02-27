using Reqnroll.VisualStudio.Connectors;

namespace Reqnroll.VisualStudio.Tests.ReqnrollExtensionServices;

public class ReqnrollExtensionServicesManagerTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ReqnrollExtensionServicesManagerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private (ReqnrollExtensionServicesManager Manager, DiscoveryServiceProxy Proxy, StubDiscoveryResultProvider FallbackProvider, InMemoryStubProjectScope ProjectScope)
        ArrangeSut(TimeSpan? registrationTimeout = null)
    {
        var projectScope = new InMemoryStubProjectScope(_testOutputHelper);
        // Use the default StubIdeScope behavior that stores background tasks for later,
        // rather than executing synchronously, to avoid blocking on ControlPlaneServer.StartAsync.

        InMemoryStubProjectBuilder.CreateOutputAssembly(projectScope);

        var fallbackProvider = new StubDiscoveryResultProvider();
        var controlPlane = new ControlPlaneServer(projectScope.IdeScope.Logger);
        var timeout = registrationTimeout ?? TimeSpan.FromMilliseconds(50);

        var manager = new ReqnrollExtensionServicesManager(projectScope, controlPlane, timeout);
        var proxy = new DiscoveryServiceProxy(manager, fallbackProvider);
        return (manager, proxy, fallbackProvider, projectScope);
    }

    private ProjectSettings GetProjectSettings(InMemoryStubProjectScope projectScope) =>
        projectScope.StubProjectSettingsProvider.GetProjectSettings();

    // --- Shutdown disposal cascade tests ---

    [Fact]
    public void DiscoveryService_Dispose_DisposesConnectorServiceManager()
    {
        //arrange
        var projectScope = new InMemoryStubProjectScope(_testOutputHelper);
        InMemoryStubProjectBuilder.CreateOutputAssembly(projectScope);
        var fallbackProvider = new StubDiscoveryResultProvider();
        var controlPlane = new ControlPlaneServer(projectScope.IdeScope.Logger);
        var manager = new ReqnrollExtensionServicesManager(projectScope, controlPlane, TimeSpan.FromMilliseconds(50));
        var proxy = new DiscoveryServiceProxy(manager, fallbackProvider);
        var bindingRegistryCache = new ProjectBindingRegistryCache(projectScope.IdeScope);
        var discoveryService = new DiscoveryService(projectScope, proxy, bindingRegistryCache);

        //act
        discoveryService.Dispose();

        //assert — Dispose should not throw (SendShutdown is safe when service not connected)
        // and a second Dispose on the manager should be safe (idempotent)
        var act = () => manager.Dispose();
        act.Should().NotThrow();
    }

    // --- Reload event wiring tests ---

    [Fact]
    public void ProjectOutputsUpdated_DoesNotThrow_WhenServiceNotConnected()
    {
        //arrange
        var (manager, _, _, projectScope) = ArrangeSut();

        //act & assert — firing the event when service is not connected should be safe
        using (manager)
        {
            var act = () => projectScope.StubIdeScope.TriggerProjectsBuilt();
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void SettingsInitialized_DoesNotThrow_WhenServiceNotConnected()
    {
        //arrange
        var (manager, _, _, projectScope) = ArrangeSut();

        //act & assert — firing the event when service is not connected should be safe
        using (manager)
        {
            var act = () => projectScope.StubProjectSettingsProvider.InvokeWeakSettingsInitializedEvent();
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        //arrange
        var (manager, _, _, projectScope) = ArrangeSut();

        //act
        manager.Dispose();

        //assert — events should not reach the disposed manager
        var act = () =>
        {
            projectScope.StubIdeScope.TriggerProjectsBuilt();
            projectScope.StubProjectSettingsProvider.InvokeWeakSettingsInitializedEvent();
        };
        act.Should().NotThrow();
    }

    // --- Error list reporting tests ---

    [Fact]
    public void RunDiscovery_FallsBackToClassicProvider_WhenServiceNotRunning()
    {
        //arrange
        var (manager, proxy, fallbackProvider, projectScope) = ArrangeSut();
        var expectedResult = new DiscoveryResult
        {
            StepDefinitions = new StepDefinition[]
            {
                new() { Method = "WhenISomething", Regex = "I something" }
            }
        };
        fallbackProvider.DiscoveryResult = expectedResult;
        var settings = GetProjectSettings(projectScope);

        //act
        using (manager)
        {
            var result = proxy.RunDiscovery(
                settings.OutputAssemblyPath,
                settings.ReqnrollConfigFilePath,
                settings);

            //assert
            result.Should().BeSameAs(expectedResult);
        }
    }

    [Fact]
    public void RunDiscovery_FallsBackToClassicProvider_WhenConnectorDllNotFound()
    {
        //arrange
        var (manager, proxy, fallbackProvider, projectScope) = ArrangeSut();
        var expectedResult = new DiscoveryResult
        {
            StepDefinitions = Array.Empty<StepDefinition>(),
            Hooks = Array.Empty<Hook>()
        };
        fallbackProvider.DiscoveryResult = expectedResult;
        var settings = GetProjectSettings(projectScope);

        //act
        using (manager)
        {
            var result = proxy.RunDiscovery(
                settings.OutputAssemblyPath,
                settings.ReqnrollConfigFilePath,
                settings);

            //assert — connector DLL doesn't exist, service never starts, fallback used
            result.Should().BeSameAs(expectedResult);
            projectScope.StubIdeScope.StubLogger.Messages.Should()
                .Contain(m => m.Contains("Connector DLL not found") ||
                              m.Contains("did not register within timeout"));
        }
    }

    [Fact]
    public void RunDiscovery_MultipleCalls_FallBackConsistently()
    {
        //arrange
        var (manager, proxy, fallbackProvider, projectScope) = ArrangeSut();
        var settings = GetProjectSettings(projectScope);

        //act
        using (manager)
        {
            var result1 = proxy.RunDiscovery(
                settings.OutputAssemblyPath, settings.ReqnrollConfigFilePath, settings);
            var result2 = proxy.RunDiscovery(
                settings.OutputAssemblyPath, settings.ReqnrollConfigFilePath, settings);

            //assert
            result1.Should().BeSameAs(fallbackProvider.DiscoveryResult);
            result2.Should().BeSameAs(fallbackProvider.DiscoveryResult);
        }
    }

    [Fact]
    public void Dispose_DoesNotThrow_WhenServiceNeverStarted()
    {
        //arrange
        var (manager, _, _, _) = ArrangeSut();

        //act & assert
        var act = () => manager.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        //arrange
        var (manager, _, _, _) = ArrangeSut();

        //act & assert
        var act = () =>
        {
            manager.Dispose();
            manager.Dispose();
        };
        act.Should().NotThrow();
    }

    // --- TFM-to-connector-DLL resolution tests ---

    [Theory]
    [InlineData("net6.0", @"Reqnroll-Generic-net6.0\reqnroll-vs.dll")]
    [InlineData("net7.0", @"Reqnroll-Generic-net7.0\reqnroll-vs.dll")]
    [InlineData("net8.0", @"Reqnroll-Generic-net8.0\reqnroll-vs.dll")]
    [InlineData("net9.0", @"Reqnroll-Generic-net9.0\reqnroll-vs.dll")]
    [InlineData("net10.0", @"Reqnroll-Generic-net10.0\reqnroll-vs.dll")]
    public void GetConnectorDll_ReturnsCorrectPath_ForNetCoreTfm(string tfmShortName, string expectedDll)
    {
        //arrange
        var tfm = TargetFrameworkMoniker.CreateFromShortName(tfmShortName);
        var settings = new ProjectSettings(
            DeveroomProjectKind.ReqnrollTestProject,
            tfm,
            tfm.Value,
            ProjectPlatformTarget.AnyCpu,
            @"C:\test\out.dll",
            "TestProject",
            new NuGetVersion("2.3.2", "2.3.2"),
            string.Empty,
            string.Empty,
            ReqnrollProjectTraits.CucumberExpression,
            ProjectProgrammingLanguage.CSharp);

        //act
        var result = ReqnrollExtensionServicesManager.GetConnectorDll(settings);

        //assert
        result.Should().Be(expectedDll);
    }

    [Fact]
    public void GetConnectorDll_DefaultsToNet80_ForFrameworkTfm()
    {
        //arrange
        var tfm = TargetFrameworkMoniker.CreateFromShortName("net48");
        var settings = new ProjectSettings(
            DeveroomProjectKind.ReqnrollTestProject,
            tfm,
            tfm.Value,
            ProjectPlatformTarget.AnyCpu,
            @"C:\test\out.dll",
            "TestProject",
            new NuGetVersion("2.3.2", "2.3.2"),
            string.Empty,
            string.Empty,
            ReqnrollProjectTraits.CucumberExpression,
            ProjectProgrammingLanguage.CSharp);

        //act
        var result = ReqnrollExtensionServicesManager.GetConnectorDll(settings);

        //assert
        result.Should().Be(@"Reqnroll-Generic-net8.0\reqnroll-vs.dll");
    }
}
