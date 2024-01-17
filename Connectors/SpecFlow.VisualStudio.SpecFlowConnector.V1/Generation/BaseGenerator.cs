using System;
using System.Linq;
using System.Xml;
using TechTalk.SpecFlow.Generator;
using TechTalk.SpecFlow.Utils;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Generation;

public abstract class BaseGenerator : RemoteContextObject, ISpecFlowGenerator
{
    public string Generate(string projectFolder, string configFilePath, string targetExtension, string featureFilePath,
        string targetNamespace, string projectDefaultNamespace, bool saveResultToFile)
    {
        using (var generator = CreateGenerator(projectFolder, configFilePath, targetExtension, projectDefaultNamespace))
        {
            var featureFileInput =
                new FeatureFileInput(FileSystemHelper.GetRelativePath(featureFilePath, projectFolder))
                {
                    CustomNamespace = targetNamespace
                };

            var generationSettings = new GenerationSettings
            {
                CheckUpToDate = false,
                WriteResultToFile = saveResultToFile
            };
            var result = generator.GenerateTestFile(featureFileInput, generationSettings);
            var connectorResult = new GenerationResult();
            if (result.Success)
                connectorResult.FeatureFileCodeBehind = new FeatureFileCodeBehind
                {
                    FeatureFilePath = featureFilePath,
                    Content = result.GeneratedTestCode
                };
            else
                connectorResult.ErrorMessage =
                    string.Join(Environment.NewLine, result.Errors.Select(e => e.Message));

            var resultJson = JsonSerialization.SerializeObject(connectorResult);
            return resultJson;
        }
    }

    protected virtual ITestGenerator CreateGenerator(string projectFolder, string configFilePath,
        string targetExtension, string projectDefaultNamespace)
    {
        ITestGeneratorFactory testGeneratorFactory = new TestGeneratorFactory();
        var projectSettings = new ProjectSettings(); //TODO: load settings
        projectSettings.ConfigurationHolder = configFilePath == null
            ? new SpecFlowConfigurationHolder()
            : CreateConfigHolder(configFilePath);
        projectSettings.ProjectFolder = projectFolder;
        projectSettings.ProjectPlatformSettings.Language = targetExtension == ".cs"
            ? GenerationTargetLanguage.CSharp
            : GenerationTargetLanguage.VB;
        projectSettings.DefaultNamespace = projectDefaultNamespace;

        return CreateGenerator(testGeneratorFactory, projectSettings);
    }

    protected abstract ITestGenerator CreateGenerator(ITestGeneratorFactory testGeneratorFactory,
        ProjectSettings projectSettings);

    protected abstract SpecFlowConfigurationHolder CreateConfigHolder(string configFilePath);

    protected SpecFlowConfigurationHolder GetXmlConfigurationHolder(string configFileContent)
    {
        var configDocument = new XmlDocument();
        configDocument.LoadXml(configFileContent);
        var configNode = configDocument.SelectSingleNode("/configuration/specFlow");
        return new SpecFlowConfigurationHolder(configNode);
    }
}
