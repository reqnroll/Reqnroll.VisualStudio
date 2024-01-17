namespace Reqnroll.VisualStudio.Editor.Services.EditorConfig;

public interface IEditorConfigOptions
{
    TResult GetOption<TResult>(string editorConfigKey, TResult defaultValue);
}
