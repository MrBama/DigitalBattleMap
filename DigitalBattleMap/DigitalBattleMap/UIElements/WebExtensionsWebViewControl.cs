using DigitalBattleMap.DataClasses;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace DigitalBattleMap.UIElements;

public class WebExtensionsWebViewControl : WebView2
{
    private bool _isInitialized = false;

    public WebExtensionsWebViewControl()
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

                if (WebExtensionVersions != null)
                {
                    var extensions = await CoreWebView2.Profile.GetBrowserExtensionsAsync();
                    foreach (var extension in WebExtensionVersions.Keys)
                    {
                        if (extensions.SingleOrDefault(e => e.Name == extension) == null)
                        {
                            await CoreWebView2.Profile.AddBrowserExtensionAsync(Path.Combine(Constants.WebExtensionsPath, extension));
                        }
                    }
                }
            }
        };
    }

    public static readonly DependencyProperty WebExtensionVersionsProperty = DependencyProperty.Register(nameof(WebExtensionVersions), typeof(Dictionary<string, string>), typeof(WebExtensionsWebViewControl));
    
    public Dictionary<string, string> WebExtensionVersions
    {
        get
        {
            return (Dictionary<string, string>)GetValue(WebExtensionVersionsProperty);
        }
        set
        {
            SetValue(WebExtensionVersionsProperty, value);
        }
    }
}
