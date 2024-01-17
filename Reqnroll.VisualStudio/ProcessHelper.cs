#nullable disable

namespace Reqnroll.VisualStudio;

public static class ProcessHelper
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

    public static RunProcessResult RunProcess(string workingDirectory, string executablePath,
        IEnumerable<string> arguments, TimeSpan? timeout = null, bool throwException = false, Encoding encoding = null)
    {
        var parameters = string.Join(" ", arguments.Select(GetSafeArgument));
        try
        {
            return RunProcessInternal(workingDirectory, executablePath, parameters, timeout, encoding);
        }
        catch (Exception ex)
        {
            if (throwException)
                throw;

            return new RunProcessResult(-1, "", ex.Message, executablePath, parameters, workingDirectory);
        }
    }

    private static RunProcessResult RunProcessInternal(string workingDirectory, string executablePath,
        string parameters, TimeSpan? timeout = null, Encoding encoding = null)
    {
        timeout = timeout ?? DefaultTimeout;

        if (workingDirectory == null || !Directory.Exists(workingDirectory))
            throw new DeveroomConfigurationException($"Unable to find directory: {workingDirectory}");

        if (executablePath == null || !File.Exists(executablePath))
            throw new DeveroomConfigurationException($"Unable to find process: {executablePath}");

        ProcessStartInfo psi = new ProcessStartInfo(executablePath, parameters)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = workingDirectory
        };

        if (encoding != null)
        {
            psi.StandardOutputEncoding = encoding;
            psi.StandardErrorEncoding = encoding;
        }

        var process = new Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true
        };

        var consoleOutBuilder = new StringBuilder();
        var consoleErrorBuilder = new StringBuilder();

        using (var outputCollector = new ProcessOutputCollector(process, consoleOutBuilder, consoleErrorBuilder))
        {
            if (!process.Start())
                throw new InvalidOperationException("Could not start process");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var timeOutInMilliseconds = Convert.ToInt32(timeout.Value.TotalMilliseconds);
            if (!process.WaitForExit(timeOutInMilliseconds) ||
                !outputCollector.OutputWaitHandle.WaitOne(timeOutInMilliseconds) ||
                !outputCollector.ErrorWaitHandle.WaitOne(timeOutInMilliseconds))
                throw new TimeoutException(
                    $"Process {psi.FileName} {psi.Arguments} took longer than {timeout.Value.TotalMinutes} min to complete");
        }

        return new RunProcessResult(process.ExitCode, consoleOutBuilder.ToString(), consoleErrorBuilder.ToString(),
            psi.FileName, psi.Arguments, psi.WorkingDirectory);
    }

    private static string GetSafeArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return "\"\"";

        if (!arg.Contains(' ') || arg.StartsWith("\""))
            return arg;

        //source: https://stackoverflow.com/a/12364234/26530
        string value = Regex.Replace(arg, @"(\\*)" + "\"", @"$1\$0");
        value = Regex.Replace(value, @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"", RegexOptions.Singleline);
        return value;
    }

    public class RunProcessResult
    {
        public RunProcessResult(int exitCode, string standardOut, string standardError, string executablePath,
            string arguments, string workingDirectory)
        {
            ExitCode = exitCode;
            StandardOut = standardOut ?? "";
            StandardError = standardError;
            ExecutablePath = executablePath;
            Arguments = arguments;
            WorkingDirectory = workingDirectory;
        }

        public int ExitCode { get; }
        public string StandardOut { get; }
        public string StandardError { get; }
        public string ExecutablePath { get; }
        public string Arguments { get; }
        public string WorkingDirectory { get; }

        public bool HasErrors => !string.IsNullOrWhiteSpace(StandardError);

        public string CommandLine =>
            $"{WorkingDirectory}> {ExecutablePath} {Arguments}";
    }

    private class ProcessOutputCollector : IDisposable
    {
        private readonly Process _process;

        public ProcessOutputCollector(Process process, StringBuilder consoleOutBuilder,
            StringBuilder consoleErrorBuilder)
        {
            _process = process;
            ConsoleOutBuilder = consoleOutBuilder;
            ConsoleErrorBuilder = consoleErrorBuilder;
            OutputWaitHandle = new AutoResetEvent(false);
            ErrorWaitHandle = new AutoResetEvent(false);

            process.OutputDataReceived += ProcessOnOutputDataReceived;
            process.ErrorDataReceived += ProcessOnErrorDataReceived;
        }

        public AutoResetEvent OutputWaitHandle { get; }
        public AutoResetEvent ErrorWaitHandle { get; }
        public StringBuilder ConsoleOutBuilder { get; }
        public StringBuilder ConsoleErrorBuilder { get; }

        public void Dispose()
        {
            _process.OutputDataReceived -= ProcessOnOutputDataReceived;
            _process.ErrorDataReceived -= ProcessOnErrorDataReceived;

            OutputWaitHandle.Dispose();
            ErrorWaitHandle.Dispose();

            if (!_process.HasExited)
                KillProcess();
        }

        private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                OutputWaitHandle.Set();
            else
                ConsoleOutBuilder.AppendLine(e.Data);
        }

        private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                // ReSharper disable once AccessToDisposedClosure
                ErrorWaitHandle.Set();
            else
                ConsoleErrorBuilder.AppendLine(e.Data);
        }

        private void KillProcess()
        {
            try
            {
                _process.Kill();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
