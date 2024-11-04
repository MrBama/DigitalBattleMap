using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace DigitalBattleMap.Utilities;

public class GitHubReleaseAsset
{
    public string name { get; set; }
    public string browser_download_url { get; set; }

    public void Download(string path)
    {
        using var client = new HttpClient();
        using var stream = client.GetStreamAsync(browser_download_url).Result;
        using var fileStream = new FileStream(path, FileMode.OpenOrCreate);
        stream.CopyTo(fileStream);
    }
}

public class GithubReleaseInfo
{
    public string tag_name { get; set; }
    public string name { get; set; }
    public List<GitHubReleaseAsset> assets { get; set; }
}

public static class GitHub
{
    public static GithubReleaseInfo GetLatestReleaseInfo(string user, string repository)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "request");
        var json = httpClient.GetStringAsync($"https://api.github.com/repos/{user}/{repository}/releases/latest").Result;
        return JsonConvert.DeserializeObject<GithubReleaseInfo>(json)!;
    }
}
