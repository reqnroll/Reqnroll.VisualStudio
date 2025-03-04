namespace Reqnroll.VisualStudio.UI.ViewModels;

public class UpgradeDialogViewModel : WizardViewModel
{
    public const string UPGRADE_HEADER_TEMPLATE = """
        # Reqnroll Updated to v{newVersion}

        Please have a look at the changes since the last installed version.

        """;

    public const string COMMUNITY_INFO_HEADER = """
        # Join the Reqnroll community!

        """;

    public const string COMMUNITY_INFO_TEXT = COMMUNITY_INFO_HEADER +
        """
        Reqnroll is an independent project that is owned and supported by the community.
        
        If you find a bug or an issue with the Reqnroll Visual Studio extension, please 
        report it on GitHub: https://github.com/reqnroll/Reqnroll.VisualStudio/issues.
        
        For all other communication (questions, feature suggestions, discussions), please 
        use the GitHub discussion board: https://github.com/reqnroll/Reqnroll/discussions
        our our [Discord server channel](https://go.reqnroll.net/discord-invite).

        Further contact details can be found on our website: https://reqnroll.net/contact/.
        """;

#if DEBUG
    public static UpgradeDialogViewModel DesignData = new("1.0.99",
        """
        $# v1.0.1 - 2019-02-27

        $## Bug fixes:

        * CreatePersistentTrackingPosition Exception / Step navigation error (#3)
        * .NET Core Bindings: Unable to load BoDi.dll (temporary fix)

        """.Replace("$#", "#")); // # on a line start confuses compiler
    public static UpgradeDialogViewModel GetDesignDataWithRealChangeLog() => 
        new("1.0.99", File.ReadAllText("../../../../../CHANGELOG.md"));
#endif

    public UpgradeDialogViewModel(string newVersion, string changeLog) : base("Close", "Welcome to Reqnroll",
        new MarkDownWizardPageViewModel("Changes")
        {
            Text = GetChangesText(newVersion, changeLog)
        },
        new MarkDownWizardPageViewModel("Community")
        {
            Text = COMMUNITY_INFO_TEXT
        })
    {
    }

    private static string GetChangesText(string newVersion, string changeLog)
    {
        changeLog = Regex.Replace(changeLog, @"^#+\s", m => "#" + m.Value, RegexOptions.Multiline);
        return UPGRADE_HEADER_TEMPLATE.Replace("{newVersion}", newVersion) + Environment.NewLine + changeLog;
    }
}
