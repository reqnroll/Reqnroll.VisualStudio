using Location = Gherkin.Ast.Location;

namespace Reqnroll.VisualStudio.Editor.Services.Parser;

internal class ReqnrollGherkinDialectProvider : GherkinDialectProvider
{
    public ReqnrollGherkinDialectProvider(string defaultLanguage) : base(defaultLanguage)
    {
    }

    protected override bool TryGetDialect(string language, Location location, out GherkinDialect dialect)
    {
        if (language.Contains("-"))
        {
            if (base.TryGetDialect(language, location, out dialect))
                return true;

            var languageBase = language.Split('-')[0];
            if (!base.TryGetDialect(languageBase, location, out var languageBaseDialect))
                return false;

            dialect = new GherkinDialect(language, languageBaseDialect.FeatureKeywords,
                languageBaseDialect.RuleKeywords, languageBaseDialect.BackgroundKeywords,
                languageBaseDialect.ScenarioKeywords, languageBaseDialect.ScenarioOutlineKeywords,
                languageBaseDialect.ExamplesKeywords, languageBaseDialect.GivenStepKeywords,
                languageBaseDialect.WhenStepKeywords, languageBaseDialect.ThenStepKeywords,
                languageBaseDialect.AndStepKeywords, languageBaseDialect.ButStepKeywords);
            return true;
        }

        return base.TryGetDialect(language, location, out dialect);
    }

    public static GherkinDialect GetDialect(string language)
    {
        var provider = Get(language);
        return provider.DefaultDialect;
    }

    public static GherkinDialectProvider Get(string defaultLanguage) =>
        //TODO: cache!
        new ReqnrollGherkinDialectProvider(defaultLanguage);
}
