#nullable disable
namespace Reqnroll.VisualStudio.ProjectSystem;

public static class ProjectScopeServicesExtensions
{
    public static void InitializeServices(this IProjectScope projectScope)
    {
        projectScope.GetDeveroomConfigurationProvider();
        projectScope.GetProjectSettingsProvider();
        projectScope.GetReqnrollExtensionServicesManager();
    }

    public static ReqnrollExtensionServicesManager GetReqnrollExtensionServicesManager(this IProjectScope projectScope)
    {
        return projectScope.Properties.GetOrCreateSingletonProperty(
            () => new ReqnrollExtensionServicesManager(projectScope));
    }

    public static IDiscoveryService GetDiscoveryService(this IProjectScope projectScope)
    {
        return projectScope.Properties.GetOrCreateSingletonProperty(() =>
        {
            var ideScope = projectScope.IdeScope;
            var classicProvider = new DiscoveryResultProvider(projectScope);
            var configuration = projectScope.GetDeveroomConfiguration();

            // Wrap the classic provider with a per-service-creation proxy when enabled;
            // the ReqnrollExtensionServicesManager singleton is shared across all proxies.
            IDiscoveryResultProvider discoveryResultProvider = configuration.UseConnectorService
                ? new DiscoveryServiceProxy(projectScope.GetReqnrollExtensionServicesManager(), classicProvider)
                : classicProvider;

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

    public static SnippetService GetSnippetService(this IProjectScope projectScope)
    {
        if (!projectScope.GetProjectSettings().IsReqnrollProject)
            return null;

        return projectScope.Properties.GetOrCreateSingletonProperty(() =>
            new SnippetService(projectScope));
    }
}
