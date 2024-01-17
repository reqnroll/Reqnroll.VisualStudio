namespace Reqnroll.VisualStudio.ProjectSystem.Configuration;

public static class ConfigurationProjectSystemExtensions
{
    public static DeveroomConfiguration GetDeveroomConfiguration(this IProjectScope projectScope)
    {
        var provider = GetDeveroomConfigurationProvider(projectScope);
        return provider.GetConfiguration();
    }

    public static IDeveroomConfigurationProvider GetDeveroomConfigurationProvider(this IProjectScope projectScope)
    {
        return projectScope.Properties.GetOrCreateSingletonProperty<IDeveroomConfigurationProvider>(() =>
            new ProjectScopeDeveroomConfigurationProvider(projectScope));
    }

    public static DeveroomConfiguration GetDeveroomConfiguration(this IIdeScope ideScope, IProjectScope projectScope)
    {
        var provider = ideScope.GetDeveroomConfigurationProvider(projectScope);
        return provider.GetConfiguration();
    }

    public static IDeveroomConfigurationProvider GetDeveroomConfigurationProvider(this IIdeScope ideScope,
        IProjectScope projectScope)
    {
        if (projectScope != null)
            return projectScope.GetDeveroomConfigurationProvider();
        return new ProjectSystemDeveroomConfigurationProvider(ideScope);
    }
}
