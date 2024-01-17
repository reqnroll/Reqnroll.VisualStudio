namespace Reqnroll.VisualStudio.Connectors;

public static class OutProcReqnrollConnectorFactory
{
    public static OutProcReqnrollConnector CreateGeneric(IProjectScope projectScope)
    {
        var ideScope = projectScope.IdeScope;
        var projectSettings = projectScope.GetProjectSettings();
        var deveroomConfiguration = projectScope.GetDeveroomConfiguration();
        var processorArchitecture = GetProcessorArchitecture(deveroomConfiguration, projectSettings);
        return new GenericOutProcReqnrollConnector(
            deveroomConfiguration,
            ideScope.Logger,
            projectSettings.TargetFrameworkMoniker,
            projectScope.IdeScope.GetExtensionFolder(),
            processorArchitecture,
            projectSettings.ReqnrollVersion,
            ideScope.MonitoringService);
    }

    public static OutProcReqnrollConnector Create(IProjectScope projectScope)
    {
        var ideScope = projectScope.IdeScope;
        var projectSettings = projectScope.GetProjectSettings();
        var deveroomConfiguration = projectScope.GetDeveroomConfiguration();
        var processorArchitecture = GetProcessorArchitecture(deveroomConfiguration, projectSettings);
        return new OutProcReqnrollConnector(
            deveroomConfiguration,
            ideScope.Logger,
            projectSettings.TargetFrameworkMoniker,
            projectScope.IdeScope.GetExtensionFolder(),
            processorArchitecture,
            projectSettings.ReqnrollVersion,
            ideScope.MonitoringService);
    }

    private static ProcessorArchitectureSetting GetProcessorArchitecture(DeveroomConfiguration deveroomConfiguration,
        ProjectSettings projectSettings)
    {
        if (deveroomConfiguration.ProcessorArchitecture != ProcessorArchitectureSetting.AutoDetect)
            return deveroomConfiguration.ProcessorArchitecture;
        if (projectSettings.PlatformTarget == ProjectPlatformTarget.x86)
            return ProcessorArchitectureSetting.X86;
        if (projectSettings.PlatformTarget == ProjectPlatformTarget.x64)
            return ProcessorArchitectureSetting.X64;
        if (projectSettings.PlatformTarget == ProjectPlatformTarget.Arm64)
            return ProcessorArchitectureSetting.Arm64;
        return ProcessorArchitectureSetting.UseSystem;
    }
}
