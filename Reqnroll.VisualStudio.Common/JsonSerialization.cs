#nullable disable

using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Reqnroll.VisualStudio.ReqnrollConnector;

internal static class JsonSerialization
{
    private const string StartMarker = ">>>>>>>>>>";
    private const string EndMarker = "<<<<<<<<<<";

    public static string StripResult(string content)
    {
        int startMarkerIndex = content.IndexOf(StartMarker, StringComparison.InvariantCultureIgnoreCase);
        if (startMarkerIndex >= 0)
            content = content.Substring(startMarkerIndex + StartMarker.Length);
        int endMarkerIndex = content.LastIndexOf(EndMarker, StringComparison.InvariantCultureIgnoreCase);
        if (endMarkerIndex >= 0)
            content = content.Substring(0, endMarkerIndex);
        return content.Trim();
    }

    public static string MarkResult(string content) =>
        StartMarker + Environment.NewLine + content + Environment.NewLine + EndMarker;

    public static string SerializeObject(object obj) =>
        JsonConvert.SerializeObject(obj, GetJsonSerializerSettings(true));

    public static T DeserializeObject<T>(string jsonString) =>
        JsonConvert.DeserializeObject<T>(jsonString, GetJsonSerializerSettings(true));

    public static string SerializeObjectWithMarker(object obj) => MarkResult(SerializeObject(obj));

    public static T DeserializeObjectWithMarker<T>(string jsonString) => DeserializeObject<T>(StripResult(jsonString));

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
