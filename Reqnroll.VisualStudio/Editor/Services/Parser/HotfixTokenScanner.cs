using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Editor.Services.Parser;

public class HotfixTokenScanner : TokenScanner
{
    public HotfixTokenScanner(TextReader reader) : base(reader)
    {
    }

    public override Token Read()
    {
        var token = base.Read();
        if (token.Line != null)
            token = new Token(new HotfixLine(token.Line), token.Location);
        return token;
    }

    private class HotfixLine : IGherkinLine
    {
        private readonly IGherkinLine _line;

        public HotfixLine(IGherkinLine line)
        {
            _line = line;
        }

        public int LineNumber => _line.LineNumber;
        public int Indent => _line.Indent;

        public void Detach()
        {
            _line.Detach();
        }

        public bool IsEmpty() => _line.IsEmpty();

        public bool StartsWith(string text) => _line.StartsWith(text);

        public bool StartsWithTitleKeyword(string keyword) => _line.StartsWithTitleKeyword(keyword);

        public string GetLineText(int indentToRemove = 0) => _line.GetLineText(indentToRemove);

        public string GetRestTrimmed(int length) => _line.GetRestTrimmed(length);

        public IEnumerable<GherkinLineSpan> GetTags() => _line.GetTags();

        //public IEnumerable<GherkinLineSpan> GetTableCells()
        //{
        //    return _line.GetTableCells();
        //}

        public IEnumerable<GherkinLineSpan> GetTableCells()
        {
            var trimmedLineText = _line.GetRestTrimmed(0);

            var items = SplitCells(trimmedLineText).ToList();
            bool isBeforeFirst = true;
            foreach (var item in items.Take(items.Count - 1)) // skipping the one after last
            {
                if (!isBeforeFirst)
                {
                    int trimmedStart;
                    var cellText = Trim(item.Item1, out trimmedStart);
                    var cellPosition = item.Item2 + trimmedStart;

                    if (cellText.Length == 0)
                        cellPosition = item.Item2;

                    yield return new GherkinLineSpan(Indent + cellPosition + 1, cellText);
                }

                isBeforeFirst = false;
            }
        }

        private IEnumerable<Tuple<string, int>> SplitCells(string row)
        {
            var rowEnum = row.GetEnumerator();

            string cell = "";
            int pos = 0;
            int startPos = 0;
            while (rowEnum.MoveNext())
            {
                pos++;
                char c = rowEnum.Current;
                if (c.ToString() == GherkinLanguageConstants.TABLE_CELL_SEPARATOR)
                {
                    yield return Tuple.Create(cell, startPos);
                    cell = "";
                    startPos = pos;
                }
                else if (c == GherkinLanguageConstants.TABLE_CELL_ESCAPE_CHAR && rowEnum.MoveNext())
                {
                    pos++;
                    c = rowEnum.Current;
                    if (c == GherkinLanguageConstants.TABLE_CELL_NEWLINE_ESCAPE)
                    {
                        cell += "\n";
                    }
                    else
                    {
                        if (c.ToString() != GherkinLanguageConstants.TABLE_CELL_SEPARATOR &&
                            c != GherkinLanguageConstants.TABLE_CELL_ESCAPE_CHAR)
                            cell += GherkinLanguageConstants.TABLE_CELL_ESCAPE_CHAR;
                        cell += c;
                    }
                }
                else
                {
                    cell += c;
                }
            }

            yield return Tuple.Create(cell, startPos);
        }

        private string Trim(string s, out int trimmedStart)
        {
            trimmedStart = 0;
            while (trimmedStart < s.Length && char.IsWhiteSpace(s[trimmedStart]))
                trimmedStart++;

            return s.Trim();
        }
    }
}
