#nullable disable
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Navigation;

namespace Reqnroll.VisualStudio.UI.Controls;

[ContentProperty("MarkDownText")]
public class MarkDownTextBlock : RichTextBox
{
    public MarkDownTextBlock()
    {
        IsDocumentEnabled = true;
        IsReadOnly = true;
        BorderThickness = new Thickness(0);
        Background = Brushes.Transparent;
    }

    private FlowDocument GetFlowDocument(string markDownText, double fontSize)
    {
        markDownText = markDownText.Replace("\r\n", "\n");

        var paragraphs = Regex.Split(markDownText, @"\s*\n\s*(\n\s*)+").Where(p => !string.IsNullOrWhiteSpace(p));
        var specialParagraphRe =
            new Regex(@"^\s*(?:(?<headline>#+)\s*(?<text>.*)|(?<list>[\*\-]\s)\s*(?<text>.*(\n\s*[^\*].*)*))");

        FlowDocument document = new FlowDocument();
        SetFlowDocumentStyles(document, fontSize);

        foreach (var paragraphIt in paragraphs)
        {
            var paragraph = paragraphIt;
            var specialParagraphMatch = specialParagraphRe.Match(paragraph);
            while (specialParagraphMatch.Success)
            {
                var text = specialParagraphMatch.Groups["text"].Value;
                if (specialParagraphMatch.Groups["headline"].Success)
                {
                    if (specialParagraphMatch.Groups["headline"].Length == 1)
                        document.Blocks.Add(new H1(text));
                    else
                        document.Blocks.Add(new H2(text));
                }
                else if (specialParagraphMatch.Groups["list"].Success)
                {
                    var listItem = new ListItem(CreateParagraph(text));
                    var list = document.Blocks.LastOrDefault() as List;
                    if (list == null)
                    {
                        list = new List();
                        document.Blocks.Add(list);
                    }

                    list.ListItems.Add(listItem);
                }

                paragraph = paragraph.Substring(specialParagraphMatch.Length);
                specialParagraphMatch = specialParagraphRe.Match(paragraph);
            }

            if (string.IsNullOrWhiteSpace(paragraph))
                continue;

            var fdPara = CreateParagraph(paragraph);

            document.Blocks.Add(fdPara);
        }

        return document;
    }

    private Paragraph CreateParagraph(string paragraph)
    {
        var inlineRe =
            new Regex(
                @"(\[(?<linkText>.*?)\]\((?<link>.*?)\)|(?<link>https?\:\/\/[\w\.\/-]+[\w\/])|\*\*(?<boldText>.*?)\*\*|\*(?<italicText>.*?)\*)");

        paragraph = Regex.Replace(paragraph.Trim(), @"\s+", " ");
        var fdPara = new Paragraph();
        var matches = inlineRe.Matches(paragraph).Cast<Match>().OrderBy(m => m.Index);
        int position = 0;
        foreach (var match in matches)
        {
            if (match.Index > position)
                fdPara.Inlines.Add(new Run(paragraph.Substring(position, match.Index - position)));
            if (match.Groups["link"].Success)
            {
                var linkText = match.Groups["linkText"].Success
                    ? match.Groups["linkText"].Value
                    : match.Groups["link"].Value;
                var hyperlink = new Hyperlink(new Run(linkText));
                var linkUri = Environment.ExpandEnvironmentVariables(match.Groups["link"].Value);
                if (Uri.TryCreate(linkUri, UriKind.Absolute, out var uri))
                {
                    hyperlink.NavigateUri = uri;
                    hyperlink.RequestNavigate += Hyperlink_OnRequestNavigate;
                }

                fdPara.Inlines.Add(hyperlink);
            }
            else if (match.Groups["boldText"].Success)
            {
                fdPara.Inlines.Add(new Bold(new Run(match.Groups["boldText"].Value)));
            }
            else if (match.Groups["italicText"].Success)
            {
                fdPara.Inlines.Add(new Italic(new Run(match.Groups["italicText"].Value)));
            }

            position = match.Index + match.Length;
        }

        if (position < paragraph.Length)
            fdPara.Inlines.Add(new Run(paragraph.Substring(position)));
        return fdPara;
    }

    private static void SetFlowDocumentStyles(FlowDocument document, double fontSize)
    {
        document.FontSize = fontSize;

        var pStyle = new Style(typeof(Paragraph));
        pStyle.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0, 0, 0, 3)));
        document.Resources.Add(typeof(Paragraph), pStyle);

        var h1Style = new Style(typeof(H1));
        h1Style.Setters.Add(new Setter(TextElement.FontSizeProperty, document.FontSize + 4));
        h1Style.Setters.Add(new Setter(TextElement.FontWeightProperty, FontWeights.Bold));
        h1Style.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0, 0, 0, 3)));
        document.Resources.Add(typeof(H1), h1Style);

        var h2Style = new Style(typeof(H2));
        h2Style.Setters.Add(new Setter(TextElement.FontSizeProperty, document.FontSize + 2));
        h2Style.Setters.Add(new Setter(TextElement.FontWeightProperty, FontWeights.Bold));
        h2Style.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0, 6, 0, 3)));
        document.Resources.Add(typeof(H2), h2Style);

        var listStyle = new Style(typeof(List));
        listStyle.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0, 2, 0, 3)));
        listStyle.Setters.Add(new Setter(Block.PaddingProperty, new Thickness(20, 0, 0, 0)));
        document.Resources.Add(typeof(List), listStyle);

        var listItemStyle = new Style(typeof(ListItem));
        listItemStyle.Setters.Add(new Setter(ListItem.MarginProperty, new Thickness(0, 0, 0, 3)));
        listItemStyle.Setters.Add(new Setter(ListItem.PaddingProperty, new Thickness(0, 0, 0, 0)));
        document.Resources.Add(typeof(ListItem), listItemStyle);
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        if (e.Uri != null)
        {
            var args = new RequestNavigateEventArgs(e.Uri, e.Target)
            {
                RoutedEvent = LinkClickedEvent
            };
            RaiseEvent(args);
        }
    }

    public class H1 : Paragraph
    {
        public H1(string text) : base(new Run(text))
        {
        }
    }

    public class H2 : Paragraph
    {
        public H2(string text) : base(new Run(text))
        {
        }
    }

    #region MarkDownText Dependency Property

    public static readonly DependencyProperty MarkDownTextProperty = DependencyProperty.Register("MarkDownText",
        typeof(string), typeof(MarkDownTextBlock),
        new PropertyMetadata(string.Empty, MarkDownTextChangedCallback), MarkDownTextValidateCallback);

    private static void MarkDownTextChangedCallback(
        DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        if (obj is MarkDownTextBlock markDownTextBlock)
            markDownTextBlock.Document =
                markDownTextBlock.GetFlowDocument(e.NewValue as string, markDownTextBlock.FontSize);
    }

    private static bool MarkDownTextValidateCallback(object value) => value != null;

    public string MarkDownText
    {
        get => (string) GetValue(MarkDownTextProperty);
        set => SetValue(MarkDownTextProperty, value);
    }

    #endregion

    #region LinkClicked Event

    public static readonly RoutedEvent LinkClickedEvent =
        EventManager.RegisterRoutedEvent("LinkClicked", RoutingStrategy.Bubble,
            typeof(RequestNavigateEventHandler), typeof(MarkDownTextBlock));

    public event RequestNavigateEventHandler LinkClicked
    {
        add => AddHandler(LinkClickedEvent, value);
        remove => RemoveHandler(LinkClickedEvent, value);
    }

    #endregion
}
