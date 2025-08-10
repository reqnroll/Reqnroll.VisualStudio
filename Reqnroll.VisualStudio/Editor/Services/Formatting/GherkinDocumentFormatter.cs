#nullable disable

namespace Reqnroll.VisualStudio.Editor.Services.Formatting;

[Export(typeof(GherkinDocumentFormatter))]
public class GherkinDocumentFormatter
{
    public void FormatGherkinDocument(DeveroomGherkinDocument gherkinDocument, DocumentLinesEditBuffer lines,
        GherkinFormatSettings formatSettings)
    {
      if (gherkinDocument.Feature == null) 
        return;

      SetTagsAndLine(lines, gherkinDocument.Feature, string.Empty);
      SetLinesForChildren(lines, gherkinDocument.Feature.Children, formatSettings, formatSettings.FeatureChildrenIndentLevel, gherkinDocument.GherkinDialect);
    }

    private void SetLinesForChildren(DocumentLinesEditBuffer lines, IEnumerable<IHasLocation> hasLocation,
        GherkinFormatSettings formatSettings, int indentLevel, GherkinDialect gherkinDialect)
    {
        foreach (var featureChild in hasLocation)
        {
            SetTagsAndLine(lines, featureChild, GetIndent(formatSettings, indentLevel));

            if (featureChild is Rule rule)
                SetLinesForChildren(lines, rule.Children, formatSettings,
                    indentLevel + formatSettings.RuleChildrenIndentLevelWithinRule, gherkinDialect);

            if (featureChild is ScenarioOutline scenarioOutline)
                foreach (var example in scenarioOutline.Examples)
                {
                    var examplesBlockIndentLevel =
                        indentLevel + formatSettings.ExamplesBlockIndentLevelWithinScenarioOutline;
                    SetTagsAndLine(lines, example, GetIndent(formatSettings, examplesBlockIndentLevel));
                    FormatTable(lines, example, formatSettings,
                        examplesBlockIndentLevel + formatSettings.ExamplesTableIndentLevelWithinExamplesBlock);
                }

            if (featureChild is IHasSteps hasSteps) 
              FormatSteps(lines, formatSettings, indentLevel, hasSteps, gherkinDialect);
        }
    }

    private void FormatSteps(DocumentLinesEditBuffer lines, GherkinFormatSettings formatSettings, int indentLevel,
      IHasSteps hasSteps, GherkinDialect gherkinDialect)
    {
      var previousKeyword = "";

      foreach (var step in hasSteps.Steps)
      {
        var stepIndentLevel = indentLevel + formatSettings.StepIndentLevelWithinStepContainer;

        var newKeyword = step.Keyword;

        if (step is DeveroomGherkinStep { StepKeyword: StepKeyword.And or StepKeyword.But })
        {
          stepIndentLevel += formatSettings.AndStepIndentLevelWithinSteps;
        }
        else
        {
          if (step.Keyword == previousKeyword)
          {
            var andKeyword = GetAndKeyword(gherkinDialect);
            newKeyword = $"{andKeyword}";
          }
          else
            previousKeyword = step.Keyword;
        }


        SetLine(lines, step, $"{GetIndent(formatSettings, stepIndentLevel)}{newKeyword}{step.Text}");

        switch (step.Argument)
        {
          case DataTable dataTable:
            FormatTable(lines, dataTable, formatSettings, stepIndentLevel + formatSettings.DataTableIndentLevelWithinStep);
            break;
          case DocString docString:
            FormatDocString(lines, docString, formatSettings, stepIndentLevel + formatSettings.DocStringIndentLevelWithinStep);
            break;
        }
      }
    }

    private static string GetAndKeyword(GherkinDialect gherkinDialect)
    {
      return gherkinDialect.AndStepKeywords.First(keyword => keyword != GherkinDialect.AsteriskKeyword);
    }

    private void SetTagsAndLine(DocumentLinesEditBuffer lines, IHasLocation hasLocation, string indent)
    {
        if (hasLocation is IHasTags hasTags) SetTags(lines, hasTags.Tags, indent);

        if (hasLocation is IHasDescription hasDescription)
            SetLine(lines, hasLocation, GetHasDescriptionLine(hasDescription, indent));
    }

    internal int[] GetTableWidths(IHasRows hasRows)
    {
        var widths = new int[hasRows.Rows.Max(r => r.Cells.Count())];
        foreach (var row in hasRows.Rows)
        foreach (var item in row.Cells.Select((c, i) => new {c, i}))
            widths[item.i] = Math.Max(widths[item.i], EscapeTableCellValue(item.c.Value).Length);
        return widths;
    }

    private string EscapeTableCellValue(string cellValue) =>
        cellValue
            .Replace("\\", "\\\\")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("|", "\\|");

