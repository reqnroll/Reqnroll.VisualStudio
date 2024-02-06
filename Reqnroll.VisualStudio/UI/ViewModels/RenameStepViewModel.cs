#nullable disable

namespace Reqnroll.VisualStudio.UI.ViewModels;

public class RenameStepViewModel : INotifyPropertyChanged
{
    private readonly Func<string, ImmutableHashSet<string>> _validateFunc;

    private string _stepText;

    private string _validationError = string.Empty;

    public RenameStepViewModel(ProjectStepDefinitionBinding binding,
        Func<string, ImmutableHashSet<string>> validateFunc)
    {
        _validateFunc = validateFunc;
        StepText = binding.Expression;
        OriginalStepText = binding.ToString();
    }

    public bool IsValid => string.IsNullOrEmpty(ValidationError);

    public string ValidationError
    {
        get => _validationError;
        set
        {
            if (value == _validationError) return;
            _validationError = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsValid));
        }
    }

    public string StepText
    {
        get => _stepText;
        set
        {
            _stepText = value;
            Validate();
        }
    }

    public string OriginalStepText { get; }

    public AnalyzedStepDefinitionExpression ParsedUpdatedExpression { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void Validate()
    {
        var errors = _validateFunc(StepText);
        ValidationError = string.Join(Environment.NewLine, errors);
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

#if DEBUG
    public static RenameStepViewModel DesignData = new(
        new ProjectStepDefinitionBinding(
            ScenarioBlock.Given,
            new Regex("^invalid$"),
            new Scope(),
            new ProjectBindingImplementation(
                "WhenIPressAdd",
                Array.Empty<string>(),
                new SourceLocation("Steps.cs", 10, 9))
        ), StubValidation);

    public static ImmutableHashSet<string> StubValidation(string updatedExpression) =>
        updatedExpression == "invalid"
            ? ImmutableHashSet.Create("This is wrong")
            : ImmutableHashSet<string>.Empty;
#endif
}
