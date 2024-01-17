#nullable disable

public class ReqnrollConfigDeserializer : IConfigDeserializer<DeveroomConfiguration>
{
    private readonly JsonNetConfigDeserializer<ReqnrollJsonConfiguration> _reqnrollConfigDeserializer = new();

    public void Populate(string jsonString, DeveroomConfiguration config)
    {
        var reqnrollJsonConfiguration = new ReqnrollJsonConfiguration {Ide = config};
        // need to support reqnroll V2 configuration: where reqnroll.json had a reqnroll root node
        reqnrollJsonConfiguration.Reqnroll = reqnrollJsonConfiguration;
        _reqnrollConfigDeserializer.Populate(jsonString, reqnrollJsonConfiguration);
        if (reqnrollJsonConfiguration.Language != null &&
            reqnrollJsonConfiguration.Language.TryGetValue("feature", out var featureLanguage))
            config.DefaultFeatureLanguage = featureLanguage;
        if (reqnrollJsonConfiguration.BindingCulture != null &&
            reqnrollJsonConfiguration.BindingCulture.TryGetValue("name", out var bindingCulture))
            config.ConfiguredBindingCulture = bindingCulture;
    }

    private class ReqnrollJsonConfiguration
    {
        public DeveroomConfiguration Ide { get; set; }
        public ReqnrollJsonConfiguration Reqnroll { get; set; }
        public Dictionary<string, string> Language { get; set; }
        public Dictionary<string, string> BindingCulture { get; set; }
    }
}
