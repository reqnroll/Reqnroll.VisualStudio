using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Editor.Services;

public static class DeveroomTagTypes
{
    public const string FeatureBlock = nameof(FeatureBlock);
    public const string RuleBlock = nameof(RuleBlock);
    public const string ScenarioDefinitionBlock = nameof(ScenarioDefinitionBlock);
    public const string ScenarioHookReference = nameof(ScenarioHookReference);
    public const string StepBlock = nameof(StepBlock);
    public const string ExamplesBlock = nameof(ExamplesBlock);
    public const string StepKeyword = nameof(StepKeyword);
    public const string DefinitionLineKeyword = nameof(DefinitionLineKeyword);
    public const string UndefinedStep = nameof(UndefinedStep);
    public const string DefinedStep = nameof(DefinedStep);
    public const string StepParameter = nameof(StepParameter);
    public const string ScenarioOutlinePlaceholder = nameof(ScenarioOutlinePlaceholder);
    public const string BindingError = nameof(BindingError);
    public const string DataTable = nameof(DataTable);
    public const string Tag = nameof(Tag);
    public const string Description = nameof(Description);
    public const string Comment = nameof(Comment);
    public const string DocString = nameof(DocString);
    public const string ParserError = nameof(ParserError);
    public const string Document = nameof(Document);
    public const string DataTableHeader = nameof(DataTableHeader);
}
