namespace Reqnroll.VisualStudio.ProjectSystem.Settings;

public static class ProjectSettingsProjectSystemExtensions
{
    public static ProjectSettings GetProjectSettings(this IProjectScope projectScope)
    {
        var provider = GetProjectSettingsProvider(projectScope);
        return provider.GetProjectSettings();
    }

    public static IProjectSettingsProvider GetProjectSettingsProvider(this IProjectScope projectScope)
    {
        return projectScope.Properties.GetOrCreateSingletonProperty<IProjectSettingsProvider>(() =>
            new ProjectSettingsProvider(projectScope, new ReqnrollProjectSettingsProvider(projectScope)));
    }
}
