using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Documents;

namespace DigitalBattleMap.UIElements;

public class HyperlinkTextBlock : TextBlock
{
    public HyperlinkTextBlock()
    {
        var dependencyProperty = DependencyPropertyDescriptor.FromProperty(TextProperty, typeof(TextBlock));
        dependencyProperty.AddValueChanged(this, (sender, args) =>
        {
            ApplyHyperinks();
        });
    }

    private void ApplyHyperinks()
    {
        // Example hyperlink: <a href="http://www.example.com">here</a>
        const string hyperlinkPrefix = "<a href=";
        const string hyperlinkPostfix = "</a>";

        if (!Text.Contains(hyperlinkPrefix))
        {
            return;
        }

        var text = Text;
        Inlines.Clear();

        while (text.Contains(hyperlinkPrefix))
        {
            var startIndex = text.IndexOf(hyperlinkPrefix);
            var endIndex = text.IndexOf(hyperlinkPostfix);
            var hyperlink = text.Substring(startIndex, endIndex + hyperlinkPostfix.Length - startIndex);

            if (startIndex != 0)
            {
                Inlines.Add(new Run(text.Substring(0, startIndex)));
            }

            Inlines.Add(CreateHyperlink(hyperlink));
            text = text.Substring(endIndex + hyperlinkPostfix.Length);
        }

        if (text != "")
        {
            Inlines.Add(text);
        }
    }

    private Hyperlink CreateHyperlink(string hyperlinkText)
    {
        var hyperlink = new Hyperlink(new Run(GetHyperlinkText(hyperlinkText)));

        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = GetHyperlinkAddress(hyperlinkText),
            UseShellExecute = true
        };

        hyperlink.Click += (s, e) => Process.Start(processStartInfo);
        return hyperlink;
    }

    private string GetHyperlinkText(string hyperlink)
    {
        var startIndex = hyperlink.IndexOf(">");
        var endIndex = hyperlink.LastIndexOf("</a>");
        return hyperlink.Substring(startIndex + 1, endIndex - startIndex - 1);
    }

    private string GetHyperlinkAddress(string hyperlink)
    {
        var startIndex = hyperlink.IndexOf("\"");
        var endIndex = hyperlink.LastIndexOf("\"");
        return hyperlink.Substring(startIndex + 1, endIndex - startIndex - 1);
    }
}
