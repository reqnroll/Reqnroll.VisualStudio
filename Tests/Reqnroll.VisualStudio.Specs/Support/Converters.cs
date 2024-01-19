using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Specs.Support;

[Binding]
public class Converters
{
    [StepArgumentTransformation(@"the latest version")]
    public NuGetVersion LatestVersionConverter() => DomainDefaults.LatestReqnrollVersion;

    [StepArgumentTransformation(@"SpecFlow v2\.\*")]
    public NuGetVersion LatestSpecFlowV2VersionConverter() => DomainDefaults.LatestSpecFlowV2Version;

    [StepArgumentTransformation(@"the latest SpecFlow v3 version")]
    [StepArgumentTransformation(@"SpecFlow v3\.\*")]
    [StepArgumentTransformation(@"SpecFlow v3\.1\.\*")]
    public NuGetVersion LatestSpecFlowV3VersionConverter() => DomainDefaults.LatestSpecFlowV3Version;

    [StepArgumentTransformation(@"v(\d[\d\.\-\w]+)")]
    public NuGetVersion VersionConverter(string versionString) => new(versionString, versionString);

    [StepArgumentTransformation]
    public string[] CommaSeparatedList(string list)
    {
        return list.Split(new[] {' ', ','}, StringSplitOptions.RemoveEmptyEntries);
    }

    [StepArgumentTransformation]
    public int[] CommaSeparatedIntList(string list) => CommaSeparatedList(list).Select(int.Parse).ToArray();
}
