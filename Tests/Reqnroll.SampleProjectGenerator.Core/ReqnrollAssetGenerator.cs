#nullable disable
namespace Reqnroll.SampleProjectGenerator;

public class ReqnrollAssetGenerator
{
    private const string NumberPattern = @"(\d+)";
    private const string StringPattern = @"""(.*)""";

    private static readonly Dictionary<string, string> ParamTypes = new()
    {
        {NumberPattern, "int"},
        {StringPattern, "string"},
        {"DataTable", "Table"},
        {"DocString", "string"}
    };

    private readonly Dictionary<string, StepDef[]> _stepDefinitions;
    private readonly List<HookDef> _hooks;
    private StepDef _unicodeStep;

    public ReqnrollAssetGenerator(int stepDefinitionCount)
    {
        _stepDefinitions =
            GetStepDefinitionList(stepDefinitionCount)
                .GroupBy(sd => sd.Keyword)
                .ToDictionary(g => g.Key, g => g.ToArray());
        _hooks = new List<HookDef>();
    }

    public int StepDefCount => _stepDefinitions.Sum(g => g.Value.Length);
    public int StepCount { get; private set; }

    private IEnumerable<StepDef> GetStepDefinitionList(int stepDefinitionCount)
    {
        var result = new List<StepDef>();
        while (result.Count < stepDefinitionCount)
        {
            var paramList = new List<string>();
            var stepDef = new StepDef
            {
                Regex = string.Join(" ",
                    LoremIpsum.GetWords(LoremIpsum.Rnd.Next(3) + 3).Select(s => ConvertToParam(s, ref paramList))),
                DataTable = LoremIpsum.Rnd.Next(8) == 0,
                DocString = LoremIpsum.Rnd.Next(16) == 0,
                Keyword = GetKeyword(result.Count)
            };
            stepDef.StepTextParams = paramList;

            var sampleText = GetStepText(stepDef, null);
            if (result.Where(sd => sd.Keyword == stepDef.Keyword)
                .Any(sd => Regex.IsMatch(sampleText, "^" + sd.Regex + "$")))
                continue;

            result.Add(stepDef);
        }

        return result;
    }

    public void AddUnicodeSteps()
    {
        _unicodeStep = new StepDef
        {
            Keyword = "Given",
            Regex = GeneratorOptions.UnicodeBindingRegex,
            StepTextParams = new List<string>()
        };
        if (_stepDefinitions.ContainsKey("Given"))
            _stepDefinitions["Given"] = _stepDefinitions["Given"].Concat(new[] {_unicodeStep}).ToArray();
        else
            _stepDefinitions["Given"] = new[] {_unicodeStep};
    }

    private string GetKeyword(int stepDefCount)
    {
        switch (stepDefCount < 3 ? stepDefCount : LoremIpsum.Rnd.Next(3))
        {
            case 0:
                return "Given";
            case 1:
                return "When";
            case 2:
                return "Then";
        }

        throw new InvalidOperationException();
    }

    private string ConvertToParam(string word, ref List<string> paramList)
    {
        var r = LoremIpsum.Rnd.Next(10);
        if (r == 0)
        {
            paramList.Add(NumberPattern);
            return NumberPattern;
        }

        if (r == 1)
        {
            paramList.Add(StringPattern);
            return StringPattern;
        }

        return word;
    }

    private static string ToPascalCase(string value)
    {
        var camelCase = Regex.Replace(value, @"\s\w", match => match.Value.Trim().ToUpperInvariant());
        return ToTitle(camelCase);
    }

    private static string ToTitle(string value) => value.Substring(0, 1).ToUpperInvariant() + value.Substring(1);

    private string GetStepText(StepDef stepDef, string[] soHeaders)
    {
        var result = stepDef.Regex;
        result = result.Replace(NumberPattern, LoremIpsum.Rnd.Next(2009).ToString());
        result = Regex.Replace(result, Regex.Escape(StringPattern),
            me => "\"" + GetSOPlaceHolder(soHeaders, LoremIpsum.GetShortText(LoremIpsum.Rnd.Next(5) + 1)) + "\"");
        //result = result.Replace(StringPattern, "\"" + GetSOPlaceHolder(soHeaders, LoremIpsum.GetShortText(LoremIpsum.Rnd.Next(5) + 1)) + "\"");
        return result;
    }

    private string GetSOPlaceHolder(string[] soHeaders, string defaultValue)
    {
        if (soHeaders != null && LoremIpsum.Rnd.Next(3) > 0)
            return $"<{soHeaders[LoremIpsum.Rnd.Next(soHeaders.Length)]}>";
        return defaultValue;
    }

