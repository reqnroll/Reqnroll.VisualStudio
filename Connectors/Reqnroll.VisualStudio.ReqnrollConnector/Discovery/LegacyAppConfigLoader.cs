using System;
using System.Globalization;
using System.Xml;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery;
internal static class LegacyAppConfigLoader
{
    public static ReqnrollConfiguration LoadConfiguration(XmlNode configNode, ReqnrollConfiguration configuration)
    {
        // we only load step assemblies, feature language and binding culture
        if (configNode.SelectSingleNode("language") is XmlElement languageNode)
        {
            var featureAttr = languageNode.Attributes["feature"];
            if (featureAttr != null)
            {
                configuration.FeatureLanguage = new CultureInfo(featureAttr.Value);
            }
        }

        if (configNode.SelectSingleNode("bindingCulture") is XmlElement bindingCultureNode)
        {
            var nameAttr = bindingCultureNode.Attributes["name"];
            if (nameAttr != null)
            {
                configuration.BindingCulture = new CultureInfo(nameAttr.Value);
            }
        }

        var stepAssemblyNodes = configNode.SelectNodes("stepAssemblies/stepAssembly");
        if (stepAssemblyNodes != null)
        {
            configuration.AdditionalStepAssemblies ??= new();
            foreach (var stepAssemblyNode in stepAssemblyNodes.OfType<XmlElement>())
            {
                var assemblyAttr = stepAssemblyNode.Attributes["assembly"];
                if (assemblyAttr != null)
                {
                    configuration.AdditionalStepAssemblies.Add(assemblyAttr.Value);
                }
            }
        }

        return configuration;
    }
}
