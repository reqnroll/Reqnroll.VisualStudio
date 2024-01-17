#nullable disable

namespace Reqnroll.VisualStudio.Discovery;

public class ParameterMatch
{
    public static readonly ParameterMatch NotMatch = new(Array.Empty<MatchedStepTextParameter>(), null,
        Array.Empty<string>());

    public ParameterMatch(MatchedStepTextParameter[] stepTextParameters, StepArgument stepArgument,
        string[] parameterTypes, string error = null)
    {
        StepTextParameters = stepTextParameters ?? throw new ArgumentNullException(nameof(stepTextParameters));
        StepArgument = stepArgument;
        ParameterTypes = parameterTypes ?? throw new ArgumentNullException(nameof(parameterTypes));
        Error = error;
    }

    public MatchedStepTextParameter[] StepTextParameters { get; }
    public StepArgument StepArgument { get; }
    public string[] ParameterTypes { get; }
    public string Error { get; }

    public bool HasError => Error != null;
    public bool MatchedDataTable => StepArgument is DataTable;
    public string DataTableParameterType => MatchedDataTable ? ParameterTypes.LastOrDefault() : null;
    public bool MatchedDocString => StepArgument is DocString;
    public string DocStringParameterType => MatchedDocString ? ParameterTypes.LastOrDefault() : null;
}
