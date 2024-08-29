#nullable disable
using Location = Gherkin.Ast.Location;

namespace Reqnroll.VisualStudio.Editor.Services.Parser;

internal class DeveroomGherkinAstBuilder : AstBuilder<DeveroomGherkinDocument>, IAstBuilder<DeveroomGherkinDocument>
{
    private readonly Func<GherkinDialect> _documentDialectProvider;
    private readonly List<ParserException> _errors = new();
    private readonly string _sourceFilePath;
    private readonly List<int> _statesForLines = new();
    private ScenarioBlock _scenarioBlock = ScenarioBlock.Given;

    public DeveroomGherkinAstBuilder(string sourceFilePath, Func<GherkinDialect> documentDialectProvider)
    {
        _sourceFilePath = sourceFilePath;
        _documentDialectProvider = documentDialectProvider;
    }

    public IEnumerable<ParserException> Errors => _errors;

    public new void Build(Token token)
    {
        base.Build(token);

        if (token.MatchedType == TokenType.Language)
        {
            // the language token should be handled as comment as well
            var commentToken = new Token(token.Line, new Location(token.Location.Line, 1))
            {
                MatchedType = TokenType.Comment,
                MatchedText = token.Line.GetLineText()
            };
            base.Build(commentToken);
        }
    }

    private static StepKeyword GetStepKeyword(GherkinDialect dialect, string stepKeyword)
    {
        if (dialect.AndStepKeywords
            .Contains(stepKeyword)) // we need to check "And" first, as the '*' is also part of the Given, When and Then keywords
            return StepKeyword.And;
        if (dialect.GivenStepKeywords.Contains(stepKeyword))
            return StepKeyword.Given;
        if (dialect.WhenStepKeywords.Contains(stepKeyword))
            return StepKeyword.When;
        if (dialect.ThenStepKeywords.Contains(stepKeyword))
            return StepKeyword.Then;
        if (dialect.ButStepKeywords.Contains(stepKeyword))
            return StepKeyword.But;

        return StepKeyword.And;
    }

    protected override Step CreateStep(Location location, string keyword, StepKeywordType keywordType, string text, StepArgument argument,
        AstNode node)
    {
        var token = node.GetToken(TokenType.StepLine);
        var stepKeyword = GetStepKeyword(token.MatchedGherkinDialect, keyword);
        _scenarioBlock = stepKeyword.ToScenarioBlock() ?? _scenarioBlock;

        return new DeveroomGherkinStep(location, keyword, keywordType, text, argument, stepKeyword, _scenarioBlock);
    }

    private void ResetBlock()
    {
        _scenarioBlock = ScenarioBlock.Given;
    }

    protected override GherkinDocument CreateGherkinDocument(Feature feature, Comment[] gherkinDocumentComments,
        AstNode node) =>
        new DeveroomGherkinDocument(feature, gherkinDocumentComments, _sourceFilePath,
            _documentDialectProvider(), _statesForLines);

    protected override Scenario CreateScenario(Tag[] tags, Location location, string keyword, string name,
        string description, Step[] steps, Examples[] examples, AstNode node)
    {
        ResetBlock();
        if (examples == null || examples.Length == 0)
            return new SingleScenario(tags, location, keyword, name, description, steps, examples);
        return new ScenarioOutline(tags, location, keyword, name, description, steps, examples);
    }

    protected override Background CreateBackground(Location location, string keyword, string name, string description,
        Step[] steps, AstNode node)
    {
        ResetBlock();
        return base.CreateBackground(location, keyword, name, description, steps, node);
    }

    protected override void HandleAstError(string message, Location location)
    {
        _errors.Add(new SemanticParserException(message, location));
    }

    public void RecordStateForLine(int line, int state)
    {
        if (line > 0)
            line--; // convert to 0 based

        if (_statesForLines.Count > line)
            return;

        if (_statesForLines.Count < line)
        {
            var lastState = _statesForLines.Any() ? _statesForLines.Last() : -1;
            while (_statesForLines.Count < line)
                _statesForLines.Add(lastState);
        }

        _statesForLines.Add(state);
    }
}
