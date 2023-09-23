using DigitalBattleMap.DataClasses;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Windows;

namespace DigitalBattleMap.UIElements;

public class WebViewControl : WebView2
{
    public WebViewControl()
    {
        Loaded += async (sender, e) =>
        {
            if(Page != null && Page.Type == WebViewPageType.Html)
            {
                await EnsureCoreWebView2Async().ConfigureAwait(true);
                NavigateToString(Page.Html);
            }            
        };
    }

    public static readonly DependencyProperty PageProperty = DependencyProperty.Register("Page", typeof(WebViewPage), typeof(WebViewControl), new UIPropertyMetadata(null, OnPagePropertyChanged));

    public WebViewPage Page
    {
        get
        {
            return (WebViewPage)GetValue(PageProperty);
        }
        set
        {
            SetValue(PageProperty, value);
        }
    }
    
    private static void OnPagePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var webViewControl = (WebViewControl)dependencyObject;
        var webViewPage = (WebViewPage)eventArgs.NewValue;

        if(webViewPage.Type == WebViewPageType.Uri)
        {
            webViewControl.Source = webViewPage.Uri;
        }
        else if(webViewPage.Type == WebViewPageType.Html && webViewControl.IsLoaded)
        {
            webViewControl.NavigateToString(webViewPage.Html);
        }
    }
}
