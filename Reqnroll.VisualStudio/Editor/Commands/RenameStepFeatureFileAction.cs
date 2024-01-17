namespace Reqnroll.VisualStudio.Editor.Commands;

internal class RenameStepFeatureFileAction : RenameStepAction
{
    public override async Task PerformRenameStep(RenameStepCommandContext ctx)
    {
        IIdeScope ideScope = ctx.ProjectOfStepDefinitionClass.IdeScope;
        var stepDefinitionUsageFinder = new StepDefinitionUsageFinder(ideScope);
        string[]? featureFiles = ctx.ProjectOfStepDefinitionClass.GetProjectFiles(".feature");
        var configuration = ctx.ProjectOfStepDefinitionClass.GetDeveroomConfiguration();

        StepDefinitionUsage[] projectUsages = stepDefinitionUsageFinder
            .FindUsages(new[] {ctx.StepDefinitionBinding}, featureFiles, configuration).ToArray();
        foreach (var fileUsage in projectUsages.GroupBy(u => u.SourceLocation.SourceFile))
        {
            var firstPosition = fileUsage.First().SourceLocation;
            EnsureFeatureFileOpen(firstPosition, ideScope);
            if (ideScope.GetTextBuffer(firstPosition, out var textBufferOfFeatureFile))
                await EditTextBuffer(textBufferOfFeatureFile, ctx.IdeScope, fileUsage,
                    usage => CalculateReplaceSpan((textBufferOfFeatureFile, usage)),
                    usage => CalculateReplacementText((textBufferOfFeatureFile, usage), ctx.AnalyzedUpdatedExpression,
                        fileUsage.Key, ctx));
            else
                ctx.AddCriticalProblem($"Could not access {firstPosition.SourceFile}");
        }
    }

    private string CalculateReplacementText((ITextBuffer textBufferOfFeatureFile, StepDefinitionUsage usage) from,
        AnalyzedStepDefinitionExpression updatedExpression, string filePath,
        RenameStepCommandContext renameStepCommandContext)
    {
        ParameterMatch parameterMatch = FindParameterMatch(from, filePath, renameStepCommandContext);

        if (parameterMatch == ParameterMatch.NotMatch) return from.usage.Step.Text;

        var resultText = new StringBuilder();
        var text = from.usage.Step.Text;
        for (int i = 0; i < parameterMatch.StepTextParameters.Length; i++)
        {
            resultText.Append(GetUnescapedText(updatedExpression.Parts[i * 2]));
            resultText.Append(text.Substring(parameterMatch.StepTextParameters[i].Index,
                parameterMatch.StepTextParameters[i].Length));
        }

        resultText.Append(GetUnescapedText(updatedExpression.Parts.Last()));

        return resultText.ToString();
    }

    private static ParameterMatch FindParameterMatch(
        (ITextBuffer textBufferOfFeatureFile, StepDefinitionUsage usage) from,
        string filePath, RenameStepCommandContext ctx)
    {
        var snapshotSpan = new SnapshotSpan(from.textBufferOfFeatureFile.CurrentSnapshot, CalculateReplaceSpan(from));
        var tagger = ctx.TaggerProvider.CreateTagger<DeveroomTag>(from.textBufferOfFeatureFile);
        var deveroomTagsForSpan = tagger.GetUpToDateDeveroomTagsForSpan(snapshotSpan).ToList();
        DeveroomTag matchedStepTag =
            deveroomTagsForSpan
                .Where(t => t.Tag.Type == DeveroomTagTypes.DefinedStep)
                .Select(t => t.Tag)
                .DefaultIfEmpty(new DeveroomTag(DeveroomTagTypes.DefinedStep, new SnapshotSpan()))
                .Single();

        if (matchedStepTag.Data is not MatchResult matchResult)
            return ParameterMatch.NotMatch;

        ParameterMatch parameterMatch = matchResult.Items.Single().ParameterMatch;

        if (HasScenarioOutlinePlaceholder(matchedStepTag))
        {
            ctx.AddNotificationProblem(
                $"{filePath}({from.usage.SourceLocation.SourceFileLine},{from.usage.SourceLocation.SourceFileColumn}): " +
                $"Could not rename scenario outline step with placeholders: {from.usage.Step.Text}");

            return ParameterMatch.NotMatch;
        }

        return parameterMatch;
    }

    private static bool HasScenarioOutlinePlaceholder(DeveroomTag matchedStepTag) => matchedStepTag.ParentTag
        .GetDescendantsOfType(DeveroomTagTypes.ScenarioOutlinePlaceholder).Any();

    private string GetUnescapedText(AnalyzedStepDefinitionExpressionPart part)
    {
        if (part is AnalyzedStepDefinitionExpressionSimpleTextPart simpleTextPart)
            return simpleTextPart.UnescapedText;
        return part.ExpressionText;
    }

    protected void EnsureFeatureFileOpen(SourceLocation sourceLocation, IIdeScope ideScope)
    {
        ideScope.OpenIfNotOpened(sourceLocation.SourceLocationSpan?.FilePath ?? sourceLocation.SourceFile);
    }

    private static Span CalculateReplaceSpan((ITextBuffer textBuffer, StepDefinitionUsage usage) from)
    {
        var line = from.textBuffer.CurrentSnapshot.GetLineFromLineNumber(from.usage.SourceLocation.SourceFileLine - 1);
        var indentPlusKeywordLength = from.usage.SourceLocation.SourceFileColumn - 1 + from.usage.Step.Keyword.Length;
        var startPosition = line.Start.Position + indentPlusKeywordLength;
        var replaceSpan = new Span(startPosition, line.End.Position - startPosition);
        return replaceSpan;
    }
}
