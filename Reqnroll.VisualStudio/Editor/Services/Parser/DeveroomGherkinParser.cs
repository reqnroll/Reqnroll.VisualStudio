#nullable disable

using Location = Gherkin.Ast.Location;

namespace Reqnroll.VisualStudio.Editor.Services.Parser;

public class DeveroomGherkinParser
{
    private readonly IMonitoringService _monitoringService;
    private IAstBuilder<DeveroomGherkinDocument> _astBuilder;

    public DeveroomGherkinParser(IGherkinDialectProvider dialectProvider, IMonitoringService monitoringService)
    {
        _monitoringService = monitoringService;
        DialectProvider = dialectProvider;
    }

    public IGherkinDialectProvider DialectProvider { get; }
    internal DeveroomGherkinAstBuilder AstBuilder => _astBuilder as DeveroomGherkinAstBuilder;

    public bool ParseAndCollectErrors(string featureFileContent, IDeveroomLogger logger,
        out DeveroomGherkinDocument gherkinDocument, out List<ParserException> parserErrors)
    {
        var reader = new StringReader(featureFileContent);
        gherkinDocument = null;
        parserErrors = new List<ParserException>();
        try
        {
            gherkinDocument = Parse(reader, "foo.feature"); //TODO: remove unused path
            return true;
        }
        catch (ParserException parserException)
        {
            logger.LogVerbose($"ParserErrors: {parserException.Message}");
            gherkinDocument = GetResultOfInvalid();
            if (parserException is CompositeParserException compositeParserException)
                parserErrors.AddRange(compositeParserException.Errors);
            else
                parserErrors.Add(parserException);
        }
        catch (Exception e)
        {
            logger.LogException(_monitoringService, e, "Exception during Gherkin parsing");
            gherkinDocument = GetResult();
        }

        return false;
    }

    private DeveroomGherkinDocument GetResultOfInvalid()
    {
        // trying to "finish" open nodes by sending dummy <endrule> messages up to 5 levels of nesting
        for (int i = 0; i < 10; i++)
        {
            var result = GetResult();
            if (result != null)
                return result;

            try
            {
                AstBuilder.EndRule(RuleType.None);
            }
            catch (Exception)
            {
            }
        }

        return null;
    }

    public DeveroomGherkinDocument Parse(TextReader featureFileReader, string sourceFilePath)
    {
        var tokenScanner = (ITokenScanner) new HotfixTokenScanner(featureFileReader);
        var tokenMatcher = new TokenMatcher(DialectProvider);
        _astBuilder = new DeveroomGherkinAstBuilder(sourceFilePath, () => tokenMatcher.CurrentDialect);

        var parser = new InternalParser(_astBuilder, AstBuilder.RecordStateForLine, _monitoringService);
        var gherkinDocument = parser.Parse(tokenScanner, tokenMatcher);

        CheckSemanticErrors(gherkinDocument);

        return gherkinDocument;
    }

    public DeveroomGherkinDocument GetResult() => _astBuilder.GetResult();

    private class InternalParser : Parser<DeveroomGherkinDocument>
    {
        private readonly IMonitoringService _monitoringService;
        private readonly Action<int, int> _recordStateForLine;

        public InternalParser(IAstBuilder<DeveroomGherkinDocument> astBuilder, Action<int, int> recordStateForLine,
            IMonitoringService monitoringService)
            : base(astBuilder)
        {
            _recordStateForLine = recordStateForLine;
            _monitoringService = monitoringService;
        }

        public int NullMatchToken(int state, Token token) =>
            MatchToken(state, token, new ParserContext
            {
                Errors = new List<ParserException>(),
                TokenMatcher = new AllFalseTokenMatcher(),
                TokenQueue = new Queue<Token>(),
                TokenScanner = new NullTokenScanner()
            });

        protected override int MatchToken(int state, Token token, ParserContext context)
        {
            _recordStateForLine?.Invoke(token.Location.Line, state);
            try
            {
                return base.MatchToken(state, token, context);
            }
            catch (InvalidOperationException ex)
            {
                _monitoringService.MonitorError(ex);
                throw;
            }
        }
    }

    #region Semantic Errors

    protected virtual void CheckSemanticErrors(DeveroomGherkinDocument reqnrollDocument)
    {
        var errors = new List<ParserException>();

        errors.AddRange(((DeveroomGherkinAstBuilder) _astBuilder).Errors);

        if (reqnrollDocument?.Feature != null)
        {
            CheckForDuplicateScenarios(reqnrollDocument.Feature, errors);

            CheckForDuplicateExamples(reqnrollDocument.Feature, errors);

            CheckForMissingExamples(reqnrollDocument.Feature, errors);

            CheckForRulesPreSpecFlow31(reqnrollDocument.Feature, errors);
        }

        // collect
        if (errors.Count == 1)
            throw errors[0];
        if (errors.Count > 1)
            throw new CompositeParserException(errors.ToArray());
    }

