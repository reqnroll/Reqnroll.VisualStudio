namespace Reqnroll.VisualStudio.ReqnrollConnector.Generation;

public interface IReqnrollGenerator
{
    string Generate(string projectFolder, string configFilePath, string targetExtension, string featureFilePath,
        string targetNamespace, string projectDefaultNamespace, bool saveResultToFile);
}
