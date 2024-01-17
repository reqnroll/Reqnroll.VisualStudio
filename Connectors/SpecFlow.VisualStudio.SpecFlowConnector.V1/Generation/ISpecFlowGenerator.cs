namespace SpecFlow.VisualStudio.SpecFlowConnector.Generation;

public interface ISpecFlowGenerator
{
    string Generate(string projectFolder, string configFilePath, string targetExtension, string featureFilePath,
        string targetNamespace, string projectDefaultNamespace, bool saveResultToFile);
}
