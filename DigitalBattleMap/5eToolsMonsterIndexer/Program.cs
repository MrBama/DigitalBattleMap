using Newtonsoft.Json;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace MonsterIndexer
{
    internal class Program
    {
        private static string _path = @"C:\RepositoryRoot\img";

        /* How to use:
         * 
         * 1. Download 5eTools github repository https://github.com/5etools-mirror-1/5etools-mirror-1.github.io
         * 2. Extract repository and go to \img
         * 3. Remove all files and directory that don't need to be indexed
         * 4. Change _path to \img path
         * 5. Run this application
         * 6. A "MonsterTokens.json" file will be created in \img
         */
        static void Main(string[] args)
        {
            using (var httpClient = new HttpClient())
            {
                var json = httpClient.GetStringAsync("https://raw.githubusercontent.com/5etools-mirror-1/5etools-mirror-1.github.io/master/data/bestiary/index.json").Result;
                var parsedJson = Regex.Replace(json, @"[^0-9a-zA-Z:,-.]+", "");

                // Gather different books
                var books = new List<Book>();
                var monsterDictionary = new Dictionary<string, string>();

                foreach (var pair in parsedJson.Split(','))
                {
                    var values = pair.Split(':');
                    books.Add(new Book { Source = values[0], Json = values[1] });
                }

                // Gather monsters from bestiary
                foreach (var book in books)
                {
                    json = httpClient.GetStringAsync($"https://raw.githubusercontent.com/5etools-mirror-1/5etools-mirror-1.github.io/master/data/bestiary/{book.Json}").Result;
                    var monsterRoot = JsonConvert.DeserializeObject<MonsterData>(json);
                    foreach (var monster in monsterRoot.Monster)
                    {
                        if (monster.Size.Count != 0)
                        {
                            monsterDictionary[monster.Name] = monster.Size.First();
                        }
                        else
                        {
                            monsterDictionary[monster.Name] = "M";
                        }
                    }
                }

                // Index images on disk using monster data
                var data = new TokenData();
                foreach (var directory in Directory.GetDirectories(_path))
                {
                    foreach (var file in Directory.GetFiles(directory))
                    {
                        var name = Path.GetFileNameWithoutExtension(file);

                        if (file.EndsWith(".png") && monsterDictionary.ContainsKey(name) && !data.ContainsToken(name))
                        {
                            var source = Path.GetFileName(directory);

                            data.Tokens.Add(new Token
                            {
                                Name = name,
                                Size = monsterDictionary[name],
                                Source = source,
                                TokenUrl = $"https://github.com/5etools-mirror-1/5etools-mirror-1.github.io/blob/master/img/{source}/{Uri.EscapeDataString(name)}.png?raw=true"
                            });
                        }
                    }
                }

                data.FillSources();
                File.WriteAllText(_path + @"\MonsterTokens.json", JsonConvert.SerializeObject(data, Formatting.Indented));
            }
        }
    }

    public class Book
    {
        public string Source { get; set; } = "";
        public string Json { get; set; } = "";
    }

    public class MonsterData
    {
        public List<Monster> Monster { get; set; } = new List<Monster>();
    }

    public class Monster
    {
        public string Name { get; set; } = "";
        public bool HasToken { get; set; }
        public List<string> Size { get; set; } = new List<string>();
    }

    public class Token
    {
        public string Name { get; set; } = "";
        public string Size { get; set; } = "";
        public string TokenUrl { get; set; } = "";
        public string Source { get; set; } = "";
    }

    public class TokenData
    {
        public List<Token> Tokens { get; set; } = new List<Token>();
        public List<string> Sources { get; set; } = new List<string>();

        public bool ContainsToken(string name)
        {
            return Tokens.SingleOrDefault(t => t.Name == name) != null;
        }

        public void FillSources()
        {
            Sources = Tokens.DistinctBy(t => t.Source).Select(t => t.Source).ToList();
        }
    }
}
