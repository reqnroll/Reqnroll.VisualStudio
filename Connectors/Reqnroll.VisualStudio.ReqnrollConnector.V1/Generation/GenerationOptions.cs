#nullable disable

namespace Reqnroll.VisualStudio.ReqnrollConnector.Generation;

[Serializable]
public class GenerationOptions
{
    public string FeatureFilePath { get; set; }
    public string ConfigFilePath { get; set; }
    public string TargetExtension { get; set; }
    public string TargetNamespace { get; set; }
    public string ProjectFolder { get; set; }
    public string ProjectDefaultNamespace { get; set; }
    public bool SaveResultToFile { get; set; }
    public string ReqnrollToolsFolder { get; set; }

    public static GenerationOptions Parse(string[] args)
    {
        var saveResultToFile = args.Contains("--save");
        var projectFolder = Path.GetFullPath(args[4])
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return new GenerationOptions
        {
            FeatureFilePath = Path.GetFullPath(Path.Combine(projectFolder, args[0])),
            ConfigFilePath = string.IsNullOrWhiteSpace(args[1])
                ? null
                : Path.GetFullPath(Path.Combine(projectFolder, args[1])),
            TargetExtension = args[2],
            TargetNamespace = args[3],
            ProjectFolder = projectFolder,
            ProjectDefaultNamespace = args[5],
            SaveResultToFile = saveResultToFile,
            ReqnrollToolsFolder = Directory.GetCurrentDirectory()
        };
    }
}
