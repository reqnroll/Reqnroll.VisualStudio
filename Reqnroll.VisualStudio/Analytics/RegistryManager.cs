namespace Reqnroll.VisualStudio.Analytics;

public interface IRegistryManager
{
    bool UpdateStatus(ReqnrollInstallationStatus status);
    ReqnrollInstallationStatus GetInstallStatus();
}

[Export(typeof(IRegistryManager))]
public class RegistryManager : IRegistryManager
{
#if DEBUG
    private static string RegPath => @"Software\Tricentis\Reqnroll\Debug";
#else
    private static string RegPath => @"Software\Tricentis\Reqnroll";
#endif

    private static string RegPathFallback => @"Software\TechTalk\Reqnroll";
    private const string Version2019 = "version";
    private const string Version = "version.vs2022";
    private const string InstallDate = "installDate.vs2022";
    private const string LastUsedDate = "lastUsedDate";
    private const string UsageDays = "usageDays";
    private const string UserLevel = "userLevel";

    public ReqnrollInstallationStatus GetInstallStatus()
    {
        var status = new ReqnrollInstallationStatus();

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegPath, RegistryKeyPermissionCheck.ReadSubTree)
                            ?? Registry.CurrentUser.OpenSubKey(RegPathFallback, RegistryKeyPermissionCheck.ReadSubTree);

            status.Installed2019Version = ReadVersion(key, Version2019);
            status.InstalledVersion = ReadVersion(key, Version);
            status.InstallDate = ReadDate(key, InstallDate);
            status.LastUsedDate = ReadDate(key, LastUsedDate);
            status.UsageDays = ReadIntValue(key, UsageDays);
            status.UserLevel = ReadIntValue(key, UserLevel);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, $"Registry read error: {this}");
        }

        return status;
    }

    public bool UpdateStatus(ReqnrollInstallationStatus status)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegPath, RegistryKeyPermissionCheck.ReadWriteSubTree);

        if (key == null)
            return false;

        if (status.InstalledVersion != null)
            key.SetValue(Version, status.InstalledVersion);
        if (status.InstallDate != ReqnrollInstallationStatus.MagicDate)
            key.SetValue(InstallDate, SerializeDate(status.InstallDate));
        if (status.LastUsedDate != ReqnrollInstallationStatus.MagicDate)
            key.SetValue(LastUsedDate, SerializeDate(status.LastUsedDate));
        key.SetValue(UsageDays, status.UsageDays);
        key.SetValue(UserLevel, status.UserLevel);

        return true;
    }

    private Version ReadVersion(RegistryKey key, string name)
    {
        if (key.GetValue(name) is string value)
            return new Version(value);
        return ReqnrollInstallationStatus.UnknownVersion;
    }

    private DateTime ReadDate(RegistryKey key, string name)
    {
        var value = key.GetValue(name);
        return DeserializeDate((int) value);
    }

    private int ReadIntValue(RegistryKey key, string name)
    {
        var value = key.GetValue(name);
        return (int) value;
    }

    private DateTime DeserializeDate(int days)
    {
        if (days <= 0)
            return ReqnrollInstallationStatus.MagicDate;
        return ReqnrollInstallationStatus.MagicDate.AddDays(days);
    }

    private int SerializeDate(DateTime dateTime) =>
        (int) dateTime.Date.Subtract(ReqnrollInstallationStatus.MagicDate).TotalDays;
}
