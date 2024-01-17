using System;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public class SpecFlowV21Discoverer : SpecFlowV22Discoverer
{
    protected override IRuntimeConfigurationProvider CreateConfigurationProvider(string configFilePath) =>
        Activator.CreateInstance<DefaultRuntimeConfigurationProvider>();
}
