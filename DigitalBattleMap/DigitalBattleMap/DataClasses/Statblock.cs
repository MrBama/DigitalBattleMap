using DigitalBattleMap.UIElements;
using DigitalBattleMap.Utilities;
using Markdig;
using System;
using System.Windows;

namespace DigitalBattleMap.DataClasses;

public class Statblock : PropertyHandler
{
    public static Statblock WithSource(string name, string source)
    {
        var uri = new Uri($"https://5e.tools/bestiary.html#{Uri.EscapeDataString(name)}_{source}");
        return new Statblock(name, new WebViewPage(uri));
    }

    public static Statblock WithMarkdown(string name, string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var html = Markdown.ToHtml(markdown, pipeline);
        return new Statblock(name, new WebViewPage(html));
    }

    private Statblock(string name, WebViewPage webViewPage)
    {
        Name = name;
        WebViewPage = webViewPage;
    }

    public string Name { get; set; }
    public WebViewPage WebViewPage { get; set; }
    public Visibility RenderVisibility { get => Get<Visibility>(); set => Set(value); }
}
