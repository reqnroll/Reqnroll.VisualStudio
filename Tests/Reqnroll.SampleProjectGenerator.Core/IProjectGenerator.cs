namespace Reqnroll.SampleProjectGenerator;

public interface IProjectGenerator
{
    string TargetFolder { get; }
    string PackagesFolder { get; }
    string AssemblyName { get; }
    string TargetFramework { get; }
    List<string> FeatureFiles { get; }
    List<NuGetPackageData> InstalledNuGetPackages { get; }
    void Generate();
    string GetOutputAssemblyPath(string config = "Debug");
}
