namespace Reqnroll.VisualStudio.Analytics;

[Export(typeof(ITelemetryConfigurationHolder))]
public class ApplicationInsightsConfigurationHolder : ITelemetryConfigurationHolder
{
    private readonly IContextInitializer _contextInitializer;

    [ImportingConstructor]
    public ApplicationInsightsConfigurationHolder(IContextInitializer contextInitializer)
    {
        _contextInitializer = contextInitializer;
    }

    public void ApplyConfiguration()
    {
        using (var stream = typeof(ApplicationInsightsConfigurationHolder).Assembly.GetManifestResourceStream(
                   "Reqnroll.VisualStudio.Analytics.InstrumentationKey.txt"))
        {
            using (var reader = new StreamReader(stream))
            {
                var instrumentationKey = reader.ReadLine();

                TelemetryConfiguration.Active.InstrumentationKey = instrumentationKey;
                TelemetryConfiguration.Active.TelemetryChannel = new InMemoryChannel();
                TelemetryConfiguration.Active.ContextInitializers.Add(_contextInitializer);
            }
        }
    }
}
