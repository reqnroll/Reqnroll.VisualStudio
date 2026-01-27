using Microsoft.Extensions.DependencyInjection;
using Reqnroll.Microsoft.Extensions.DependencyInjection;
using VsExtConnectorTestSamples.AspNetAndMSDI.Tests.StepDefinitions;

namespace VsExtConnectorTestSamples.AspNetAndMSDI.Tests;

public class DI
{
    [ScenarioDependencies]
    public static IServiceCollection CreateServices()
    {
        IServiceCollection services = new ServiceCollection();
        //services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddSingleton<DateTimeStepDefinitions>();
        return services;
    }
}