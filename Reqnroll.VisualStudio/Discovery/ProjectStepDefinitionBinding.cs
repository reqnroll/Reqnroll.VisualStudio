#nullable disable

namespace Reqnroll.VisualStudio.Discovery;

public class ProjectStepDefinitionBinding : ProjectBinding
{
    public ProjectStepDefinitionBinding(ScenarioBlock stepDefinitionType, Regex regex, Scope scope,
        ProjectBindingImplementation implementation, string specifiedExpression = null, string error = null)
    : base(implementation, scope, error)
    {
        StepDefinitionType = stepDefinitionType;
        Regex = regex;
        SpecifiedExpression = specifiedExpression;
    }

    public override bool IsValid => Regex != null && base.IsValid;
    public ScenarioBlock StepDefinitionType { get; }
    public string SpecifiedExpression { get; }
    public Regex Regex { get; }

    public string Expression => SpecifiedExpression ?? GetSpecifiedExpressionFromRegex();

    private string GetSpecifiedExpressionFromRegex()
    {
        var result = Regex?.ToString();
        if (result == null)
            return null;

        // remove only ONE ^/$ from around the regex
        if (result.StartsWith("^"))
            result = result.Substring(1);
        if (result.EndsWith("$"))
            result = result.Substring(0, result.Length - 1);
        return result;
    }

    private static Regex GetRegexFromSpecifiedExpression(string expression) =>
        new($"^{expression}$", RegexOptions.CultureInvariant);

    public MatchResultItem Match(Step step, IGherkinDocumentContext context, string stepText = null)
    {
        if (!IsValid || !(step is DeveroomGherkinStep deveroomGherkinStep))
            return null;
        if (deveroomGherkinStep.ScenarioBlock != StepDefinitionType)
            return null;
        stepText = stepText ?? step.Text;
        var match = Regex.Match(stepText);
        if (!match.Success)
            return null;

        if (!MatchScope(context))
            return null;

        var parameterMatch = MatchParameter(step, match);
        return MatchResultItem.CreateMatch(this, parameterMatch);
    }

    private ParameterMatch MatchParameter(Step step, Match match)
    {
        var parameterCount = Implementation.ParameterTypes?.Length ?? 0;
        var matchedStepParameters = match.Groups.OfType<Group>().Skip(1)
            .Where(g => g.Success)
            .Select(g => new MatchedStepTextParameter(g.Index, g.Length)).ToArray();
        var expectedParameterCount = matchedStepParameters.Length + (step.Argument == null ? 0 : 1);
        if (parameterCount != expectedParameterCount) //handle parameter error
            return new ParameterMatch(matchedStepParameters, step.Argument, Implementation.ParameterTypes,
                $"The method '{Implementation.Method}' has invalid parameter count, {expectedParameterCount} parameter(s) expected");
        return new ParameterMatch(matchedStepParameters, step.Argument, Implementation.ParameterTypes);
    }

    public override string ToString() => $"[{StepDefinitionType}({Expression})]: {Implementation}";

    public ProjectStepDefinitionBinding WithSpecifiedExpression(string expression)
    {
        var regex = GetRegexFromSpecifiedExpression(expression);
        return new ProjectStepDefinitionBinding(StepDefinitionType, regex, Scope, Implementation, expression, Error);
    }
}
