using Location = Gherkin.Ast.Location;

namespace Reqnroll.VisualStudio.Editor.Services.Parser;

public class DeveroomGherkinStep : Step
{
    public DeveroomGherkinStep(Location location, string keyword, string text, StepArgument argument,
        StepKeyword stepKeyword, ScenarioBlock scenarioBlock) : base(location, keyword, text, argument)
    {
        StepKeyword = stepKeyword;
        ScenarioBlock = scenarioBlock;
    }

    public ScenarioBlock ScenarioBlock { get; }
    public StepKeyword StepKeyword { get; }
}
