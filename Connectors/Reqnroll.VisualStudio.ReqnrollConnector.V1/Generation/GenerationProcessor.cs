#nullable disable
using Reqnroll.VisualStudio.ReqnrollConnector.AppDomainHelper;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Generation;

public class GenerationProcessor
{
    private readonly GenerationOptions _options;

    public GenerationProcessor(GenerationOptions options)
    {
        _options = options;
    }

    public string Process()
    {
        var generatorAssemblyPath = Path.Combine(_options.ReqnrollToolsFolder, "Reqnroll.Generator.dll");
        using (AssemblyHelper.SubscribeResolveForAssembly(generatorAssemblyPath))
        {
            var reqnrollAssemblyPath = Path.Combine(_options.ReqnrollToolsFolder, "Reqnroll.dll");
            FileVersionInfo reqnrollVersion = File.Exists(reqnrollAssemblyPath)
                ? FileVersionInfo.GetVersionInfo(reqnrollAssemblyPath)
                : null;

            var generatorType = typeof(ReqnrollV22Generator);
            if (reqnrollVersion != null)
            {
                var versionNumber =
                    (reqnrollVersion.FileMajorPart * 100 + reqnrollVersion.FileMinorPart) * 1000 +
                    reqnrollVersion.FileBuildPart;

                if (versionNumber >= 3_00_000)
                    throw new NotSupportedException(
                        $"Design time code generation is not supported in this reqnroll version: {reqnrollVersion.FileVersion}");
                if (versionNumber >= 2_02_000)
                    generatorType = typeof(ReqnrollV22Generator);
                else if (versionNumber >= 1_09_000)
                    generatorType = typeof(ReqnrollV19Generator);
            }

            var generator = (IReqnrollGenerator) Activator.CreateInstance(generatorType);
            return generator.Generate(_options.ProjectFolder, _options.ConfigFilePath, _options.TargetExtension,
                _options.FeatureFilePath, _options.TargetNamespace, _options.ProjectDefaultNamespace,
                _options.SaveResultToFile);
        }
    }
}
