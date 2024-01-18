namespace Reqnroll.VisualStudio.ProjectSystem.Settings;

[Flags]
public enum ReqnrollProjectTraits
{
    None = 0,
    MsBuildGeneration               = 0b00000001,
    XUnitAdapter                    = 0b00000010,
    DesignTimeFeatureFileGeneration = 0b00000100,
    CucumberExpression              = 0b00001000,
    LegacySpecFlow                  = 0b00010000,
    SpecFlowCompatibility           = 0b00100000
}
