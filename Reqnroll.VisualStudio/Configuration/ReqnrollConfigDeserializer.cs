#nullable disable

namespace Reqnroll.VisualStudio.Configuration;

public class ReqnrollConfigDeserializer : IConfigDeserializer<DeveroomConfiguration>
{
    private readonly JsonNetConfigDeserializer<ReqnrollJsonConfiguration> _reqnrollConfigDeserializer = new();

    public void Populate(string jsonString, DeveroomConfiguration config)
    {
        var reqnrollJsonConfiguration = new ReqnrollJsonConfiguration {Ide = config};
        _reqnrollConfigDeserializer.Populate(jsonString, reqnrollJsonConfiguration);
        if (reqnrollJsonConfiguration.Language != null &&
            reqnrollJsonConfiguration.Language.TryGetValue("feature", out var featureLanguage))
            config.DefaultFeatureLanguage = featureLanguage;
        if (reqnrollJsonConfiguration.Language != null &&
            reqnrollJsonConfiguration.Language.TryGetValue("binding", out var bindingCulture))
            config.DefaultFeatureLanguage = bindingCulture;
        if (reqnrollJsonConfiguration.BindingCulture != null &&
            reqnrollJsonConfiguration.BindingCulture.TryGetValue("name", out var bindingCultureFromSpecFlow))
            config.ConfiguredBindingCulture = bindingCultureFromSpecFlow;
    }

    private class ReqnrollJsonConfiguration
    {
        public DeveroomConfiguration Ide { get; set; }
        public Dictionary<string, string> Language { get; set; }
        public Dictionary<string, string> BindingCulture { get; set; }
    }
}
