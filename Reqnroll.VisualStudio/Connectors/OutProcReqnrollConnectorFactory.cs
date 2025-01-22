namespace Reqnroll.VisualStudio.Connectors;

public static class OutProcReqnrollConnectorFactory
{
    public static bool UseCustomConnector(IProjectScope projectScope)
    {
        var deveroomConfiguration = projectScope.GetDeveroomConfiguration();
        return deveroomConfiguration.BindingDiscovery.ConnectorPath != null;
    }

    public static OutProcReqnrollConnector CreateCustom(IProjectScope projectScope)
    {
        var ideScope = projectScope.IdeScope;
        var projectSettings = projectScope.GetProjectSettings();
        var deveroomConfiguration = projectScope.GetDeveroomConfiguration();
        var processorArchitecture = GetProcessorArchitecture(deveroomConfiguration, projectSettings);
        return new CustomOutProcReqnrollConnector(
            deveroomConfiguration,
            ideScope.Logger,
            projectSettings.TargetFrameworkMoniker,
            projectScope.IdeScope.GetExtensionFolder(),
            processorArchitecture,
            projectSettings,
            ideScope.MonitoringService);
    }

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
            projectSettings,
            ideScope.MonitoringService);
    }

    public static OutProcReqnrollConnector CreateLegacy(IProjectScope projectScope)
    {
        var ideScope = projectScope.IdeScope;
        var projectSettings = projectScope.GetProjectSettings();
        var deveroomConfiguration = projectScope.GetDeveroomConfiguration();
        var processorArchitecture = GetProcessorArchitecture(deveroomConfiguration, projectSettings);
        return new LegacyOutProcReqnrollConnector(
            deveroomConfiguration,
            ideScope.Logger,
            projectSettings.TargetFrameworkMoniker,
            projectScope.IdeScope.GetExtensionFolder(),
            processorArchitecture,
            projectSettings,
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
