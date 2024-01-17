namespace Reqnroll.SampleProjectGenerator;

public record ProcessStartInfoEx(
    string WorkingDirectory,
    string ExecutablePath,
    string Arguments)
{
    public IReadOnlyDictionary<string, string> EnvironmentVariables => ImmutableDictionary<string, string>.Empty;
    public TimeSpan Timeout => TimeSpan.FromMinutes(5);
}
