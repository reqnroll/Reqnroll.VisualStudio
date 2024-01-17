namespace Reqnroll.VisualStudio.Editor.Services.Formatting;

public class GherkinFormatSettings
{
    public GherkinFormatConfiguration Configuration { get; set; } = new();

    public string Indent { get; set; } = "    ";

    public int FeatureChildrenIndentLevel => Configuration.IndentFeatureChildren ? 1 : 0;

    public int RuleChildrenIndentLevelWithinRule => Configuration.IndentRuleChildren ? 1 : 0;

    public int StepIndentLevelWithinStepContainer => Configuration.IndentSteps ? 1 : 0;

    public int AndStepIndentLevelWithinSteps => Configuration.IndentAndSteps ? 1 : 0;

    public int DataTableIndentLevelWithinStep => Configuration.IndentDataTable ? 1 : 0;
    public int DocStringIndentLevelWithinStep => Configuration.IndentDocString ? 1 : 0;

    public int ExamplesBlockIndentLevelWithinScenarioOutline => Configuration.IndentExamples ? 1 : 0;

    public int ExamplesTableIndentLevelWithinExamplesBlock => Configuration.IndentExamplesTable ? 1 : 0;

    public string TableCellPadding => new(' ', Configuration.TableCellPaddingSize);


    public static GherkinFormatSettings Load(IEditorConfigOptionsProvider editorConfigOptionsProvider,
        IWpfTextView textView, DeveroomConfiguration configuration)
    {
        var gherkinFormatConfiguration = configuration?.Editor?.GherkinFormat ?? new GherkinFormatConfiguration();

        var editorConfigOptions = editorConfigOptionsProvider.GetEditorConfigOptions(textView);
        editorConfigOptions.UpdateFromEditorConfig(gherkinFormatConfiguration);

        var editorOptions = textView.Options;
        var convertTabsToSpaces = editorOptions.GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId);
        var indentSize = editorOptions.GetOptionValue(DefaultOptions.IndentSizeOptionId);

        var formatSettings = new GherkinFormatSettings
        {
            Indent = convertTabsToSpaces ? new string(' ', indentSize) : new string('\t', 1),
            Configuration = gherkinFormatConfiguration
        };

        return formatSettings;
    }
}
