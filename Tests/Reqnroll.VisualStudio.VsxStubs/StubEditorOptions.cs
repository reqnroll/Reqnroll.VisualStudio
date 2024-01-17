using System;

namespace Reqnroll.VisualStudio.VsxStubs;

public class StubEditorOptions : IEditorOptions
{
    public bool ReplicateNewLineCharacterOption { get; set; } = false;
    public string NewLineCharacterOption { get; set; } = Environment.NewLine;

    public T GetOptionValue<T>(string optionId) => throw new NotImplementedException();

    public T GetOptionValue<T>(EditorOptionKey<T> key)
    {
        if (key.Equals(DefaultWpfViewOptions.EnableSimpleGraphicsId))
            return (T) (object) true;
        if (key.Equals(DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionId))
            return (T) (object) true;
        if (key.Equals(DefaultOptions.ReplicateNewLineCharacterOptionId))
            return (T) (object) ReplicateNewLineCharacterOption;
        if (key.Equals(DefaultOptions.NewLineCharacterOptionId))
            return (T) (object) NewLineCharacterOption;
        if (key.Equals(DefaultOptions.ConvertTabsToSpacesOptionId))
            return (T) (object) true;
        if (key.Equals(DefaultOptions.IndentSizeOptionId))
            return (T) (object) 4;
        throw new NotImplementedException(key.ToString());
    }

    public object GetOptionValue(string optionId) => throw new NotImplementedException();

    public void SetOptionValue(string optionId, object value)
    {
        throw new NotImplementedException();
    }

    public void SetOptionValue<T>(EditorOptionKey<T> key, T value)
    {
        throw new NotImplementedException();
    }

    public bool IsOptionDefined(string optionId, bool localScopeOnly) => throw new NotImplementedException();

    public bool IsOptionDefined<T>(EditorOptionKey<T> key, bool localScopeOnly) => throw new NotImplementedException();

    public bool ClearOptionValue(string optionId) => throw new NotImplementedException();

    public bool ClearOptionValue<T>(EditorOptionKey<T> key) => throw new NotImplementedException();

    public IEnumerable<EditorOptionDefinition> SupportedOptions => throw new NotImplementedException();

    public IEditorOptions GlobalOptions => throw new NotImplementedException();

    public IEditorOptions Parent
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public event EventHandler<EditorOptionChangedEventArgs>? OptionChanged;
}
