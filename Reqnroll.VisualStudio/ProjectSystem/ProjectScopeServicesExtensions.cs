#nullable disable
namespace Reqnroll.VisualStudio.ProjectSystem;

public static class ProjectScopeServicesExtensions
{
    public static void InitializeServices(this IProjectScope projectScope)
    {
        projectScope.GetDeveroomConfigurationProvider();
        projectScope.GetProjectSettingsProvider();
    }

    public static IDiscoveryService GetDiscoveryService(this IProjectScope projectScope)
    {
        return projectScope.Properties.GetOrCreateSingletonProperty(() =>
        {
            var ideScope = projectScope.IdeScope;
            var discoveryResultProvider = new DiscoveryResultProvider(projectScope);
            var bindingRegistryCache = new ProjectBindingRegistryCache(ideScope);
            IDiscoveryService discoveryService =
                new DiscoveryService(projectScope, discoveryResultProvider, bindingRegistryCache);
            discoveryService.TriggerDiscovery("ProjectScopeServicesExtensions.GetDiscoveryService");
            return discoveryService;
        });
    }

    public static IDeveroomTagParser GetDeveroomTagParser(this IProjectScope projectScope)
    {
        return projectScope.Properties.GetOrCreateSingletonProperty(() =>
        {
            var deveroomConfigurationProvider = projectScope.GetDeveroomConfigurationProvider();
            var discoveryService = projectScope.GetDiscoveryService();
            IDeveroomTagParser tagParser = new DeveroomTagParser(
                projectScope.IdeScope.Logger,
                projectScope.IdeScope.MonitoringService,
                deveroomConfigurationProvider,
                discoveryService);
            return tagParser;
        });
    }

    public static GenerationService GetGenerationService(this IProjectScope projectScope)
    {
        if (!projectScope.GetProjectSettings().IsReqnrollProject)
            return null;

        return projectScope.Properties.GetOrCreateSingletonProperty(() =>
            new GenerationService(projectScope));
    }

    public static SnippetService GetSnippetService(this IProjectScope projectScope)
    {
        if (!projectScope.GetProjectSettings().IsReqnrollProject)
            return null;

        return projectScope.Properties.GetOrCreateSingletonProperty(() =>
            new SnippetService(projectScope));
    }
}
