#nullable disable
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Reqnroll.VisualStudio.Configuration;

internal interface IConfigDeserializer<in TConfiguration>
{
    void Populate(string jsonString, TConfiguration config);
}

internal class JsonNetConfigDeserializer<TConfiguration> : IConfigDeserializer<TConfiguration>
{
    public void Populate(string jsonString, TConfiguration config)
    {
        JsonConvert.PopulateObject(jsonString, config, GetJsonSerializerSettings(true));
    }

    public static JsonSerializerSettings GetJsonSerializerSettings(bool indented)
    {
        var serializerSettings = new JsonSerializerSettings();
        var contractResolver = new CamelCasePropertyNamesContractResolver();
        contractResolver.NamingStrategy.ProcessDictionaryKeys = false;
        serializerSettings.ContractResolver = contractResolver;
        serializerSettings.Converters = new List<JsonConverter>
        {
            new StringEnumConverter
            {
#if OLD_JSONNET_API
                CamelCaseText = true
#else
                NamingStrategy = new CamelCaseNamingStrategy()
#endif
            }
        };
        serializerSettings.Formatting = indented ? Formatting.Indented : Formatting.None;
        serializerSettings.NullValueHandling = NullValueHandling.Ignore;
        return serializerSettings;
    }
}
