using Location = Gherkin.Ast.Location;

namespace Reqnroll.VisualStudio.Editor.Services;

public class DeveroomTagParser : IDeveroomTagParser
{
    internal static readonly Regex NewLineRe = new(@"\r\n|\n|\r");
    private readonly IDeveroomConfigurationProvider _deveroomConfigurationProvider;
    private readonly IDiscoveryService _discoveryService;
    private readonly IDeveroomLogger _logger;
    private readonly IMonitoringService _monitoringService;

    public DeveroomTagParser(
        IDeveroomLogger logger,
        IMonitoringService monitoringService,
        IDeveroomConfigurationProvider deveroomConfigurationProvider,
        IDiscoveryService discoveryService
    )
    {
        _logger = logger;
        _monitoringService = monitoringService;
        _deveroomConfigurationProvider = deveroomConfigurationProvider;
        _discoveryService = discoveryService;
    }

    public IReadOnlyCollection<DeveroomTag> Parse(ITextSnapshot fileSnapshot)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        try
        {
            var configuration = _deveroomConfigurationProvider.GetConfiguration();
            var bindingRegistry = _discoveryService.BindingRegistryCache.Value;
            return ParseInternal(fileSnapshot, bindingRegistry, configuration);
        }
        catch (Exception ex)
        {
            _logger.LogException(_monitoringService, ex, "Unhandled parsing error");
            return Array.Empty<DeveroomTag>();
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogVerbose(
                $"Parsed buffer v{fileSnapshot.Version.VersionNumber} in {stopwatch.ElapsedMilliseconds}ms on thread {Thread.CurrentThread.ManagedThreadId}");
        }
    }

    private IReadOnlyCollection<DeveroomTag> ParseInternal(ITextSnapshot fileSnapshot,
        ProjectBindingRegistry bindingRegistry,
        DeveroomConfiguration deveroomConfiguration)
    {
        var dialectProvider = ReqnrollGherkinDialectProvider.Get(deveroomConfiguration.DefaultFeatureLanguage);
        var parser = new DeveroomGherkinParser(dialectProvider, _monitoringService);

        parser.ParseAndCollectErrors(fileSnapshot.GetText(), _logger,
            out var gherkinDocument, out var parserErrors);

        ImmutableSortedSet<DeveroomTag>.Builder result =
            ImmutableSortedSet.CreateBuilder(new DeveroomTagPositionComparer());

        if (gherkinDocument != null)
            AddGherkinDocumentTags(fileSnapshot, bindingRegistry, gherkinDocument, result);

        foreach (var parserException in parserErrors)
        {
            var line = GetSnapshotLine(parserException.Location, fileSnapshot);
            var startPoint = GetColumnPoint(line, parserException.Location);
            var span = new SnapshotSpan(startPoint, line.End);

            var deveroomTag = new DeveroomTag(DeveroomTagTypes.ParserError,
                span, parserException.Message);
            result.Add(deveroomTag);
        }

        return result.ToImmutable();
    }

    private void AddGherkinDocumentTags(ITextSnapshot fileSnapshot, ProjectBindingRegistry bindingRegistry,
        DeveroomGherkinDocument gherkinDocument, ISet<DeveroomTag> result)
    {
        var documentTag = new DeveroomTag(DeveroomTagTypes.Document,
            new SnapshotSpan(fileSnapshot, 0, fileSnapshot.Length), gherkinDocument);
        result.Add(documentTag);

        if (gherkinDocument.Feature != null)
        {
            var featureTag = GetFeatureTags(fileSnapshot, bindingRegistry, gherkinDocument.Feature);
            var allTags = GetAllTags(featureTag);
            result.UnionWith(allTags);
        }

        if (gherkinDocument.Comments != null)
            foreach (var comment in gherkinDocument.Comments)
            {
                var deveroomTag = new DeveroomTag(DeveroomTagTypes.Comment,
                    GetTextSpan(fileSnapshot, comment.Location, comment.Text));
                result.Add(deveroomTag);
            }
    }

    private DeveroomTag GetFeatureTags(ITextSnapshot fileSnapshot, ProjectBindingRegistry bindingRegistry,
        Feature feature)
    {
        var featureTag = CreateDefinitionBlockTag(feature, DeveroomTagTypes.FeatureBlock, fileSnapshot,
            fileSnapshot.LineCount);

        foreach (var block in feature.Children)
            if (block is StepsContainer stepsContainer)
                AddScenarioDefinitionBlockTag(fileSnapshot, bindingRegistry, stepsContainer, featureTag);
            else if (block is Rule rule)
                AddRuleBlockTag(fileSnapshot, bindingRegistry, rule, featureTag);

        return featureTag;
    }

    private void AddRuleBlockTag(ITextSnapshot fileSnapshot, ProjectBindingRegistry bindingRegistry, Rule rule,
        DeveroomTag featureTag)
    {
        var lastStepsContainer = rule.StepsContainers().LastOrDefault();
        var lastLine = lastStepsContainer != null
            ? GetScenarioDefinitionLastLine(lastStepsContainer)
            : rule.Location.Line;
        var ruleTag = CreateDefinitionBlockTag(rule,
            DeveroomTagTypes.RuleBlock, fileSnapshot,
            lastLine, featureTag);

        foreach (var stepsContainer in rule.StepsContainers())
            AddScenarioDefinitionBlockTag(fileSnapshot, bindingRegistry, stepsContainer, ruleTag);
    }

    private void AddScenarioDefinitionBlockTag(ITextSnapshot fileSnapshot, ProjectBindingRegistry bindingRegistry,
        StepsContainer scenarioDefinition, DeveroomTag parentTag)
    {
        var scenarioDefinitionTag = CreateDefinitionBlockTag(scenarioDefinition,
            DeveroomTagTypes.ScenarioDefinitionBlock, fileSnapshot,
            GetScenarioDefinitionLastLine(scenarioDefinition), parentTag);

        foreach (var step in scenarioDefinition.Steps)
        {
            var stepTag = scenarioDefinitionTag.AddChild(new DeveroomTag(DeveroomTagTypes.StepBlock,
                GetBlockSpan(fileSnapshot, step.Location, GetStepLastLine(step)), step));

            stepTag.AddChild(
                new DeveroomTag(DeveroomTagTypes.StepKeyword,
                    GetTextSpan(fileSnapshot, step.Location, step.Keyword),
                    step.Keyword));

            if (step.Argument is DataTable dataTable)
            {
                var dataTableBlockTag = new DeveroomTag(DeveroomTagTypes.DataTable,
                    GetBlockSpan(fileSnapshot, dataTable.Rows.First().Location,
                        dataTable.Rows.Last().Location.Line),
                    dataTable);
                stepTag.AddChild(dataTableBlockTag);
                var dataTableHeader = dataTable.Rows.FirstOrDefault();
                if (dataTableHeader != null)
                    TagRowCells(fileSnapshot, dataTableHeader, dataTableBlockTag, DeveroomTagTypes.DataTableHeader);
            }
            else if (step.Argument is DocString docString)
            {
                stepTag.AddChild(
                    new DeveroomTag(DeveroomTagTypes.DocString,
                        GetBlockSpan(fileSnapshot, docString.Location,
                            GetStepLastLine(step)),
                        docString));
            }

            if (scenarioDefinition is ScenarioOutline) AddPlaceholderTags(fileSnapshot, stepTag, step);

            if (bindingRegistry == ProjectBindingRegistry.Invalid)
                continue;

            var match = bindingRegistry.MatchStep(step, scenarioDefinitionTag);
            if (match.HasDefined || match.HasAmbiguous)
            {
                stepTag.AddChild(new DeveroomTag(DeveroomTagTypes.DefinedStep,
                    GetTextSpan(fileSnapshot, step.Location, step.Text, offset: step.Keyword.Length),
                    match));
                if (!(scenarioDefinition is ScenarioOutline) || !step.Text.Contains("<"))
                {
                    var parameterMatch = match.Items.First(m => m.ParameterMatch != null).ParameterMatch;
                    AddParameterTags(fileSnapshot, parameterMatch, stepTag, step);
                }
            }

            if (match.HasUndefined)
                stepTag.AddChild(new DeveroomTag(DeveroomTagTypes.UndefinedStep,
                    GetTextSpan(fileSnapshot, step.Location, step.Text, offset: step.Keyword.Length),
                    match));

            if (match.HasErrors)
                stepTag.AddChild(new DeveroomTag(DeveroomTagTypes.BindingError,
                    GetTextSpan(fileSnapshot, step.Location, step.Text, offset: step.Keyword.Length),
                    match.GetErrorMessage()));
        }

        if (scenarioDefinition is ScenarioOutline scenarioOutline)
            foreach (var scenarioOutlineExample in scenarioOutline.Examples)
            {
                var examplesBlockTag = CreateDefinitionBlockTag(scenarioOutlineExample,
                    DeveroomTagTypes.ExamplesBlock, fileSnapshot,
                    GetExamplesLastLine(scenarioOutlineExample), scenarioDefinitionTag);
                if (scenarioOutlineExample.TableHeader != null)
                    TagRowCells(fileSnapshot, scenarioOutlineExample.TableHeader, examplesBlockTag,
                        DeveroomTagTypes.ScenarioOutlinePlaceholder);
            }

        if (scenarioDefinition is Scenario scenario && bindingRegistry != ProjectBindingRegistry.Invalid)
        {
            var match = bindingRegistry.MatchScenarioToHooks(scenario, scenarioDefinitionTag);
            if (match.HasHooks)
            {
                var firstTagTag = scenarioDefinitionTag
                    .GetDescendantsOfType(DeveroomTagTypes.Tag)
                    .OrderBy(t => t.Span.Start)
                    .FirstOrDefault();

                var startTag = firstTagTag ?? scenarioDefinitionTag;
                var span = new SnapshotSpan(startTag.Span.Start, scenarioDefinitionTag.Span.End);

                var hookReferenceTag = new DeveroomTag(DeveroomTagTypes.ScenarioHookReference, span, match);
                scenarioDefinitionTag.AddChild(hookReferenceTag);
            }
        }
    }

    private void TagRowCells(ITextSnapshot fileSnapshot, TableRow row, DeveroomTag parentTag, string tagType)
    {
        foreach (var cell in row.Cells)
            parentTag.AddChild(new DeveroomTag(tagType,
                GetSpan(fileSnapshot, cell.Location, cell.Value.Length),
                cell));
    }

    private void AddParameterTags(ITextSnapshot fileSnapshot, ParameterMatch parameterMatch, DeveroomTag stepTag,
        Step step)
    {
        foreach (var parameter in parameterMatch.StepTextParameters)
            stepTag.AddChild(new DeveroomTag(DeveroomTagTypes.StepParameter,
                GetSpan(fileSnapshot, step.Location, parameter.Length, step.Keyword.Length + parameter.Index),
                parameter));
    }

    private void AddPlaceholderTags(ITextSnapshot fileSnapshot, DeveroomTag stepTag, Step step)
    {
        var placeholders = MatchedScenarioOutlinePlaceholder.MatchScenarioOutlinePlaceholders(step);
        foreach (var placeholder in placeholders)
            stepTag.AddChild(new DeveroomTag(DeveroomTagTypes.ScenarioOutlinePlaceholder,
                GetSpan(fileSnapshot, step.Location, placeholder.Length, step.Keyword.Length + placeholder.Index),
                placeholder));
    }

    private DeveroomTag CreateDefinitionBlockTag(IHasDescription astNode, string tagType, ITextSnapshot fileSnapshot,
        int lastLine)
        => CreateDefinitionBlockTag(astNode, tagType, fileSnapshot, lastLine, VoidDeveroomTag.Instance);

    private DeveroomTag CreateDefinitionBlockTag(IHasDescription astNode, string tagType, ITextSnapshot fileSnapshot,
        int lastLine, DeveroomTag parentTag)
    {
        var span = GetBlockSpan(fileSnapshot, ((IHasLocation) astNode).Location, lastLine);
        var blockTag = new DeveroomTag(tagType, span, astNode);
        parentTag.AddChild(blockTag);
        blockTag.AddChild(CreateDefinitionLineKeyword(fileSnapshot, astNode));
        if (astNode is IHasTags hasTags)
            foreach (var gherkinTag in hasTags.Tags)
                blockTag.AddChild(
                    new DeveroomTag(DeveroomTagTypes.Tag,
                        GetTextSpan(fileSnapshot, gherkinTag.Location, gherkinTag.Name),
                        gherkinTag));

        if (!string.IsNullOrEmpty(astNode.Description))
        {
            var startLineNumber = ((IHasLocation) astNode).Location.Line + 1;
            while (string.IsNullOrWhiteSpace(fileSnapshot
                       .GetLineFromLineNumber(GetSnapshotLineNumber(startLineNumber, fileSnapshot)).GetText()))
                startLineNumber++;
            blockTag.AddChild(
                new DeveroomTag(DeveroomTagTypes.Description,
                    GetBlockSpan(fileSnapshot, startLineNumber,
                        CountLines(astNode.Description))));
        }

        return blockTag;
    }

    private int CountLines(string text) => NewLineRe.Matches(text).Count + 1;

    private DeveroomTag CreateDefinitionLineKeyword(ITextSnapshot fileSnapshot, IHasDescription hasDescription) =>
        new(DeveroomTagTypes.DefinitionLineKeyword,
            GetTextSpan(fileSnapshot, ((IHasLocation) hasDescription).Location, hasDescription.Keyword, 1));

    private IEnumerable<DeveroomTag> GetAllTags(DeveroomTag tag)
    {
        yield return tag;
        foreach (var childTag in tag.ChildTags)
        foreach (var allChildTag in GetAllTags(childTag))
            yield return allChildTag;
    }

    private int GetScenarioDefinitionLastLine(StepsContainer stepsContainer)
    {
        if (stepsContainer is ScenarioOutline scenarioOutline)
        {
            var lastExamples = scenarioOutline.Examples.LastOrDefault();
            if (lastExamples != null) return GetExamplesLastLine(lastExamples);
        }

        var lastStep = stepsContainer.Steps.LastOrDefault();
        if (lastStep == null)
            return stepsContainer.Location.Line;
        return GetStepLastLine(lastStep);
    }

    private static int GetExamplesLastLine(Examples examples)
    {
        var lastRow = examples.TableBody?.LastOrDefault() ?? examples.TableHeader;
        if (lastRow != null)
            return lastRow.Location.Line;
        return examples.Location.Line;
    }

    private int GetStepLastLine(Step step)
    {
        if (step.Argument is DocString docStringArg)
        {
            int lineCount = CountLines(docStringArg.Content);
            return docStringArg.Location.Line + lineCount - 1 + 2;
        }

        if (step.Argument is DataTable dataTable) return dataTable.Rows.Last().Location.Line;
        return step.Location.Line;
    }

    private SnapshotSpan GetBlockSpan(ITextSnapshot snapshot, Location startLocation, int locationEndLine)
    {
        var startLine = GetSnapshotLine(startLocation, snapshot);
        var endLine = snapshot.GetLineFromLineNumber(GetSnapshotLineNumber(locationEndLine, snapshot));

        return new SnapshotSpan(startLine.Start, endLine.End);
    }

    private SnapshotSpan GetBlockSpan(ITextSnapshot snapshot, int startLineNumber, int lineCount)
    {
        var startLine = snapshot.GetLineFromLineNumber(GetSnapshotLineNumber(startLineNumber, snapshot));
        var endLine = snapshot.GetLineFromLineNumber(GetSnapshotLineNumber(startLineNumber + lineCount - 1, snapshot));

        return new SnapshotSpan(startLine.Start, endLine.End);
    }

    private SnapshotSpan GetTextSpan(ITextSnapshot snapshot, Location location, string text, int extraLength = 0,
        int offset = 0) =>
        GetSpan(snapshot, location, text.Length + extraLength, offset);

    private SnapshotSpan GetSpan(ITextSnapshot snapshot, Location location, int length, int offset = 0)
    {
        var line = GetSnapshotLine(location, snapshot);
        var startPoint = GetColumnPoint(line, location);
        if (offset != 0)
            startPoint = startPoint.Add(offset);
        return new SnapshotSpan(startPoint, length);
    }

    private int GetSnapshotLineNumber(Location location, ITextSnapshot snapshot) =>
        GetSnapshotLineNumber(location.Line, snapshot);

    private int GetSnapshotLineNumber(int locationLine, ITextSnapshot snapshot) =>
        locationLine == 0
            ? 0 // global error
            : locationLine - 1 >= snapshot.LineCount
                ? snapshot.LineCount - 1 // unexpected end of file
                : locationLine - 1;

    private int GetSnapshotColumn(Location location) =>
        location.Column == 0
            ? 0 // whole line error
            : location.Column - 1;

    private SnapshotPoint GetColumnPoint(ITextSnapshotLine line, Location location) =>
        line.Start.Add(GetSnapshotColumn(location));

    private ITextSnapshotLine GetSnapshotLine(Location location, ITextSnapshot snapshot) =>
        snapshot.GetLineFromLineNumber(GetSnapshotLineNumber(location, snapshot));
}
