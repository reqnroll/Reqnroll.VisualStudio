using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Specs.Support;

public static class DomainDefaults
{
    public const string StepDefinitionFileName = "Steps.cs";

    //TODO: calculate latest versions automatically
    public static NuGetVersion LatestSpecFlowV2Version = new("2.4.1", "2.4.1");
    public static NuGetVersion LatestSpecFlowV3Version = new("3.6.23", "3.6.23");
    public static NuGetVersion LatestReqnrollVersion = new("1.0.0", "1.0.0");
}
