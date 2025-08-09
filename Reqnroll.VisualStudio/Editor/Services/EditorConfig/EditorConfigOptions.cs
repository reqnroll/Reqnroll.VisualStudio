#nullable disable
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Reqnroll.VisualStudio.Editor.Services.EditorConfig;

/// <summary>
/// A simplified implementation of IEditorConfigOptions that extracts values directly from the DocumentOptionSet.
/// </summary>
public class EditorConfigOptions : IEditorConfigOptions
{
    private readonly DocumentOptionSet _options;
    private readonly Dictionary<string, string> _directEditorConfigValues;

    public EditorConfigOptions(DocumentOptionSet options)
    {
        _options = options;
        _directEditorConfigValues = ExtractEditorConfigValues();
    }

    public TResult GetOption<TResult>(string editorConfigKey, TResult defaultValue)
    {
        if (_directEditorConfigValues.TryGetValue(editorConfigKey, out var simplifiedValue))
        {
            if (TryConvertFromString(simplifiedValue, defaultValue, out var convertedValue))
            {
                return convertedValue;
            }
        }
        return defaultValue;
    }

    private static bool TryConvertFromString<TResult>(string value, TResult defaultValue, out TResult convertedValue)
    {
        convertedValue = typeof(TResult) switch
        {
            var t when t == typeof(bool) && bool.TryParse(value, out var boolVal) => (TResult)(object)boolVal,
            var t when t == typeof(int) && int.TryParse(value, out var intVal) => (TResult)(object)intVal,
            var t when t == typeof(string) => (TResult)(object)value,
            _ => defaultValue
        };

        return !Equals(convertedValue, defaultValue);
    }

    private Dictionary<string, string> ExtractEditorConfigValues()
    {
        var values = new Dictionary<string, string>();

        try
        {
            // Use reflection to access the underlying options collection safely
            var optionsType = _options.GetType();
            var structuredAnalyzerConfigOptionsField = optionsType.GetField("_configOptions", BindingFlags.NonPublic | BindingFlags.Instance);
            var structuredAnalyzerConfigOptions = structuredAnalyzerConfigOptionsField?.GetValue(_options);

            var structuredAnalyzerConfigOptionsType = structuredAnalyzerConfigOptions?.GetType();
            var optionsField = structuredAnalyzerConfigOptionsType?.GetField("_options", BindingFlags.NonPublic | BindingFlags.Instance);
            var optionsAnalyzerConfigOptions = optionsField?.GetValue(structuredAnalyzerConfigOptions);
            var dacOptionsField = optionsAnalyzerConfigOptions?.GetType().GetField("Options", BindingFlags.NonPublic | BindingFlags.Instance);
            var optionsCollection = dacOptionsField?.GetValue(optionsAnalyzerConfigOptions) as ImmutableDictionary<string, string>;
            if (optionsCollection != null)
                values = new Dictionary<string, string>(optionsCollection);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to extract EditorConfig values: {ex.Message}");
        }

        return values;
    }
}