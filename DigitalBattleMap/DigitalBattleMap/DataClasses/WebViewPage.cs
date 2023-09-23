using DigitalBattleMap.UIElements;
using System;

namespace DigitalBattleMap.DataClasses;

public class WebViewPage
{
    public WebViewPage(Uri uri)
    {
        Type = WebViewPageType.Uri;
        Uri = uri;
    }

    public WebViewPage(string html)
    {
        Type = WebViewPageType.Html;
        Html = html;
    }

    public WebViewPageType Type { get; }
    public Uri Uri { get; }
    public string Html { get; }
}
