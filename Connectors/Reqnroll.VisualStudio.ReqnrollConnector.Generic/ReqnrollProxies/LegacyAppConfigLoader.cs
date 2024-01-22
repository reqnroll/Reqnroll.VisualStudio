namespace ReqnrollConnector.ReqnrollProxies;
internal static class LegacyAppConfigLoader
{
    class ConfigData
    {
        public FeatureData? Feature { get; set; }
        public BindingCultureData? BindingCulture { get; set; }
        public StepAssemblyData[]? StepAssemblies { get; set; }
    }

    class FeatureData
    {
        public string? FeatureLanguage { get; set; }
    }

    class BindingCultureData
    {
        public string? Name { get; set; }
    }

    class StepAssemblyData
    {
        public string? Assembly { get; set; }
    }

    public static string? LoadConfiguration(FileDetails configFileDetails)
    {
        var configFileContent = File.ReadAllText(configFileDetails.FullName);

        var configDocument = new XmlDocument();
        configDocument.LoadXml(configFileContent);
        var specFlowNode = configDocument.SelectSingleNode("/configuration/specFlow");
        if (specFlowNode == null)
            return null;

        return LoadConfiguration(specFlowNode);
    }

    private static string LoadConfiguration(XmlNode configNode)
    {
        var configData = new ConfigData();

        // we only load step assemblies, feature language and binding culture
        if (configNode.SelectSingleNode("language") is XmlElement languageNode)
        {
            var featureAttr = languageNode.Attributes["feature"];
            if (featureAttr != null)
            {
                configData.Feature = new FeatureData
                {
                    FeatureLanguage = featureAttr.Value
                };
            }
        }

        if (configNode.SelectSingleNode("bindingCulture") is XmlElement bindingCultureNode)
        {
            var nameAttr = bindingCultureNode.Attributes["name"];
            if (nameAttr != null)
            {
                configData.BindingCulture = new BindingCultureData
                {
                    Name = nameAttr.Value
                };
            }
        }

        var stepAssemblyNodes = configNode.SelectNodes("stepAssemblies/stepAssembly");
        if (stepAssemblyNodes != null)
        {
            var stepAssemblies = new List<StepAssemblyData>();
            foreach (var stepAssemblyNode in stepAssemblyNodes.OfType<XmlElement>())
            {
                var assemblyAttr = stepAssemblyNode.Attributes["assembly"];
                if (assemblyAttr != null)
                {
                    stepAssemblies.Add(new StepAssemblyData{ Assembly = assemblyAttr.Value });
                }
            }

            configData.StepAssemblies = stepAssemblies.ToArray();
        }

        return JsonSerialization.SerializeObject(configData);
    }
}
