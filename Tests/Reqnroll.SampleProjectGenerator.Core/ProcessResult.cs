namespace Reqnroll.SampleProjectGenerator;

public record ProcessResult(
    int ExitCode,
    string StdOutput,
    string StdError,
    TimeSpan ExecutionTime);
