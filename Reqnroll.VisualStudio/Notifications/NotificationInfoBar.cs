#nullable disable

using Microsoft.VisualStudio.Imaging;

namespace Reqnroll.VisualStudio.Notifications;

public class NotificationInfoBar : IVsInfoBarUIEvents
{
    private readonly ExternalBrowserNotificationService _browserService;
    private readonly IMonitoringService _monitoringService;
    private readonly NotificationData _notification;
    private readonly NotificationDataStore _notificationDataStore;
    private readonly IServiceProvider _serviceProvider;
    private uint _cookie;

    public NotificationInfoBar(
        IServiceProvider serviceProvider,
        ExternalBrowserNotificationService browserService,
        NotificationDataStore notificationDataStore,
        IMonitoringService monitoringService,
        NotificationData notification)
    {
        _serviceProvider = serviceProvider;
        _browserService = browserService;
        _notificationDataStore = notificationDataStore;
        _monitoringService = monitoringService;
        _notification = notification;
    }

    public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
    {
        infoBarUIElement.Unadvise(_cookie);
        _notificationDataStore.SetDismissed(_notification);
        _monitoringService.MonitorNotificationDismissed(_notification);
    }

    public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        string url = (string) actionItem.ActionContext;

        var opened = _browserService.ShowPage(url);
        if (opened)
            _monitoringService.MonitorLinkClicked("NotificationInfoBar", _notification.LinkUrl,
                new Dictionary<string, object>
                {
                    {"NotificationId", _notification.Id}
                });
    }

    public async Task ShowInfoBar()
    {
        await ShowInfoBar(_notification.Message, _notification.LinkText, _notification.LinkUrl);
        _monitoringService.MonitorNotificationShown(_notification);
    }

    private async Task ShowInfoBar(string message, string linkText, string linkUrl)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var shell = _serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
        if (shell != null)
        {
            shell.GetProperty((int) __VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);
            var host = (IVsInfoBarHost) obj;

            if (host == null) return;

            InfoBarTextSpan text = new InfoBarTextSpan(message);
            var actionItems = new List<InfoBarActionItem>();
            if (!string.IsNullOrWhiteSpace(linkText) && !string.IsNullOrWhiteSpace(linkUrl))
                actionItems.Add(new InfoBarHyperlink(linkText, linkUrl));
            InfoBarModel infoBarModel = new InfoBarModel(
                new[] {text},
                actionItems,
                KnownMonikers.StatusInformation);

            var factory = _serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
            IVsInfoBarUIElement element = factory.CreateInfoBar(infoBarModel);
            element.Advise(this, out _cookie);
            host.AddInfoBar(element);
        }
    }
}
