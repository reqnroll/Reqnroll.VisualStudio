namespace Reqnroll.VisualStudio.Analytics;

public class ReqnrollInstallationStatus
{
    public static readonly DateTime MagicDate = new(2024, 1, 12); // when Reqnroll has born
    public static readonly Version UnknownVersion = new(0, 0);
    public bool IsInstalled => InstalledVersion != UnknownVersion;
    public Version InstalledVersion { get; set; } = UnknownVersion;
    public DateTime InstallDate { get; set; }
    public DateTime LastUsedDate { get; set; }
    public int UsageDays { get; set; }
    public int UserLevel { get; set; }
}