    private bool IsTableCellContentNumeric(string cellValue)
    {
        if (string.IsNullOrWhiteSpace(cellValue))
            return false;
        // Return true if the cell value contains any digit
        return cellValue.Any(char.IsDigit);
    }

    private bool IsTableCellContentRightAligned(string cellValue, GherkinFormatSettings formatSettings)
    {
        if (!formatSettings.RightAlignNumericTableCells)
            return false;
        // Return true if the cell value contains any digit and is not empty
        return IsTableCellContentNumeric(cellValue);
    }
    private string GetUnfinishedTableCell(string lineText)
    {
        var match = Regex.Match(lineText, @"(?<!\\)(\\\\)*\|(?<remaining>.*?)$", RegexOptions.RightToLeft);
        string unfinishedCell = null;
        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups["remaining"].Value))
            unfinishedCell = match.Groups["remaining"].Value.Trim();

        return unfinishedCell;
    }

    private void FormatTable(DocumentLinesEditBuffer lines, IHasRows hasRows, GherkinFormatSettings formatSettings,
        int indentLevel)
    {
        var indent = GetIndent(formatSettings, indentLevel);
        FormatTable(lines, hasRows, formatSettings, indent);
    }

    public void FormatTable(DocumentLinesEditBuffer lines, IHasRows hasRows, GherkinFormatSettings formatSettings,
        string indent, int[] widths = null)
    {
        widths ??= GetTableWidths(hasRows);
        foreach (var row in hasRows.Rows)
        {
            var result = new StringBuilder();
            result.Append(indent);
            result.Append("|");
            foreach (var item in row.Cells.Select((c, i) => new {c, i}))
            {
                result.Append(formatSettings.TableCellPadding);
                var escapedCellValue = EscapeTableCellValue(item.c.Value);
                var width = widths[item.i];
                var paddedCell = IsTableCellContentRightAligned(item.c.Value, formatSettings) ? escapedCellValue.PadLeft(width) : escapedCellValue.PadRight(width);
                result.Append(paddedCell);
                result.Append(formatSettings.TableCellPadding);
                result.Append('|');
            }

            var unfinishedCell = GetUnfinishedTableCell(lines.GetLineOneBased(row.Location.Line));
            if (unfinishedCell != null)
            {
                result.Append(formatSettings.TableCellPadding);
                result.Append(unfinishedCell);
            }

            SetLine(lines, row, result.ToString());
        }
    }

    private void FormatDocString(DocumentLinesEditBuffer lines, DocString docString,
        GherkinFormatSettings formatSettings, int indentLevel)
    {
        var indent = GetIndent(formatSettings, indentLevel);
        var docStringStartLine = docString.Location.Line;
        var docStringContentLines = DeveroomTagParser.NewLineRe.Split(docString.Content);
        if (string.IsNullOrEmpty(docString.Content) &&
            !string.IsNullOrWhiteSpace(lines.GetLineOneBased(docStringStartLine + 1)))
            docStringContentLines = Array.Empty<string>();

        var docStringEndLine = docStringStartLine + docStringContentLines.Length + 1;
        var delimiterLine = $"{indent}{docString.Delimiter}";

        lines.SetLineOneBased(docStringStartLine, delimiterLine);
        var docStringRow = 1;
        foreach (var contentLine in docStringContentLines)
        {
            var line = $"{indent}{contentLine}";
            lines.SetLineOneBased(docStringStartLine + docStringRow++, line);
        }

        lines.SetLineOneBased(docStringEndLine, delimiterLine);
    }

    private string GetHasDescriptionLine(IHasDescription hasDescription, string indent)
    {
        var line = $"{indent}{hasDescription.Keyword}:";
        if (!string.IsNullOrEmpty(hasDescription.Name))
            line += $" {hasDescription.Name}";
        return line;
    }

    private void SetTags(DocumentLinesEditBuffer lines, IEnumerable<Tag> tags, string indent)
    {
        var tagGroup = tags.GroupBy(t => t.Location.Line);
        foreach (var tag in tagGroup)
        {
            var line = indent + string.Join(" ", tag.Select(t => t.Name));
            lines.SetLineOneBased(tag.Key, line);
        }
    }

    private void SetLine(DocumentLinesEditBuffer lines, IHasLocation hasLocation, string line)
    {
        if (hasLocation?.Location != null && hasLocation.Location.Line >= 1
                                          && hasLocation.Location.Column - 1 < line.Length)
            lines.SetLineOneBased(hasLocation.Location.Line, line);
    }

    private string GetIndent(GherkinFormatSettings formatSettings, int indentLevel)
    {
        if (indentLevel == 0)
            return string.Empty;
        if (indentLevel == 1)
            return formatSettings.Indent;
        return string.Join(string.Empty, Enumerable.Range(0, indentLevel).Select(_ => formatSettings.Indent));
    }
}