    public string GenerateFeatureFile(string folder, int scenarioCount, int scenarioOutlineCount = 0)
    {
        var featureTitle = ToTitle(LoremIpsum.GetShortText());
        var fileName = $"{ToPascalCase(featureTitle)}.feature";
        var filePath = Path.Combine(folder, fileName);
        File.WriteAllText(filePath, GenerateFeatureFileContent(scenarioCount, scenarioOutlineCount, featureTitle));
        return filePath;
    }

    public string GenerateFeatureFileContent(int scenarioCount, int scenarioOutlineCount = 0,
        string featureTitle = null)
    {
        var content = new StringBuilder();
        content.AppendLine($"Feature: {featureTitle ?? ToTitle(LoremIpsum.GetShortText())}");
        content.AppendLine();
        content.AppendLine(LoremIpsum.GetShortText());
        content.AppendLine(LoremIpsum.GetShortText());
        content.AppendLine();

        var scenarioDefs = LoremIpsum.Randomize(
            Enumerable.Range(0, scenarioCount).Select(i => "S")
                .Concat(Enumerable.Range(0, scenarioOutlineCount).Select(i => "O")));
        for (int i = 0; i < scenarioDefs.Length; i++)
            if (scenarioDefs[i] == "S")
                GenerateScenario(content);
            else
                GenerateScenarioOutline(content);

        return content.ToString();
    }

    private void GenerateScenarioOutline(StringBuilder content)
    {
        var headers = LoremIpsum.GetUniqueWords(3);

        content.AppendLine(LoremIpsum.GetShortText(LoremIpsum.Rnd.Next(3), "@"));
        content.AppendLine($"Scenario Outline: {ToTitle(LoremIpsum.GetShortText())}");
        AddSteps(content, headers);
        content.AppendLine($"Examples: {ToTitle(LoremIpsum.GetShortText())}");
        AppendTable(content, 3, headers);
        content.AppendLine();
    }

    private void GenerateScenario(StringBuilder content)
    {
        content.AppendLine(LoremIpsum.GetShortText(LoremIpsum.Rnd.Next(3), "@"));
        content.AppendLine($"Scenario: {ToTitle(LoremIpsum.GetShortText())}");

        AddSteps(content);
        content.AppendLine();
    }

    private void AddSteps(StringBuilder content, string[] soHeaders = null)
    {
        if (_unicodeStep != null)
            content.AppendLine($"  {_unicodeStep.Keyword} {GetStepText(_unicodeStep, soHeaders)}");
        AddStep(content, "Given", soHeaders: soHeaders);
        for (int andIndex = 0; andIndex < LoremIpsum.Rnd.Next(3); andIndex++)
            AddStep(content, "And", "Given", soHeaders);
        AddStep(content, "When", soHeaders: soHeaders);
        AddStep(content, "Then", soHeaders: soHeaders);
        for (int andIndex = 0; andIndex < LoremIpsum.Rnd.Next(3); andIndex++)
            AddStep(content, "And", "Then", soHeaders);
    }

    private void AddStep(StringBuilder content, string keyword, string keywordType = null, string[] soHeaders = null)
    {
        StepCount++;

        keywordType = keywordType ?? keyword;
        if (!_stepDefinitions.TryGetValue(keywordType, out var sdList))
            throw new Exception("keyword not found: " + keywordType);
        var stepDef = sdList[LoremIpsum.Rnd.Next(sdList.Length)];

        content.AppendLine($"  {keyword} {GetStepText(stepDef, soHeaders)}");
        if (stepDef.DataTable)
        {
            int cellCount = LoremIpsum.Rnd.Next(4) + 2;
            AppendTable(content, cellCount);
        }
        else if (stepDef.DocString)
        {
            content.AppendLine("    \"\"\"");
            content.AppendLine($"    {LoremIpsum.GetShortText(5)}");
            content.AppendLine($"    {LoremIpsum.GetShortText()}");
            content.AppendLine($"    {LoremIpsum.GetShortText(6)}");
            content.AppendLine($"    {LoremIpsum.GetShortText(5)}");
            content.AppendLine("    \"\"\"");
        }
    }

    private static void AppendTable(StringBuilder content, int cellCount, string[] headers = null)
    {
        var rows = new List<string[]>();
        headers = headers ?? LoremIpsum.GetUniqueWords(cellCount);
        rows.Add(headers);
        for (int i = 0; i < 4; i++)
            rows.Add(LoremIpsum.GetWords(cellCount));

        var cellWiths = Enumerable.Range(0, cellCount)
            .Select(i => rows.Max(r => r[i].Length)).ToArray();

        foreach (var row in rows)
        {
            content.Append("    | ");
            for (int i = 0; i < cellCount; i++)
            {
                content.Append(row[i].PadRight(cellWiths[i]));
                content.Append(" | ");
            }

            content.AppendLine();
        }
    }

