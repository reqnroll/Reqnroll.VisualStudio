using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace Reqnroll.VisualStudio;

public class ExternalBrowserNotificationService
{
    private readonly IIdeScope _ideScope;

    public ExternalBrowserNotificationService(IIdeScope ideScope)
    {
        _ideScope = ideScope;
    }

    public bool ShowPage(string url)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            _ideScope.Logger.LogWarning($"Showing page for '{url}' skipped, because the machine is offline");
            return false;
        }

        try
        {
            Process.Start(url);
            return true;
        }
        catch (Exception ex)
        {
            _ideScope.Logger.LogException(_ideScope.MonitoringService, ex, $"Browser start error: {ex}");
            return false;
        }
    }
}
