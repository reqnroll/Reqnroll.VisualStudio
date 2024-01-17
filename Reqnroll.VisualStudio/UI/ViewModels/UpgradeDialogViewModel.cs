namespace Reqnroll.VisualStudio.UI.ViewModels;

public class UpgradeDialogViewModel : WizardViewModel
{
    public const string UPGRADE_HEADER_TEMPLATE = @"
# Reqnroll Updated to v{newVersion}

Please have a look at the changes since the last installed version.

";

    public const string COMMUNITY_INFO_HEADER = @"
# Join the Reqnroll community!

";

    public const string COMMUNITY_INFO_TEXT = COMMUNITY_INFO_HEADER + @"
## Find solutions, share ideas and engage in discussions.

* Join our community forum: https://support.reqnroll.net/

* Join our Discord channel: https://discord.com/invite/xQMrjDXx7a

* Follow us on Twitter: https://twitter.com/reqnroll

* Follow us on LinkedIn: https://www.linkedin.com/company/reqnroll/

* Subscribe on YouTube: https://www.youtube.com/c/ReqnrollBDD

* Join our next webinar: https://reqnroll.net/community/webinars/

In case you are missing an important feature, please leave us your feature request [here](https://support.reqnroll.net/hc/en-us/community/topics/360000519178-Feature-Requests).
";

#if DEBUG
    public static UpgradeDialogViewModel DesignData = new("1.0.99", @"# v1.0.1 - 2019-02-27

Bug fixes:

* CreatePersistentTrackingPosition Exception / Step navigation error
* .NET Core Bindings: Unable to load BoDi.dll (temporary fix)

");
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

    public MarkDownWizardPageViewModel OtherNewsPage =>
        Pages.OfType<MarkDownWizardPageViewModel>().FirstOrDefault(p => p.Name == "Community");

    private static string GetChangesText(string newVersion, string changeLog)
    {
        changeLog = Regex.Replace(changeLog, @"^#\s", m => "#" + m.Value, RegexOptions.Multiline);
        return UPGRADE_HEADER_TEMPLATE.Replace("{newVersion}", newVersion) + Environment.NewLine + changeLog;
    }
}
