#nullable disable
using Reqnroll.VisualStudio.Snippets;
using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Reqnroll.VisualStudio.UI.ViewModels;

public class CreateStepDefinitionsDialogViewModel : INotifyPropertyChanged
{
#if DEBUG
    public static CreateStepDefinitionsDialogViewModel DesignData = new()
    {
        ClassName = "MyFeatureSteps",
        ExpressionStyle = SnippetExpressionStyle.CucumberExpression,
        Items = new ObservableCollection<StepDefinitionSnippetItemViewModel>
        {
            new()
            {
                Snippet = @"[Given(@""there is a simple Reqnroll project for (.*)"")]
public void GivenThereIsASimpleReqnrollProjectForVersion(Version reqnrollVersion)
{
    throw new PendingStepException();
}"
            },
            new()
            {
                Snippet = @"[When(@""there is a simple Reqnroll project for (.*)"")]
public void GivenThereIsASimpleReqnrollProjectForVersion(Version reqnrollVersion)
{
    throw new PendingStepException();
}"
            },
            new()
            {
                Snippet = @"[When(@""there is a simple Reqnroll project for (.*)"")]
public void GivenThereIsASimpleReqnrollProjectForVersion(Version reqnrollVersion)
{
    throw new PendingStepException();
}"
            }
        }
    };
#endif

    public string ClassName { get; set; }
    public SnippetExpressionStyle ExpressionStyle { get; set; }
    private bool _generateAsyncMethods;
    public bool GenerateAsyncMethods
    {
        get => _generateAsyncMethods;
        set
        {
            if (_generateAsyncMethods != value)
            {
                _generateAsyncMethods = value;
                OnPropertyChanged(nameof(GenerateAsyncMethods));
                if (!IsInitializing)
                {
                    RegenerateItems(); // Regenerate when property changes
                }
            }
        }
    }
    public ObservableCollection<StepDefinitionSnippetItemViewModel> Items { get; set; } = new();
    public CreateStepDefinitionsDialogResult Result { get; set; }
    public Func<CreateStepDefinitionsDialogViewModel, IEnumerable<StepDefinitionSnippetItemViewModel>> Generator { get; set; }
    public DeveroomTag[] UndefinedStepTags { get; set; }
    public SnippetService SnippetService { get; set; }
    public string Indent { get; set; }
    public string NewLine { get; set; }

    public bool IsInitializing;

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void RegenerateItems()
    {
        Items.Clear();
        foreach (var item in Generator(this))
        {
            Items.Add(item);
        }
        OnPropertyChanged(nameof(Items));
    }
}
