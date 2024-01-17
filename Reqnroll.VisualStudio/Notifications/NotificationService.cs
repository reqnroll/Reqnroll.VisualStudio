#nullable disable

using System.Net;
using System.Net.Http;

namespace Reqnroll.VisualStudio.Notifications;

public class NotificationService
{
    private const string DefaultApiUrl = "https://notifications.reqnroll.net/api/notifications/visualstudio";
    private const string ReqnrollNotificationUnpublishedEnvironmentVariable = "REQNROLL_NOTIFICATION_UNPUBLISHED";
    private readonly NotificationDataStore _notificationDataStore;
    private readonly NotificationInfoBarFactory _notificationInfoBarFactory;

    public NotificationService(NotificationDataStore notificationDataStore,
        NotificationInfoBarFactory notificationInfoBarFactory)
    {
        _notificationDataStore = notificationDataStore;
        _notificationInfoBarFactory = notificationInfoBarFactory;
    }

    public void Initialize()
    {
        //Fire and forget no await
#pragma warning disable 4014
        Task.Run(CheckAndNotifyAsync);
#pragma warning restore 4014
    }

    private async Task CheckAndNotifyAsync()
    {
        try
        {
            var notification = await GetNotificationAsync();

            if (notification != null && !_notificationDataStore.IsDismissed(notification))
                await NotifyAsync(notification);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, "Error during creating the InfoBar.");
        }
    }

    private static string GetApiUrl() =>
        Environment.GetEnvironmentVariable(ReqnrollNotificationUnpublishedEnvironmentVariable) != "1"
            ? DefaultApiUrl
            : $"{DefaultApiUrl}/unpublished";

    private static async Task<NotificationData> GetNotificationAsync()
    {
        var httpClient = new HttpClient();
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        var result = await httpClient.GetAsync(GetApiUrl());
        result.EnsureSuccessStatusCode();
        var content = await result.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<NotificationData>(content);
    }

    private async Task NotifyAsync(NotificationData notification)
    {
        var infoBar = _notificationInfoBarFactory.Create(notification);
        await infoBar.ShowInfoBar();
    }
}
