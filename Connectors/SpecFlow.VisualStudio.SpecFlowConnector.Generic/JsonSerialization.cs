using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecFlowConnector;

public static class JsonSerialization
{
    private const string StartMarker = ">>>>>>>>>>";
    private const string EndMarker = "<<<<<<<<<<";

    public static string MarkResult(string content) =>
        StartMarker + Environment.NewLine + content + Environment.NewLine + EndMarker;

    public static string SerializeObject(object obj, ILogger? log = null)
    {
        try
        {
            return JsonSerializer.Serialize(obj, GetJsonSerializerSettings());
        }
        catch (Exception e)
        {
            log?.Error(e.ToString());
            throw;
        }
    }

    public static Option<TResult> DeserializeObject<TResult>(string json, ILogger? log = null)
    {
        try
        {
            var deserializeObject = JsonSerializer.Deserialize<TResult>(json, GetJsonSerializerSettings());
            return deserializeObject;
        }
        catch (Exception e)
        {
            log?.Error(e.ToString());
            return None.Value;
        }
    }

    public static JsonSerializerOptions GetJsonSerializerSettings() =>
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

    public static DiscoveryOptions DeserializeDiscoveryOptions(string json, ILogger? log = null)
    {
        try
        {
            var deserializeObject = JsonSerializer.Deserialize<DiscoveryOptions>(json, GetJsonSerializerSettings());
            if (deserializeObject == null)
            {
                throw new InvalidOperationException("Deserialized DiscoveryOptions is null");
            }
            return deserializeObject;
        }
        catch (Exception e)
        {
            log?.Error(e.ToString());
            throw;
        }
    }

    public static string SerializeRunnerResult(ReflectionExecutor.RunnerResult runnerResult, ILogger? log = null)
    {
        try
        {
            return JsonSerializer.Serialize(runnerResult, GetJsonSerializerSettings());
        }
        catch (Exception e)
        {
            log?.Error(e.ToString());
            return $"{e.Message}{Environment.NewLine}{runnerResult.errorMessage}{Environment.NewLine}{runnerResult.Log}";
        }
    }

    public static ReflectionExecutor.RunnerResult DeserializeObjectRunnerResult(string json, ILogger? log = null)
    {
        try
        {
            var deserializeObject = JsonSerializer.Deserialize<ReflectionExecutor.RunnerResult>(json, GetJsonSerializerSettings());
            if (deserializeObject == null)
            {
                throw new InvalidOperationException("Deserialized RunnerResult is null");
            }
            return deserializeObject;
        }
        catch (Exception e)
        {
            log?.Error(e.ToString());
            return new ReflectionExecutor.RunnerResult(log?.ToString() ?? "", ImmutableSortedDictionary<string, string>.Empty, null, 
                $"{e.Message}{Environment.NewLine}{json}");
        }
    }
}