    private void CheckForRulesPreSpecFlow31(Feature feature, List<ParserException> errors)
    {
        //TODO: Show error when Rule keyword is used in SpecFlow v3.0 or earlier
    }

    private void CheckForDuplicateScenarios(Feature feature, List<ParserException> errors)
    {
        // duplicate scenario name
        var duplicatedScenarios = feature.FlattenScenarioDefinitions().GroupBy(sd => sd.Name, sd => sd)
            .Where(g => g.Count() > 1).ToArray();
        errors.AddRange(
            duplicatedScenarios.Select(g =>
                new SemanticParserException(
                    $"Feature file already contains a scenario with name '{g.Key}'",
                    g.ElementAt(1).Location)));
    }

    private void CheckForDuplicateExamples(Feature feature, List<ParserException> errors)
    {
        foreach (var scenarioOutline in feature.FlattenScenarioDefinitions().OfType<ScenarioOutline>())
        {
            var duplicateExamples = scenarioOutline.Examples
                .Where(e => !string.IsNullOrWhiteSpace(e.Name))
                .Where(e => e.Tags.All(t => t.Name != "ignore"))
                .GroupBy(e => e.Name, e => e).Where(g => g.Count() > 1);

            foreach (var duplicateExample in duplicateExamples)
            {
                var message =
                    $"Scenario Outline '{scenarioOutline.Name}' already contains an example with name '{duplicateExample.Key}'";
                var semanticParserException =
                    new SemanticParserException(message, duplicateExample.ElementAt(1).Location);
                errors.Add(semanticParserException);
            }
        }
    }

    private void CheckForMissingExamples(Feature feature, List<ParserException> errors)
    {
        foreach (var scenarioOutline in feature.FlattenScenarioDefinitions().OfType<ScenarioOutline>())
            if (DoesntHavePopulatedExamples(scenarioOutline))
            {
                var message = $"Scenario Outline '{scenarioOutline.Name}' has no examples defined";
                var semanticParserException = new SemanticParserException(message, scenarioOutline.Location);
                errors.Add(semanticParserException);
            }
    }

    private static bool DoesntHavePopulatedExamples(ScenarioOutline scenarioOutline)
    {
        return !scenarioOutline.Examples.Any() ||
               scenarioOutline.Examples.Any(x => x.TableBody == null || !x.TableBody.Any());
    }

    #endregion

    #region Expected tokens

    private class NullAstBuilder : IAstBuilder<DeveroomGherkinDocument>
    {
        public void Build(Token token)
        {
        }

        public void StartRule(RuleType ruleType)
        {
        }

        public void EndRule(RuleType ruleType)
        {
        }

        public DeveroomGherkinDocument GetResult() => null;

        public void Reset()
        {
        }
    }

    private class AllFalseTokenMatcher : ITokenMatcher
    {
        public bool Match_EOF(Token token) => false;

        public bool Match_Empty(Token token) => false;

        public bool Match_Comment(Token token) => false;

        public bool Match_TagLine(Token token) => false;

        public bool Match_FeatureLine(Token token) => false;

        public bool Match_RuleLine(Token token) => false;

        public bool Match_BackgroundLine(Token token) => false;

        public bool Match_ScenarioLine(Token token) => false;

        public bool Match_ExamplesLine(Token token) => false;

        public bool Match_StepLine(Token token) => false;

        public bool Match_DocStringSeparator(Token token) => false;

        public bool Match_TableRow(Token token) => false;

        public bool Match_Language(Token token) => false;

        public bool Match_Other(Token token) => false;

        public void Reset()
        {
        }
    }

    private class NullTokenScanner : ITokenScanner
    {
        public Token Read() => new(null, new Location());
    }

    public static TokenType[] GetExpectedTokens(int state, IMonitoringService monitoringService)
    {
        var parser = new InternalParser(new NullAstBuilder(), null, monitoringService)
        {
            StopAtFirstError = true
        };

        try
        {
            parser.NullMatchToken(state, new Token(null, new Location()));
        }
        catch (UnexpectedEOFException ex)
        {
            return ex.ExpectedTokenTypes.Select(type => (TokenType) Enum.Parse(typeof(TokenType), type.TrimStart('#')))
                .ToArray();
        }

        return new TokenType[0];
    }

    #endregion
}
