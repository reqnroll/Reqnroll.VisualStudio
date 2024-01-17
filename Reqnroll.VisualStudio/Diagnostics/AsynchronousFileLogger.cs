namespace Reqnroll.VisualStudio.Diagnostics;

public class AsynchronousFileLogger : IDeveroomLogger, IDisposable
{
    private readonly Channel<LogMessage> _channel;
    private readonly IFileSystem _fileSystem;
    private readonly CancellationTokenSource _stopTokenSource;

    protected AsynchronousFileLogger(IFileSystem fileSystem, TraceLevel level)
    {
        _fileSystem = fileSystem;
        Level = level;
        _channel = Channel.CreateBounded<LogMessage>(100);
        _stopTokenSource = new CancellationTokenSource();
        LogFilePath = GetLogFile();
    }

    public string LogFilePath { get; private set; }
    public TraceLevel Level { get; }

    public virtual void Log(LogMessage message)
    {
        if (message.Level > Level) return;
        _channel.Writer.TryWrite(message);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal static string GetLogFile()
    {
        return Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData), "Reqnroll",
#if DEBUG
            $"reqnroll-vs-debug-{DateTime.UtcNow:yyyyMMdd}.log");
#else
            $"reqnroll-vs-{DateTime.Now:yyyyMMdd}.log");
#endif
    }

    public static AsynchronousFileLogger CreateInstance(IFileSystem fileSystem)
    {
        var fileLogger = new AsynchronousFileLogger(fileSystem, TraceLevel.Verbose);
        Task.Factory.StartNew(
            fileLogger.Start,
            fileLogger._stopTokenSource.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        return fileLogger;
    }

    private Task Start()
    {
        EnsureLogFolder();
        DeleteOldLogFiles();
        return WorkerLoop();
    }

    private async Task WorkerLoop()
    {
        while (!_stopTokenSource.IsCancellationRequested)
            try
            {
                var message = await _channel.Reader.ReadAsync(_stopTokenSource.Token);
                WriteLogMessage(message);
            }
            catch (Exception ex) when (ex is not (ChannelClosedException or TaskCanceledException))
            {
                Debug.WriteLine(ex, $"Error writing to the {LogFilePath}");
            }
            catch
            {
                // ignored
            }
    }

    protected void WriteLogMessage(LogMessage message)
    {
        var content =
            $"{message.TimeStamp:yyyy-MM-ddTHH\\:mm\\:ss.fffzzz}, {message.Level}@{message.ManagedThreadId}, {message.CallerMethod}: {message.Message}";
        if (message.Exception != null) content += $" : {message.Exception}";
        content += Environment.NewLine;

        _fileSystem.File.AppendAllText(LogFilePath, content, Encoding.UTF8);
    }

    protected void EnsureLogFolder()
    {
        LogFilePath = Path.GetFullPath(LogFilePath);
        var logFolder = Path.GetDirectoryName(LogFilePath);
        if (!_fileSystem.Directory.Exists(logFolder))
            _fileSystem.Directory.CreateDirectory(logFolder);
    }

    private void DeleteOldLogFiles()
    {
        try
        {
            var logFolder = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(logFolder))
                return;

            var logFiles = Directory.GetFiles(logFolder, "reqnroll-vs-*.log");

            foreach (string logFile in logFiles)
            {
                FileInfo fi = new FileInfo(logFile);
                if (fi.LastWriteTime < DateTime.UtcNow.AddDays(-10))
                    fi.Delete();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, "Error deleting log files");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        _channel.Writer.TryComplete();
        _stopTokenSource.Cancel(true);
        _stopTokenSource.Dispose();
    }
}
