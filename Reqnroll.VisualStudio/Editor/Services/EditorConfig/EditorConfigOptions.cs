#nullable disable
using Microsoft.CodeAnalysis.Options;

namespace Reqnroll.VisualStudio.Editor.Services.EditorConfig;

public class EditorConfigOptions : IEditorConfigOptions
{
    private readonly DocumentOptionSet _options;

    public EditorConfigOptions(DocumentOptionSet options)
    {
        _options = options;
    }

    public TResult GetOption<TResult>(string editorConfigKey, TResult defaultValue)
    {
        var storageLocation = CreateStorageLocation<TResult>(editorConfigKey);
        if (storageLocation == null)
            return defaultValue;
        return _options.GetOption(new Option<TResult>("reqnroll.vs", editorConfigKey, defaultValue, storageLocation));
    }

    private OptionStorageLocation CreateStorageLocation<TResult>(string editorConfigKey)
    {
        var supportedTypes = new[] {typeof(bool), typeof(string), typeof(int)};
        if (!supportedTypes.Contains(typeof(TResult)))
            throw new NotSupportedException($"Editor config setting type {typeof(TResult).Name} is not supported.");
        var typeName = typeof(TResult) == typeof(bool) ? "Bool" : typeof(TResult).Name;
        return
            typeof(OptionSet).Assembly.GetType("Microsoft.CodeAnalysis.Options.EditorConfigStorageLocation", false)?
                    .GetMethod($"For{typeName}Option", BindingFlags.Public | BindingFlags.Static)?
                    .Invoke(null, new object[] {editorConfigKey})
                as OptionStorageLocation;
    }
}
