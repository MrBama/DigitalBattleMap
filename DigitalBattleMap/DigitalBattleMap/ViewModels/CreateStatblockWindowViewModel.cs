using DigitalBattleMap.DataClasses;
using DigitalBattleMap.UIElements;
using DigitalBattleMap.Utilities;
using Markdig;
using Microsoft.Web.WebView2.Core;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class CreateStatblockWindowViewModel : ViewModelBase
{
    public CreateStatblockWindowViewModel()
    {
        Initialize();
    }

    public CreateStatblockWindowViewModel(string markdownText)
    {
        Initialize();
        MarkdownText = markdownText;
    }

    protected override void InitializeCommands()
    {
        OkCommand = new RelayCommand(p => OkButton());
    }

    public string MarkdownText { get => Get<string>(); set => Set(value, UpdateWebView); }
    public WebViewPage WebViewPage { get => Get<WebViewPage>(); set => Set(value); }
    public bool Success { get; set; }

    public ICommand OkCommand { get; set; }

    private void Initialize()
    {
        MarkdownText = "";
    }

    private void OkButton()
    {
        if (MarkdownText != "")
        {
            Success = true;
        }
    }

    private void UpdateWebView()
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var html = Markdown.ToHtml(MarkdownText, pipeline);
        WebViewPage = new WebViewPage(html);
    }
}
