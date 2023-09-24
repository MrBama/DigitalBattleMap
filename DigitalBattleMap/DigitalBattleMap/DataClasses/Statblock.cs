using DigitalBattleMap.Utilities;
using Markdig;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows;

namespace DigitalBattleMap.DataClasses;

public abstract class Statblock : PropertyHandler
{
    protected Statblock(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
    public Type Type { get => GetType(); }

    [JsonIgnore]
    public WebViewPage WebViewPage { get => GetWebViewPage(); }
    [JsonIgnore]
    public Visibility RenderVisibility { get => Get<Visibility>(); set => Set(value); }

    public abstract void Persist(string name);
    public abstract Statblock Copy();
    protected abstract WebViewPage GetWebViewPage();
}

public class SourceStatblock : Statblock
{
    private WebViewPage _webViewPage;

    public SourceStatblock(string sourceName, string sourceBook) : base(sourceName)
    {
        SourceName = sourceName;
        SourceBook = sourceBook;
    }

    public string SourceName { get; set; }
    public string SourceBook { get; set; }

    public override void Persist(string name)
    {
        Name = name;
    }

    public override Statblock Copy()
    {
        var statblock = new SourceStatblock(SourceName, SourceBook);
        statblock.Name = Name;
        return statblock;
    }

    protected override WebViewPage GetWebViewPage()
    {
        if (_webViewPage == null)
        {
            var uri = new Uri($"https://5e.tools/bestiary.html#{Uri.EscapeDataString(SourceName)}_{SourceBook}");
            _webViewPage = new WebViewPage(uri);
        }

        return _webViewPage;
    }
}

public class MarkdownStatblock : Statblock
{
    private string _markdown;

    [JsonConstructor]
    public MarkdownStatblock(string name, string markdownPath) : base(name)
    {
        MarkdownPath = markdownPath;
    }

    public MarkdownStatblock(string markdown) : base("")
    {
        _markdown = markdown;
    }

    public string MarkdownPath { get; set; }

    public string GetMarkdown()
    {
        _markdown ??= IO.File.ReadAllText(MarkdownPath);
        return _markdown;
    }

    public override void Persist(string name)
    {
        var markdown = GetMarkdown();
        Name = name;
        MarkdownPath = Path.Combine(Constants.CustomTokensPath, $"{name}.md");
        IO.File.WriteAllText(MarkdownPath, markdown);
    }

    public override Statblock Copy()
    {
        return new MarkdownStatblock(Name, MarkdownPath);
    }

    protected override WebViewPage GetWebViewPage()
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var html = Markdown.ToHtml(GetMarkdown(), pipeline);
        return new WebViewPage(html);
    }
}
