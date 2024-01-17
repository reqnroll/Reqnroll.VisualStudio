using System;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery;

public class ReqnrollV21Discoverer : ReqnrollV22Discoverer
{
    protected override IRuntimeConfigurationProvider CreateConfigurationProvider(string configFilePath) =>
        Activator.CreateInstance<DefaultRuntimeConfigurationProvider>();
}
