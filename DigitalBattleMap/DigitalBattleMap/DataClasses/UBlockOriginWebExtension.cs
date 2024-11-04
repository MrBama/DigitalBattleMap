using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.IO;
using System.Linq;

namespace DigitalBattleMap.DataClasses;

public class UBlockOriginWebExtension : IWebExtension
{
    private static readonly string _tempDirectoryPath = Constants.TempDirectoryPath + "WebExtension";
    private static readonly string _webExtensionPath = Path.Combine(Constants.WebExtensionsPath, "uBlock Origin");
    private const string user = "gorhill";
    private const string repository = "uBlock";

    public string Name { get; set; } = "uBlock Origin";
    public string Version { get; set; } = "";

    public bool Install()
    {
        try
        {
            var releaseInfo = GitHub.GetLatestReleaseInfo(user, repository);
            Download(releaseInfo);
            Version = releaseInfo.tag_name;
        }
        catch (Exception exception)
        {
            exception.Log();
            return false;
        }

        return true;
    }

    public bool IsUpdateAvailable(string currentVersion)
    {
        var releaseInfo = GitHub.GetLatestReleaseInfo(user, repository);
        return releaseInfo.tag_name != currentVersion;
    }

    public bool Update(string currentVersion)
    {
        try
        {
            var releaseInfo = GitHub.GetLatestReleaseInfo(user, repository);
            if (releaseInfo.tag_name != currentVersion)
            {
                // Update required
                Download(releaseInfo);
            }
            Version = releaseInfo.tag_name;
        }
        catch (Exception exception)
        {
            exception.Log();
            return false;
        }

        return true;
    }

    private void Download(GithubReleaseInfo releaseInfo)
    {
        var asset = releaseInfo.assets.SingleOrDefault(a => a.name.Contains("chromium"));
        if (asset != null)
        {
            using var tempDirectory = new TempDirectory(_tempDirectoryPath);
            var zipFilePath = Path.Combine(_tempDirectoryPath, "WebExtension.zip");
            asset.Download(zipFilePath);

            IO.ZipFile.ExtractToDirectory(zipFilePath, _tempDirectoryPath);

            if (IO.Directory.Exists(_webExtensionPath))
            {
                IO.Directory.Delete(_webExtensionPath, true);
            }

            IO.Directory.CreateDirectory(_webExtensionPath);
            IO.Directory.Copy(Path.Combine(_tempDirectoryPath, "uBlock0.chromium"), _webExtensionPath, true);
        }
    }
}
