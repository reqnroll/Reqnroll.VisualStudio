#nullable disable
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;

namespace Reqnroll.VisualStudio.Editor.Classification;

internal static class DeveroomClassifications
{
    public const string Keyword = "deveroom.keyword";
    public const string Tag = "deveroom.tag";
    public const string Description = "deveroom.description";
    public const string Comment = "deveroom.comment";
    public const string DocString = "deveroom.doc_string";
    public const string DataTable = "deveroom.data_table";
    public const string DataTableHeader = "deveroom.data_table_header";

    public const string UndefinedStep = "deveroom.undefined_step";
    public const string StepParameter = "deveroom.step_parameter";
    public const string ScenarioOutlinePlaceholder = "deveroom.scenario_outline_placeholder";

    // This disables "The field is never used" compiler's warning. Justification: the field is used by MEF.
#pragma warning disable 169

    [Export] [Name(VsContentTypes.FeatureFile)] [BaseDefinition("text")]
    private static ContentTypeDefinition _typeDefinition;

    [Export] [FileExtension(".feature")] [ContentType(VsContentTypes.FeatureFile)]
    private static FileExtensionToContentTypeDefinition _fileExtensionToContentTypeDefinition;


    [Export] [Name(Keyword)] [BaseDefinition("keyword")]
    private static ClassificationTypeDefinition _keywordClassificationTypeDefinition;

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Keyword)]
    [Name(Keyword)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class GherkinKeywordClassificationFormat : ClassificationFormatDefinition
    {
        public GherkinKeywordClassificationFormat()
        {
            DisplayName = "Reqnroll Keyword";
        }
    }


    [Export] [Name(Tag)] [BaseDefinition("type")]
    private static ClassificationTypeDefinition _tagClassificationTypeDefinition;

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Tag)]
    [Name(Tag)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class GherkinTagClassificationFormat : ClassificationFormatDefinition
    {
        public GherkinTagClassificationFormat()
        {
            DisplayName = "Reqnroll Tag";
        }
    }


    [Export] [Name(Description)] [BaseDefinition("excluded code")]
    private static ClassificationTypeDefinition _descriptionClassificationTypeDefinition;

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Description)]
    [Name(Description)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class GherkinDescriptionClassificationFormat : ClassificationFormatDefinition
    {
        public GherkinDescriptionClassificationFormat()
        {
            DisplayName = "Reqnroll Description";
            IsItalic = true;
        }
    }


    [Export] [Name(DocString)] [BaseDefinition("string")]
    private static ClassificationTypeDefinition _docStringClassificationTypeDefinition;

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = DocString)]
    [Name(DocString)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class GherkinDocStringClassificationFormat : ClassificationFormatDefinition
    {
        public GherkinDocStringClassificationFormat()
        {
            DisplayName = "Reqnroll Doc String";
        }
    }


    [Export] [Name(DataTable)] [BaseDefinition("string")]
    private static ClassificationTypeDefinition _dataTableClassificationTypeDefinition;

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = DataTable)]
    [Name(DataTable)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class GherkinDataTableClassificationFormat : ClassificationFormatDefinition
    {
        public GherkinDataTableClassificationFormat()
        {
            DisplayName = "Reqnroll Data Table";
        }
    }


    [Export] [Name(DataTableHeader)] [BaseDefinition(DataTable)]
    private static ClassificationTypeDefinition _dataTableHeaderClassificationTypeDefinition;

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = DataTableHeader)]
    [Name(DataTableHeader)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class GherkinDataTableHeaderClassificationFormat : ClassificationFormatDefinition
    {
        public GherkinDataTableHeaderClassificationFormat()
        {
            DisplayName = "Reqnroll Data Table Header";
            IsItalic = true;
        }
    }


    [Export] [Name(Comment)] [BaseDefinition("comment")]
    private static ClassificationTypeDefinition _commentClassificationTypeDefinition;

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Comment)]
    [Name(Comment)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class GherkinCommentClassificationFormat : ClassificationFormatDefinition
    {
        public GherkinCommentClassificationFormat()
        {
            DisplayName = "Reqnroll Comment";
        }
    }


    [Export] [Name(UndefinedStep)]
    private static ClassificationTypeDefinition _undefinedStepClassificationTypeDefinition;

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = UndefinedStep)]
    [Name(UndefinedStep)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class GherkinUndefinedStepClassificationFormat : ClassificationFormatDefinition
    {
        public GherkinUndefinedStepClassificationFormat()
        {
            DisplayName = "Reqnroll Undefined Step";
            ForegroundColor = (Color) ColorConverter.ConvertFromString("#887DBA");
        }
    }


    [Export] [Name(StepParameter)] [BaseDefinition("string")]
    private static ClassificationTypeDefinition _stepParameterClassificationTypeDefinition;

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = StepParameter)]
    [Name(StepParameter)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class GherkinStepParameterClassificationFormat : ClassificationFormatDefinition
    {
        public GherkinStepParameterClassificationFormat()
        {
            DisplayName = "Reqnroll Step Parameter";
        }
    }


    [Export] [Name(ScenarioOutlinePlaceholder)] [BaseDefinition("number")]
    private static ClassificationTypeDefinition _scenarioOutlinePlaceholderClassificationTypeDefinition;

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = ScenarioOutlinePlaceholder)]
    [Name(ScenarioOutlinePlaceholder)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class GherkinScenarioOutlinePlaceholderClassificationFormat : ClassificationFormatDefinition
    {
        public GherkinScenarioOutlinePlaceholderClassificationFormat()
        {
            DisplayName = "Reqnroll Scenario Outline Placeholder";
            IsItalic = true;
        }
    }


#if DEBUG
    public const string DebugMarker = "deveroom.debug_marker";

    [Export] [Name(DebugMarker)] private static ClassificationTypeDefinition _debugEditorClassificationTypeDefinition;

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = DebugMarker)]
    [Name(DebugMarker)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class DebugEditorClassificationFormat : ClassificationFormatDefinition
    {
        public DebugEditorClassificationFormat()
        {
            DisplayName = "Reqnroll Debugging Editor";
            BackgroundColor = Colors.Yellow;
        }
    }
#endif
#pragma warning restore 169
}