    public List<string> GenerateStepDefClasses(string targetFolder, int stepDefPerClassCount)
    {
        var sdList = LoremIpsum.Randomize(_stepDefinitions.SelectMany(g => g.Value));
        int startIndex = 0;
        var result = new List<string>();

        result.Add(Path.Combine(targetFolder, "..", "AutomationStub.cs"));

        while (startIndex < sdList.Length)
        {
            result.Add(
                GenerateStepDefClass(targetFolder,
                    sdList.Skip(startIndex).Take(Math.Min(stepDefPerClassCount, sdList.Length - startIndex)), 
                    Enumerable.Empty<HookDef>()));
            startIndex += stepDefPerClassCount;
        }

        if (_hooks.Count > 0)
        {
            result.Add(GenerateStepDefClass(targetFolder, Enumerable.Empty<StepDef>(), _hooks));
        }

        return result;
    }

    private string GenerateStepDefClass(string folder, IEnumerable<StepDef> stepDefs, IEnumerable<HookDef> hookDefs)
    {
        var className = ToPascalCase(LoremIpsum.GetShortText()) + "Steps";
        var filePath = Path.Combine(folder, className + ".cs");
        File.WriteAllText(filePath, GenerateStepDefClassContent(stepDefs, hookDefs, className));
        return filePath;
    }

    private string GenerateStepDefClassContent(IEnumerable<StepDef> stepDefs, IEnumerable<HookDef> hookDefs, string className)
    {
        var content = new StringBuilder();
        content.AppendLine("using System;");
        content.AppendLine("using Reqnroll;");
        content.AppendLine("namespace DeveroomSample.StepDefinitions");
        content.AppendLine("{");
        content.AppendLine("    [Binding]");
        content.AppendLine($"    public class {className}");
        content.AppendLine("    {");

        foreach (var stepDef in stepDefs)
        {
            var asyncPrefix = stepDef.Async ? "async " : "";
            content.AppendLine($"        [{stepDef.Keyword}(@\"{stepDef.Regex.Replace("\"", "\"\"")}\")]");
            content.AppendLine(
                $"        public {asyncPrefix}void {stepDef.Keyword}{ToPascalCase(LoremIpsum.GetShortText())}({GetParams(stepDef)})");
            content.AppendLine("        {");
            content.AppendLine(
                $"           AutomationStub.DoStep({string.Join(", ", stepDef.Params.Select((p, i) => $"p{i}"))});");
            if (stepDef.Async)
                content.AppendLine("           await System.Threading.Tasks.Task.Delay(200);");
            content.AppendLine("        }");
            content.AppendLine();
        }

        foreach (var hookDef in hookDefs)
        {
            var asyncPrefix = hookDef.Async ? "async " : "";
            content.AppendLine($"        [{hookDef.HookType}()]");
            content.AppendLine(
                $"        public {asyncPrefix}void {hookDef.MethodName ?? ToPascalCase(LoremIpsum.GetShortText())}()");
            content.AppendLine("        {");
            content.AppendLine(
                $"           AutomationStub.DoHook();");
            if (hookDef.Async)
                content.AppendLine("           await System.Threading.Tasks.Task.Delay(200);");
            content.AppendLine("        }");
            content.AppendLine();
        }

        content.AppendLine("    }");
        content.AppendLine("}");
        return content.ToString();
    }

    private string GetParams(StepDef stepDef)
    {
        return string.Join(", ", stepDef.Params.Select((p, i) => $"{ParamTypes[p]} p{i}"));
    }

    public void AddAsyncStep()
    {
        var stepDef = new StepDef
        {
            Keyword = "When",
            Regex = "there is an async step",
            StepTextParams = new List<string>(),
            Async = true
        };
        if (_stepDefinitions.ContainsKey("When"))
            _stepDefinitions["When"] = _stepDefinitions["When"].Concat(new[] {stepDef}).ToArray();
        else
            _stepDefinitions["When"] = new[] {stepDef};
    }

    private class StepDef
    {
        public string Keyword { get; set; }
        public string Regex { get; set; }
        public bool DataTable { get; set; }
        public bool DocString { get; set; }
        public List<string> StepTextParams { get; set; }
        public bool Async { get; set; }

        public IEnumerable<string> Params
        {
            get
            {
                var result = StepTextParams.AsEnumerable();
                if (DataTable)
                    result = result.Append(nameof(DataTable));
                else if (DocString)
                    result = result.Append(nameof(DocString));
                return result;
            }
        }
    }

    private class HookDef
    {
        public bool Async { get; set; }
        public string HookType { get; set; }
        public string MethodName { get; set; }
    }

    public void AddBeforeScenarioHook()
    {
        _hooks.Add(new HookDef
        {
            HookType = "BeforeScenario",
            MethodName = "BeforeScenarioHook"
        });
    }
}
