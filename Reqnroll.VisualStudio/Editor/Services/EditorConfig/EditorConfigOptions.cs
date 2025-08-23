#nullable disable
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
        System.Diagnostics.Debug.WriteLine($"[EditorConfigOptions] GetOption: key='{editorConfigKey}', defaultValue='{defaultValue}', type={typeof(TResult).Name}");
        
        if (_directEditorConfigValues.TryGetValue(editorConfigKey, out var simplifiedValue))
        {
            System.Diagnostics.Debug.WriteLine($"[EditorConfigOptions] Found value: '{simplifiedValue}' for key '{editorConfigKey}'");
            if (TryConvertFromString(simplifiedValue, defaultValue, out var convertedValue))
            {
                System.Diagnostics.Debug.WriteLine($"[EditorConfigOptions] Converted value: '{convertedValue}' for key '{editorConfigKey}'");
                return convertedValue;
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[EditorConfigOptions] No value found for key '{editorConfigKey}', returning default: '{defaultValue}'");
        }
        return defaultValue;
    }

    private static bool TryConvertFromString<TResult>(string value, TResult defaultValue, out TResult convertedValue)
    {
        try
        {
            convertedValue = (TResult)Convert.ChangeType(value, typeof(TResult));
            return true;
        }
        catch
        {
            convertedValue = defaultValue;
            return false;
        }
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
            var dacOptionsField = optionsAnalyzerConfigOptions?.GetType()?.GetField("Options", BindingFlags.NonPublic | BindingFlags.Instance);
            var optionsCollection = dacOptionsField?.GetValue(optionsAnalyzerConfigOptions) as ImmutableDictionary<string, string>;
            if (optionsCollection != null)
            {
                values = new Dictionary<string, string>(optionsCollection);
                System.Diagnostics.Debug.WriteLine($"[EditorConfigOptions] Extracted {values.Count} EditorConfig values");
                foreach (var kvp in values)
                {
                    System.Diagnostics.Debug.WriteLine($"[EditorConfigOptions] Key: '{kvp.Key}' = '{kvp.Value}'");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[EditorConfigOptions] No options collection found");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to extract EditorConfig values: {ex.Message}");
        }

        return values;
    }
}