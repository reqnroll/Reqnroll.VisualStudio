namespace Reqnroll.VisualStudio.ProjectSystem.Settings;

public record ProjectSettings(
    DeveroomProjectKind Kind,
    TargetFrameworkMoniker TargetFrameworkMoniker,
    string TargetFrameworkMonikers,
    ProjectPlatformTarget PlatformTarget,
    string OutputAssemblyPath,
    string DefaultNamespace,
    NuGetVersion ReqnrollVersion,
    string ReqnrollGeneratorFolder,
    string ReqnrollConfigFilePath,
    ReqnrollProjectTraits ReqnrollProjectTraits,
    ProjectProgrammingLanguage ProgrammingLanguage
)
{
    public bool IsUninitialized => Kind == DeveroomProjectKind.Uninitialized;
    public bool IsReqnrollTestProject => Kind == DeveroomProjectKind.ReqnrollTestProject;
    public bool IsReqnrollLibProject => Kind == DeveroomProjectKind.ReqnrollLibProject;
    public bool IsReqnrollProject => IsReqnrollTestProject || IsReqnrollLibProject;

    public bool DesignTimeFeatureFileGenerationEnabled =>
        ReqnrollProjectTraits.HasFlag(ReqnrollProjectTraits.DesignTimeFeatureFileGeneration);

    public bool HasDesignTimeGenerationReplacement =>
        ReqnrollProjectTraits.HasFlag(ReqnrollProjectTraits.MsBuildGeneration) ||
        ReqnrollProjectTraits.HasFlag(ReqnrollProjectTraits.XUnitAdapter);

    public string GetReqnrollVersionLabel() => ReqnrollVersion?.ToString() ?? "n/a";

    public string GetShortLabel()
    {
        var result = $"{TargetFrameworkMoniker},Reqnroll:{GetReqnrollVersionLabel()}";
        if (PlatformTarget != ProjectPlatformTarget.Unknown && PlatformTarget != ProjectPlatformTarget.AnyCpu)
            result += "," + PlatformTarget;
        if (DesignTimeFeatureFileGenerationEnabled)
            result += ",Gen";
        return result;
    }
}
