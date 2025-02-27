namespace Reqnroll.VisualStudio.Tests.Discovery;

[Collection(NonParallelTestCollectionDefinition.Name)]
public class ProjectBindingRegistryCacheTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    private volatile int _updateTaskCount;

    public ProjectBindingRegistryCacheTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task CannotRevertToAnOlderVersionOfBindingRegistry()
    {
        //arrange
        var ideScope = new StubIdeScope(_testOutputHelper);
        var cache = new ProjectBindingRegistryCache(ideScope);
        var olderRegistry = ProjectBindingRegistry.FromBindings(Array.Empty<ProjectStepDefinitionBinding>());
        var newerRegistry = ProjectBindingRegistry.FromBindings(Array.Empty<ProjectStepDefinitionBinding>());

        //act
        await cache.Update(_ => newerRegistry);

        Func<Task> secondUpdateWithOlderRegistry = () => cache.Update(_ => olderRegistry);

        //assert
        await secondUpdateWithOlderRegistry.Should().ThrowAsync<InvalidOperationException>();
        cache.Value.Should().BeSameAs(newerRegistry);
    }

    [Fact]
    public async Task DoNotRevertToAnInvalidBindingRegistry()
    {
        //arrange
        var ideScope = new StubIdeScope(_testOutputHelper);
        var cache = new ProjectBindingRegistryCache(ideScope);
        var existingRegistry = ProjectBindingRegistry.FromBindings(Array.Empty<ProjectStepDefinitionBinding>());
        var invalidRegistry = ProjectBindingRegistry.Invalid;

        //act
        await cache.Update(_ => existingRegistry);
        await cache.Update(_ => invalidRegistry);

        //assert
        cache.Value.Should().BeSameAs(existingRegistry);
        ideScope.StubLogger.Messages.Should().Contain("Got an invalid registry, ignoring...");
    }

    [Fact]
    public async Task UpdatesBindingRegistry()
    {
        //arrange
        var ideScope = new StubIdeScope(_testOutputHelper);
        var cache = new ProjectBindingRegistryCache(ideScope);
        var bindingRegistry = ProjectBindingRegistry.FromBindings(new ProjectStepDefinitionBinding[]
        {
            new TestProjectStepDefinitionBinding(),
            new TestProjectStepDefinitionBinding("Error")
        });

        //act
        await cache.Update(_ => bindingRegistry);

        //assert
        cache.Value.Should().BeSameAs(bindingRegistry);
    }

    [Fact]
    public void ParallelUpdate()
    {
        //arrange
        var start = DateTimeOffset.UtcNow;
        var ideScope = new Mock<IIdeScope>(MockBehavior.Strict);
        var stubLogger = new StubLogger();
        var logger = new DeveroomCompositeLogger();
        logger.Add(new DeveroomXUnitLogger(_testOutputHelper));
        logger.Add(stubLogger);

        ideScope.SetupGet(s => s.Logger).Returns(logger);
        ideScope.Setup(s => s.CalculateSourceLocationTrackingPositions(It.IsAny<IEnumerable<SourceLocation>>()));

        var projectBindingRegistryCache = new ProjectBindingRegistryCache(ideScope.Object);

        var oldVersions = new ConcurrentQueue<int>();
        var initialRegistry = new ProjectBindingRegistry(Array.Empty<ProjectStepDefinitionBinding>(), Array.Empty<ProjectHookBinding>(), 123456);

        var timeout = TimeSpan.FromSeconds(20);
        using var cts = new CancellationTokenSource(timeout);
        int i = 0;
        var taskCreationOptions = TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach |
                                  TaskCreationOptions.HideScheduler;

        WarmUpThreads(logger, timeout);

        //act
        try
        {
            Task.Factory.StartNew(async () => { await GetLatestInvoker(cts, projectBindingRegistryCache); }, cts.Token,
                taskCreationOptions, TaskScheduler.Default);

            for (i = 0; i < 1000 && !cts.IsCancellationRequested; ++i)
            {
                Task.Factory.StartNew(
                    async () => { await InvokeUpdate(cts, projectBindingRegistryCache, oldVersions, stubLogger); },
                    cts.Token, taskCreationOptions, TaskScheduler.Default);
#pragma warning disable VSTHRD002
                Task.Delay(i / 10, cts.Token).Wait(cts.Token);
#pragma warning restore
            }
        }
        catch (OperationCanceledException e)
        {
            _testOutputHelper.WriteLine(e.ToString());
        }

        //assert
        var elapsed = DateTimeOffset.UtcNow - start;
        _testOutputHelper.WriteLine($"i:{i} cancelled:{cts.IsCancellationRequested} elapsed:{elapsed}");
#pragma warning disable VSTHRD002
        var cachedRegistry = projectBindingRegistryCache.Value;
        var registry = projectBindingRegistryCache.GetLatest().Result;
#pragma warning restore
        registry.Version.Should()
            .BeGreaterOrEqualTo(cachedRegistry.Version, "cached value is modified at the end of the update");
        registry.Version.Should().BeGreaterOrEqualTo(initialRegistry.Version + _updateTaskCount);

        oldVersions.Count.Should().BeGreaterOrEqualTo(_updateTaskCount);
        oldVersions.Should().BeInAscendingOrder();
    }

    private static void WarmUpThreads(DeveroomCompositeLogger logger, TimeSpan timeout)
    {
        logger.LogInfo("Warming up threads");

        var w = new CountdownEvent(100);
        for (int i = 0; i < w.InitialCount; ++i)
            new Thread(_ => { w.Signal(); }).Start();

        w.Wait(timeout).Should()
            .BeTrue(
                $"Warmup has to be completed while there are {w.CurrentCount} threads left.");
        logger.LogInfo("Warmup done");
    }

    private static async Task GetLatestInvoker(CancellationTokenSource cts,
        ProjectBindingRegistryCache projectBindingRegistryCache)
    {
        var priorVer = 0;
        while (!cts.IsCancellationRequested)
        {
            var bindingRegistry = await projectBindingRegistryCache.GetLatest();
            priorVer.Should().BeLessOrEqualTo(bindingRegistry.Version);
            priorVer = bindingRegistry.Version;
            await Task.Delay(10, cts.Token);
        }
    }

    private async Task InvokeUpdate(CancellationTokenSource cts,
        ProjectBindingRegistryCache projectBindingRegistryCache,
        ConcurrentQueue<int> oldVersions, StubLogger stubLogger)
    {
        while (!cts.IsCancellationRequested)
        {
            await projectBindingRegistryCache.Update(async old =>
            {
                await Task.Yield();
                oldVersions.Enqueue(old.Version);
                return new ProjectBindingRegistry(Array.Empty<ProjectStepDefinitionBinding>(), Array.Empty<ProjectHookBinding>(),
                    Guid.NewGuid().GetHashCode());
            });

            Interlocked.Increment(ref _updateTaskCount);

            await Task.Yield();

            if (stubLogger.Logs.Any(log => log.Message.Contains("in 2 iteration")))
                await cts.CancelAsync();
        }
    }

    private class TestProjectStepDefinitionBinding : ProjectStepDefinitionBinding
    {
        public TestProjectStepDefinitionBinding()
            : base(ScenarioBlock.Given, new Regex(string.Empty), new Scope(),
                new ProjectBindingImplementation("M1", Array.Empty<string>(), new SourceLocation("file", 0, 0)))
        {
        }

        public TestProjectStepDefinitionBinding(string error)
            : base(ScenarioBlock.Given, new Regex(string.Empty), new Scope(),
                new ProjectBindingImplementation("M1", Array.Empty<string>(), new SourceLocation("file", 0, 0)),
                "", error)
        {
        }
    }
}
