using System;
using System.Linq;

namespace Reqnroll.VisualStudio.ProjectSystem.Settings;

[Flags]
public enum ReqnrollProjectTraits
{
    None = 0,
    MsBuildGeneration = 1,
    XUnitAdapter = 2,
    DesignTimeFeatureFileGeneration = 4,
    CucumberExpression = 8
}
