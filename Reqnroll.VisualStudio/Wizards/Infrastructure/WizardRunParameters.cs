namespace Reqnroll.VisualStudio.Wizards.Infrastructure;

public class WizardRunParameters
{
    public const string CustomToolSettingKey = "$customtool$";
    public const string BuildActionKey = "$buildaction$";
    public const string CopyToOutputDirectoryKey = "$copytooutputdir$";

    public WizardRunParameters(bool isAddNewItem, IProjectScope projectScope, IIdeScope ideScope, string templateFolder,
        string targetFolder, string targetFileName, Dictionary<string, string> replacementsDictionary)
    {
        IsAddNewItem = isAddNewItem;
        ProjectScope = projectScope;
        IdeScope = ideScope;
        TemplateFolder = templateFolder;
        TargetFolder = targetFolder;
        TargetFileName = targetFileName;
        ReplacementsDictionary = replacementsDictionary;
    }

    public bool IsAddNewItem { get; }
    public IProjectScope ProjectScope { get; }
    public IIdeScope IdeScope { get; }
    public string TemplateFolder { get; }
    public string TargetFolder { get; }
    public string TargetFileName { get; set; }
    public Dictionary<string, string> ReplacementsDictionary { get; }

    public IMonitoringService MonitoringService => ProjectScope.IdeScope.MonitoringService;
}
