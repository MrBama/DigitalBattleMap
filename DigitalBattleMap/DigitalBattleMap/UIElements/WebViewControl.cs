using DigitalBattleMap.DataClasses;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Windows;

namespace DigitalBattleMap.UIElements;

public class WebViewControl : WebView2
{
    private bool _isInitialized = false;

    public WebViewControl()
    {
        Loaded += async (sender, e) =>
        {
            if (!_isInitialized)
            {
                var options = new CoreWebView2EnvironmentOptions
                {
                    AreBrowserExtensionsEnabled = true
                };

                var env = await CoreWebView2Environment.CreateAsync("", null, options);
                await EnsureCoreWebView2Async(env);

                _isInitialized = true;

                if(Page != null)
                {
                    if (Page.Type == WebViewPageType.Uri)
                    {
                        Source = Page.Uri;
                    }
                    else if (Page.Type == WebViewPageType.Html)
                    {
                        CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Light;
                        NavigateToString(Page.Html);
                    }
                }
            }
        };
    }

    public static readonly DependencyProperty PageProperty = DependencyProperty.Register(nameof(Page), typeof(WebViewPage), typeof(WebViewControl), new UIPropertyMetadata(null, OnPagePropertyChanged));
    public static readonly DependencyProperty IsDisposingProperty = DependencyProperty.Register(nameof(IsDisposing), typeof(bool), typeof(WebViewControl), new UIPropertyMetadata(false, OnIsDisposingPropertyChanged));

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

    public bool IsDisposing
    {
        get
        {
            return (bool)GetValue(IsDisposingProperty);
        }
        set
        {
            SetValue(IsDisposingProperty, value);
        }
    }

    private static void OnPagePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var webViewControl = (WebViewControl)dependencyObject;
        var webViewPage = (WebViewPage)eventArgs.NewValue;

        if(webViewControl._isInitialized)
        {
            if (webViewPage.Type == WebViewPageType.Uri)
            {
                webViewControl.Source = webViewPage.Uri;
            }
            else if (webViewPage.Type == WebViewPageType.Html)
            {
                webViewControl.NavigateToString(webViewPage.Html);
            }
        } 
    }

    private static void OnIsDisposingPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var webViewControl = (WebViewControl)dependencyObject;
        var isDisposing = (bool)eventArgs.NewValue;

        if(isDisposing)
        {
            webViewControl.Dispose();
        }
    }
}
