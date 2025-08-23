#nullable disable

namespace Reqnroll.VisualStudio.Editor.Services.EditorConfig;

public static class EditorConfigOptionsExtensions
{
    private static object GetOption(this IEditorConfigOptions editorConfigOptions, Type optionType,
        string editorConfigKey, object defaultValue)
    {
        if (editorConfigOptions == null) throw new ArgumentNullException(nameof(editorConfigOptions));
        if (optionType == null) throw new ArgumentNullException(nameof(optionType));
        if (editorConfigKey == null) throw new ArgumentNullException(nameof(editorConfigKey));

        var method = typeof(IEditorConfigOptions)
            .GetMethod(nameof(IEditorConfigOptions.GetOption))!
            .MakeGenericMethod(optionType);
        return method.Invoke(editorConfigOptions, new[] {editorConfigKey, defaultValue});
    }

    public static void UpdateFromEditorConfig<TConfig>(this IEditorConfigOptions editorConfigOptions, TConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        System.Diagnostics.Debug.WriteLine($"[UpdateFromEditorConfig] Updating configuration for type: {typeof(TConfig).Name}");

        var propertiesWithEditorConfig = typeof(TConfig)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => new
            {
                PropertyInfo = p,
                EditorConfigKey = ((EditorConfigSettingAttribute) Attribute.GetCustomAttribute(p, typeof(EditorConfigSettingAttribute)))
                    ?.EditorConfigSettingName
            })
            .Where(p => p.EditorConfigKey != null);

        foreach (var property in propertiesWithEditorConfig)
        {
            var currentValue = property.PropertyInfo.GetValue(config);
            System.Diagnostics.Debug.WriteLine($"[UpdateFromEditorConfig] Property: {property.PropertyInfo.Name}, EditorConfigKey: {property.EditorConfigKey}, CurrentValue: {currentValue}");
            
            var updatedValue = editorConfigOptions.GetOption(property.PropertyInfo.PropertyType,
                property.EditorConfigKey, currentValue);
            
            System.Diagnostics.Debug.WriteLine($"[UpdateFromEditorConfig] UpdatedValue: {updatedValue}");
            
            if (!Equals(currentValue, updatedValue))
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateFromEditorConfig] Setting property {property.PropertyInfo.Name} from '{currentValue}' to '{updatedValue}'");
                property.PropertyInfo.SetValue(config, updatedValue);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateFromEditorConfig] Property {property.PropertyInfo.Name} unchanged: '{currentValue}'");
            }
        }
    }
}
