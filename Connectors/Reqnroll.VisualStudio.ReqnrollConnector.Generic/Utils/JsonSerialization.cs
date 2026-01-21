using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReqnrollConnector.Logging;

namespace ReqnrollConnector.Utils;

public static class JsonSerialization
{
    private const string StartMarker = ">>>>>>>>>>";
    private const string EndMarker = "<<<<<<<<<<";

    public static string MarkResult(string content) =>
        StartMarker + Environment.NewLine + content + Environment.NewLine + EndMarker;

    public static string SerializeObjectCamelCase(object obj, ILogger? log = null)
    {
        try
        {
            return JsonSerializer.Serialize(obj, GetJsonSerializerSettingsCamelCase());
        }
        catch (Exception e)
        {
            log?.Error(e.ToString());
            throw;
        }
    }

    public static TResult? DeserializeObjectDefaultCase<TResult>(string json, ILogger? log = null)
    {
        try
        {
            var deserializeObject = JsonSerializer.Deserialize<TResult>(json, GetJsonSerializerSettingsDefaultCase());
            return deserializeObject;
        }
        catch (Exception e)
        {
            log?.Error(e.ToString());
            return default;
        }
    }

    public static JsonSerializerOptions GetJsonSerializerSettingsCamelCase() =>
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

    public static JsonSerializerOptions GetJsonSerializerSettingsDefaultCase() =>
        new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
}
