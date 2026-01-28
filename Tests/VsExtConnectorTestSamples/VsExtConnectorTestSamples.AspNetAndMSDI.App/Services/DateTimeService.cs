using VsExtConnectorTestSamples.AspNetAndMSDI.App.Interfaces;

namespace VsExtConnectorTestSamples.AspNetAndMSDI.App.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime GetDateTime()
    {
        return DateTime.Now;
    }
}