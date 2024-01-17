using System;
using Reqnroll;

namespace TestProject
{
    [Binding]
    public class Feature1StepDefinitions
    {
        [When(@"I press add")]
        public void WhenIPressAdd()
        {
            throw new PendingStepException();
        }

        [When(@"I press subtract")]
        public void WhenIPressAdd()
        {
            throw new PendingStepException();
        }
    }
}
