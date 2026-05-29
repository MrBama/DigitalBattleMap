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
        string tempDirectoryPath = Constants.TempDirectoryPath + "CampaignExport";
        string campaignFilePath = Path.Combine(tempDirectoryPath, "Campaign.json");

        using var tempDirectory = new TempDirectory();

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
                        Export(Path.Combine(tempDirectoryPath, tokenIdentifier.Name), customToken);
                    }
                }
            }
        }

        if (IO.File.Exists(path))
        {
            IO.File.Delete(path);
        }
        IO.ZipFile.CreateFromDirectory(tempDirectoryPath, path + ".campaign");
    }

    public static void Export(string path, TokenGroup tokenGroup, List<Token> customTokens)
    {
        string tempDirectoryPath = Constants.TempDirectoryPath + "TokenGroupExport";
        string tokenGroupFilePath = Path.Combine(tempDirectoryPath, "TokenGroup.json");

        using var tempDirectory = new TempDirectory();

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
                    Export(Path.Combine(tempDirectoryPath, tokenName), customToken);
                }
            }
        }

        if (IO.File.Exists(path))
        {
            IO.File.Delete(path);
        }
        IO.ZipFile.CreateFromDirectory(tempDirectoryPath, path + ".tokengroup");
    }

    public static void Export(string path, Token token)
    {
        string tempDirectoryPath = Constants.TempDirectoryPath + "TokenExport";
        string tokenFilePath = Path.Combine(tempDirectoryPath, "Token.json");
        string tokenImageFilePath = Path.Combine(tempDirectoryPath, "Image.png");
        string statblockFilePath = Path.Combine(tempDirectoryPath, "Markdown.md");

        using var tempDirectory = new TempDirectory();

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
        IO.ZipFile.CreateFromDirectory(tempDirectoryPath, path + ".token");
    }

    private static void ImportCampaign(string path, Settings settings)
    {
        string tempDirectoryPath = Constants.TempDirectoryPath + "CampaignImport";
        string campaignFilePath = Path.Combine(tempDirectoryPath, "Campaign.json");

        using var tempDirectory = new TempDirectory();
        IO.ZipFile.ExtractToDirectory(path, tempDirectoryPath);

        if (FileManager.OpenFile(campaignFilePath, out Campaign campaign))
        {
            if (settings.Campaigns.SingleOrDefault(t => t.Name == campaign.Name) == null)
            {
                settings.Campaigns.Add(campaign.Clone<Campaign>());

                foreach (var player in campaign.Players)
                {
                    foreach (var tokenIdentifier in player.TokenIdentifiers)
                    {
                        var tokenPath = Path.Combine(tempDirectoryPath, $"{tokenIdentifier.Name}.token");
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
        string tempDirectoryPath = Constants.TempDirectoryPath + "TokenGroupImport";
        string tokenGroupFilePath = Path.Combine(tempDirectoryPath, "TokenGroup.json");

        using var tempDirectory = new TempDirectory();
        IO.ZipFile.ExtractToDirectory(path, tempDirectoryPath);

        if (FileManager.OpenFile(tokenGroupFilePath, out TokenGroup tokenGroup))
        {
            if(settings.TokenGroups.SingleOrDefault(t => t.Name == tokenGroup.Name) == null)
            {
                settings.TokenGroups.Add(tokenGroup.Clone<TokenGroup>());

                foreach (var tokenName in tokenGroup.TokenNames)
                {
                    var tokenPath = Path.Combine(tempDirectoryPath, $"{tokenName}.token");
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
        string tempDirectoryPath = Constants.TempDirectoryPath + "TokenImport";
        string tokenFilePath = Path.Combine(tempDirectoryPath, "Token.json");
        string tokenImageFilePath = Path.Combine(tempDirectoryPath, "Image.png");
        string statblockFilePath = Path.Combine(tempDirectoryPath, "Markdown.md");

        using var tempDirectory = new TempDirectory();
        IO.ZipFile.ExtractToDirectory(path, tempDirectoryPath);

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
