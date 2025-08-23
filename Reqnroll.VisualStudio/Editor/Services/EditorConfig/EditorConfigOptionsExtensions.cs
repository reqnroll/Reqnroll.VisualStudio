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
            var updatedValue = editorConfigOptions.GetOption(property.PropertyInfo.PropertyType,
                property.EditorConfigKey, currentValue);
            if (!Equals(currentValue, updatedValue))
                property.PropertyInfo.SetValue(config, updatedValue);
        }
    }
}
