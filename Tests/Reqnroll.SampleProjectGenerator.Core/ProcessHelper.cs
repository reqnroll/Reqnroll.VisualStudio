namespace Reqnroll.SampleProjectGenerator;

public class ProcessHelper
{
    public ProcessResult RunProcess(ProcessStartInfoEx psiEx)
    {
        var psi = CreateProcessStartInfo(psiEx);

        using var process = new Process {StartInfo = psi};
        var result = Execute(process, psiEx.Timeout);
        return result;
    }

    private ProcessStartInfo CreateProcessStartInfo(ProcessStartInfoEx psiEx)
    {
        var processStartInfo = new ProcessStartInfo(psiEx.ExecutablePath, psiEx.Arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = psiEx.WorkingDirectory
        };

        foreach (var env in psiEx.EnvironmentVariables) processStartInfo.Environment.Add(env.Key, env.Value);

        return processStartInfo;
    }

    private ProcessResult Execute(Process process, TimeSpan timeout)
    {
        int timeOutInMilliseconds = Convert.ToInt32(timeout.TotalMilliseconds);
        var stdError = new StringBuilder();
        var stdOutput = new StringBuilder();
        var outputWaiter = new CountdownEvent(2);
        process.ErrorDataReceived += (_, e) => AppendDataReceived(stdError, e.Data);
        process.OutputDataReceived += (_, e) => AppendDataReceived(stdOutput, e.Data);
        var sw = Stopwatch.StartNew();

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        bool processResult = process.WaitForExit(timeOutInMilliseconds);

        if (!processResult)
            process.Kill();

        var waitForOutputs = timeout - sw.Elapsed;
        if (waitForOutputs <= TimeSpan.Zero || waitForOutputs > TimeSpan.FromMinutes(1))
            waitForOutputs = TimeSpan.FromMinutes(1);
        outputWaiter.Wait(waitForOutputs);

        sw.Stop();

        return new ProcessResult(
            process.ExitCode,
            stdOutput.ToString(),
            stdError.ToString(),
            sw.Elapsed);

        void AppendDataReceived(StringBuilder builder, string? data)
        {
            if (data is not null) //null is a sign to the end of the output
                builder.AppendLine(data);
            else
                outputWaiter.Signal();
        }
    }

    public ProcessResult RunProcess(ProcessStartInfoEx psiEx, Action<string> consoleWriteLine)
    {
        var result = RunProcess(psiEx);
        consoleWriteLine($"stdOut:{result.StdOutput}");
        consoleWriteLine($"stdErr:{result.StdError}");
        consoleWriteLine($"code:{result.ExitCode}");
        consoleWriteLine($"time:{result.ExecutionTime}");
        return result;
    }
}
