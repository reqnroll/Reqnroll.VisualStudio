namespace Reqnroll.VisualStudio.Editor.Services.EditorConfig;

internal class NullEditorConfigOptions : IEditorConfigOptions
{
    public static readonly NullEditorConfigOptions Instance = new();

    public TResult GetOption<TResult>(string editorConfigKey, TResult defaultValue)
        => defaultValue;

    public bool GetBoolOption(string editorConfigKey, bool defaultValue)
        => defaultValue;
}
