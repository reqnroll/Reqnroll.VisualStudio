using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Notifications;

public class NotificationInfoBarFactory
{
    private readonly ExternalBrowserNotificationService _browserNotificationService;
    private readonly IMonitoringService _monitoringService;
    private readonly NotificationDataStore _notificationDataStore;
    private readonly IServiceProvider _serviceProvider;

    public NotificationInfoBarFactory([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
        IIdeScope ideScope, NotificationDataStore notificationDataStore, IMonitoringService monitoringService)
    {
        _serviceProvider = serviceProvider;
        _notificationDataStore = notificationDataStore;
        _browserNotificationService = new ExternalBrowserNotificationService(ideScope);
        _monitoringService = monitoringService;
    }

    public NotificationInfoBar Create(NotificationData notification) => new(_serviceProvider,
        _browserNotificationService, _notificationDataStore, _monitoringService, notification);
}
