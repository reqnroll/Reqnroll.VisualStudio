namespace Reqnroll.VisualStudio.Discovery;
public enum HookType
{
    Unknown = 0,
    BeforeTestRun = 1,
    BeforeTestThread = 2,
    BeforeFeature = 3,
    BeforeScenario = 4,
    BeforeScenarioBlock = 5,
    BeforeStep = 6,
    AfterStep = 7,
    AfterScenarioBlock = 8,
    AfterScenario = 9,
    AfterFeature = 10,
    AfterTestThread = 11,
    AfterTestRun = 12,
}