namespace Reqnroll.VisualStudio.Tests.Diagnostics;

public class LoggingTests
{
    [Theory]
    [InlineData(TraceLevel.Verbose)]
    [InlineData(TraceLevel.Info)]
    [InlineData(TraceLevel.Warning)]
    [InlineData(TraceLevel.Error)]
    public void MessageIsLogged(TraceLevel logLevel)
    {
        //arrange
        var fileSystem = new MockFileSystem();
        var logger = AsynchronousFileLogger.CreateInstance(fileSystem);
        Warmup(logger, fileSystem);

        //act
        var message = new LogMessage(logLevel, "msg", nameof(MessageIsLogged));
        logger.Log(message);

        var sw = Stopwatch.StartNew();
        while (!fileSystem.File.ReadAllText(logger.LogFilePath).Contains("msg") &&
               sw.Elapsed < TimeSpan.FromMilliseconds(10))
            Thread.Sleep(10);

        //assert
        fileSystem.File.Exists(logger.LogFilePath).Should().BeTrue("the log should be written quickly");
        fileSystem.File.ReadAllText(logger.LogFilePath).Should().Contain(
            $"{message.TimeStamp:yyyy-MM-ddTHH\\:mm\\:ss.fffzzz}, {message.Level}@{message.ManagedThreadId}, {message.CallerMethod}: {message.Message}");
    }

    private void Warmup(AsynchronousFileLogger logger, IFileSystem fileSystem)
    {
        var message = new LogMessage(TraceLevel.Error, "warmup", nameof(MessageIsLogged));
        logger.Log(message);

        var sw = Stopwatch.StartNew();
        while (!fileSystem.File.Exists(logger.LogFilePath) && sw.Elapsed < TimeSpan.FromMilliseconds(1000))
            Thread.Sleep(1);
    }

    [Fact]
    public void DisposeStopsLogging()
    {
        //arrange
        var fileSystem = new MockFileSystem();
        var logger = AsynchronousFileLogger.CreateInstance(fileSystem);
        Warmup(logger, fileSystem);

        //act
        logger.Dispose();
        var message = new LogMessage(TraceLevel.Error, "msg", nameof(DisposeStopsLogging));
        logger.Log(message);

        Thread.Sleep(100);

        //assert
        fileSystem.File.ReadAllText(logger.LogFilePath).Should().NotContain("msg", "disposed before logging");
    }
}
