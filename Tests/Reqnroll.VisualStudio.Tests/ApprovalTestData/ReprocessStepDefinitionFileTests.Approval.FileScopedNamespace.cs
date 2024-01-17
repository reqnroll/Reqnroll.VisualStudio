namespace TestProject;

[Binding]
public class Feature1StepDefinitions
{
    [When(@"I press add")]
    public void WhenIPressAdd()
    {
        throw new PendingStepException();
    }
}
