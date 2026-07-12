using DigitalBattleMap.DataClasses;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static DigitalBattleMap.Utilities.FileManager;

namespace DigitalBattleMap.Utilities;

public static class ImportExport
{
    public static bool Import(string path, Settings settings)
    {
        if(path.EndsWith(".campaign"))
        {
            ImportCampaign(path, settings);
        }
        else if(path.EndsWith(".tokengroup"))
        {
            ImportTokenGroup(path, settings);
        }
        else if (path.EndsWith(".token"))
        {
            ImportToken(path, settings);
        }
        else
        {
            return false;
        }

        return true;
    }

    public static void Export(string path, Campaign campaign, List<Token> customTokens)
    {
        using var tempDirectory = new TempDirectory();
        string campaignFilePath = Path.Combine(tempDirectory.Path, "Campaign.json");

        FileManager.SaveFile(campaign, campaignFilePath);

        var copiedTokenNames = new List<string>();
        foreach (var player in campaign.Players)
        {
            foreach (var tokenIdentifier in player.TokenIdentifiers)
            {
                if (!copiedTokenNames.Contains(tokenIdentifier.Name))
                {
                    var customToken = customTokens.SingleOrDefault(t => t.Name == tokenIdentifier.Name);
                    if (customToken != null)
                    {
                        copiedTokenNames.Add(tokenIdentifier.Name);
                        Export(Path.Combine(tempDirectory.Path, tokenIdentifier.Name), customToken);
                    }
                }
            }
        }

        if (IO.File.Exists(path))
        {
            IO.File.Delete(path);
        }
        IO.ZipFile.CreateFromDirectory(tempDirectory.Path, path + ".campaign");
    }

    public static void Export(string path, TokenGroup tokenGroup, List<Token> customTokens)
    {
        using var tempDirectory = new TempDirectory();
        string tokenGroupFilePath = Path.Combine(tempDirectory.Path, "TokenGroup.json");

        FileManager.SaveFile(tokenGroup, tokenGroupFilePath);

        var copiedTokenNames = new List<string>();
        foreach (var tokenName in tokenGroup.TokenNames)
        {
            if (!copiedTokenNames.Contains(tokenName))
            {
                var customToken = customTokens.SingleOrDefault(t => t.Name == tokenName);
                if (customToken != null)
                {
                    copiedTokenNames.Add(tokenName);
                    Export(Path.Combine(tempDirectory.Path, tokenName), customToken);
                }
            }
        }

        if (IO.File.Exists(path))
        {
            IO.File.Delete(path);
        }
        IO.ZipFile.CreateFromDirectory(tempDirectory.Path, path + ".tokengroup");
    }

    public static void Export(string path, Token token)
    {
        using var tempDirectory = new TempDirectory();
        string tokenFilePath = Path.Combine(tempDirectory.Path, "Token.json");
        string tokenImageFilePath = Path.Combine(tempDirectory.Path, "Image.png");
        string statblockFilePath = Path.Combine(tempDirectory.Path, "Markdown.md");

        FileManager.SaveFile(token, tokenFilePath);
        IO.File.Copy(token.ImagePath, tokenImageFilePath);

        if (token.Statblock is MarkdownStatblock markdownStatblock)
        {
            IO.File.Copy(markdownStatblock.MarkdownPath, statblockFilePath);
        }

        if (IO.File.Exists(path))
        {
            IO.File.Delete(path);
        }
        IO.ZipFile.CreateFromDirectory(tempDirectory.Path, path + ".token");
    }

    private static void ImportCampaign(string path, Settings settings)
    {
        using var tempDirectory = new TempDirectory();
        string campaignFilePath = Path.Combine(tempDirectory.Path, "Campaign.json");

        IO.ZipFile.ExtractToDirectory(path, tempDirectory.Path);

        if (FileManager.OpenFile(campaignFilePath, out Campaign campaign))
        {
            if (settings.Campaigns.SingleOrDefault(t => t.Name == campaign.Name) == null)
            {
                settings.Campaigns.Add(campaign.Clone<Campaign>());

                foreach (var player in campaign.Players)
                {
                    foreach (var tokenIdentifier in player.TokenIdentifiers)
                    {
                        var tokenPath = Path.Combine(tempDirectory.Path, $"{tokenIdentifier.Name}.token");
                        if (IO.File.Exists(tokenPath))
                        {
                            ImportToken(tokenPath, settings, false);
                        }
                    }
                }

                settings.Save();
            }
        }
    }

    private static void ImportTokenGroup(string path, Settings settings)
    {
        using var tempDirectory = new TempDirectory();
        string tokenGroupFilePath = Path.Combine(tempDirectory.Path, "TokenGroup.json");

        IO.ZipFile.ExtractToDirectory(path, tempDirectory.Path);

        if (FileManager.OpenFile(tokenGroupFilePath, out TokenGroup tokenGroup))
        {
            if(settings.TokenGroups.SingleOrDefault(t => t.Name == tokenGroup.Name) == null)
            {
                settings.TokenGroups.Add(tokenGroup.Clone<TokenGroup>());

                foreach (var tokenName in tokenGroup.TokenNames)
                {
                    var tokenPath = Path.Combine(tempDirectory.Path, $"{tokenName}.token");
                    if (IO.File.Exists(tokenPath))
                    {
                        ImportToken(tokenPath, settings, false);
                    }
                }

                settings.Save();
            }
        }
    }

    private static void ImportToken(string path, Settings settings, bool saveSettings = true)
    {
        using var tempDirectory = new TempDirectory();
        string tokenFilePath = Path.Combine(tempDirectory.Path, "Token.json");
        string tokenImageFilePath = Path.Combine(tempDirectory.Path, "Image.png");
        string statblockFilePath = Path.Combine(tempDirectory.Path, "Markdown.md");

        IO.ZipFile.ExtractToDirectory(path, tempDirectory.Path);

        if (FileManager.OpenFile(tokenFilePath, out Token token, new DerivedClassJsonConverter<Statblock>()))
        {
            if (settings.CustomTokens.SingleOrDefault(t => t.Name == token.Name) == null)
            {
                var imagePath = Path.Combine(Constants.CustomTokensPath, $"{token.Name}.png");
                IO.File.Copy(tokenImageFilePath, imagePath);
                token.ImagePath = imagePath;

                if (token.Statblock is MarkdownStatblock markdownStatblock)
                {
                    var statblockMarkdownPath = Path.Combine(Constants.CustomTokensPath, $"{token.Name}.md");
                    IO.File.Copy(statblockFilePath, statblockMarkdownPath);
                    markdownStatblock.MarkdownPath = statblockMarkdownPath;
                }

                settings.CustomTokens.Add(token.Clone<Token>());
                if(saveSettings)
                    settings.Save();
            }
        }
    }
}
