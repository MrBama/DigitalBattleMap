using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Formatting = Newtonsoft.Json.Formatting;

namespace MonsterIndexer
{
    internal class Program
    {
        private static string _repositoryUsername = "5etools-mirror-3";
        private static string _repositoryNamePrefix = "5etools";
        private static string _legacyRepositoryNamePrefix = "5etools-2014";

        private static string _path = @"C:\Git\5etools-img-main\5etools-img-main\bestiary\tokens";
        private static string _legacyPath = @"C:\Git\5etools-2014-img-main\5etools-2014-img-main\bestiary\tokens";

        // Add paths to extra book JSON files here (e.g. https://5e.tools/managebrew.html export the book JSONs).
        // Each file must contain a "monster" array with "tokenHref.url" fields and a "_meta.sources[0].json" source code.
        // Examples (uncomment and fill in the real paths):
        private static List<string> _extraBookPaths = new()
         {
             @"C:\Git\Kobold Press; Tome of Beasts 1 (2023 Edition).json",
             @"C:\Git\MCDM Productions; Flee, Mortals!.json",
             @"C:\Git\MCDM Productions; Where Evil Lives.json",
             @"C:\Git\MCDM Productions; The Illrigger Revised.json",
             @"C:\Git\Keith Baker; Frontiers of Eberron Quickstone.json",
             @"C:\Git\Keith Baker; Chronicles of Eberron.json"
         };
        //private static List<string> _extraBookPaths = new();

        /* How to use:
         * 
         * 1. Download 5eTools img github repository https://github.com/5etools-mirror-3/5etools-2014-img
         * 1. Download 5eTools legacy img github repository https://github.com/5etools-mirror-3/5etools-img
         * 2. Extract repository and go to /bestiary/tokens
         * 3. Change _path and _legacyPath to .../bestiary/tokens path for both repositories
         * 4. Run this application
         * 5. A "MonsterTokens.json" file will be created in /bestiary/tokens
         */
        static void Main()
        {
            // Gather different books
            var books = GetBooks(_repositoryNamePrefix);
            var legacyBooks = GetBooks(_legacyRepositoryNamePrefix);

            // Remove duplicated books
            foreach (var legacyBook in legacyBooks.ToList())
            {
                var book = books.SingleOrDefault(b => b.Source == legacyBook.Source);
                if (book != null)
                {
                    books.Remove(book);
                }
            }

            // Create list of tokens with monster info
            var tokenData = CreateTokenData(books, _path, _repositoryNamePrefix);
            var legacyTokenData = CreateTokenData(legacyBooks, _legacyPath, _legacyRepositoryNamePrefix);

            // Combine tokens with legacy tokens
            foreach (var legacyToken in legacyTokenData.Tokens)
            {
                if(tokenData.ContainsToken(legacyToken.Name))
                {
                    var token = tokenData.Tokens.Single(t => t.Name == legacyToken.Name);
                    var legacyName = legacyToken.Name + " (2014)";

                    token.LegacyName = legacyName;
                    legacyToken.Name = legacyName;
                }

                tokenData.Tokens.Add(legacyToken);
                tokenData.Sources.Add(legacyToken.Source);
            }

            foreach (var extraBookPath in _extraBookPaths)
            {
                var extraTokenData = CreateTokenDataFromJsonFile(extraBookPath);

                foreach (var extraToken in extraTokenData.Tokens)
                {
                    if (tokenData.ContainsToken(extraToken.Name))
                    {
                        // Disambiguate using the book's source code, e.g. "Dragon (ToB1-2023)"
                        extraToken.Name = $"{extraToken.Name} ({extraToken.Source})";
                    }

                    tokenData.Tokens.Add(extraToken);
                    tokenData.Sources.Add(extraToken.Source);
                }
            }

            tokenData.Sort();
            tokenData.FillSources();
            File.WriteAllText(_path + @"\MonsterTokens.json", JsonConvert.SerializeObject(tokenData, Formatting.Indented));
        }

        private static List<Book> GetBooks(string repositoryNamePrefix)
        {
            var books = new List<Book>();

            using var httpClient = new HttpClient();
            var json = httpClient.GetStringAsync($"https://raw.githubusercontent.com/{_repositoryUsername}/{repositoryNamePrefix}-src/refs/heads/main/data/bestiary/index.json").Result;
            var parsedJson = Regex.Replace(json, @"[^0-9a-zA-Z:,-.]+", "");

            foreach (var pair in parsedJson.Split(','))
            {
                var values = pair.Split(':');
                if (!values[0].StartsWith("UA"))
                {
                    books.Add(new Book { Source = values[0], JsonLink = values[1] });
                }
            }

            return books;
        }

        private static TokenData CreateTokenData(List<Book> books, string pathToImages, string repositoryNamePrefix)
        {
            var monsterDictionary = new Dictionary<string, MonsterInfo>(StringComparer.OrdinalIgnoreCase);
            using var httpClient = new HttpClient();

            // Gather monsters from bestiary
            foreach (var book in books)
            {
                var json = httpClient.GetStringAsync($"https://raw.githubusercontent.com/{_repositoryUsername}/{repositoryNamePrefix}-src/refs/heads/main/data/bestiary/{book.JsonLink}").Result;
                var monsterRoot = JsonConvert.DeserializeObject<MonsterData>(json);
                foreach (var monster in monsterRoot!.Monster)
                {
                    monsterDictionary[monster.Name] = new MonsterInfo(monster, book.Source);
                }
            }

            // Gather Hp from copies
            foreach (var monsterInfo in monsterDictionary.Values)
            {
                if (monsterInfo.Hp == 0 && monsterInfo.CopyName != null)
                {
                    monsterInfo.Hp = monsterDictionary[monsterInfo.CopyName].Hp;
                }
            }

            // Index images on disk using monster data
            var data = new TokenData();
            foreach (var directory in Directory.GetDirectories(pathToImages))
            {
                foreach (var file in Directory.GetFiles(directory))
                {
                    var name = Path.GetFileNameWithoutExtension(file);

                    if (file.EndsWith(".webp") && monsterDictionary.ContainsKey(name) && !data.ContainsToken(name))
                    {
                        var source = Path.GetFileName(directory);

                        if (source == monsterDictionary[name].Source)
                        {
                            data.Tokens.Add(new Token
                            {
                                Name = name,
                                Size = monsterDictionary[name].Size,
                                Source = monsterDictionary[name].Source,
                                Hp = monsterDictionary[name].Hp,
                                TokenUrl = $"https://github.com/{_repositoryUsername}/{repositoryNamePrefix}-img/blob/main/bestiary/tokens/{source}/{Uri.EscapeDataString(name)}.webp?raw=true",
                                StatblockUrl = $"https://5e.tools/bestiary.html#{Uri.EscapeDataString(name)}_{monsterDictionary[name].Source}"
                            });
                        }
                    }
                }
            }

            data.FillSources();
            return data;
        }

        private static TokenData CreateTokenDataFromJsonFile(string jsonFilePath)
        {
            var json = File.ReadAllText(jsonFilePath);
            var bookFile = JsonConvert.DeserializeObject<ExtraBookFile>(json) ?? new ExtraBookFile();
            var source = bookFile.Meta.Sources.FirstOrDefault()?.Json ?? Path.GetFileNameWithoutExtension(jsonFilePath);

            var data = new TokenData();
            foreach (var monster in bookFile.Monster)
            {
                if (monster.TokenHref == null || monster.TokenHref.Type != "external")
                    continue;

                var size = monster.Size.Count > 0 ? monster.Size.First() : "M";
                var hp = monster.Hp.Average != 0 ? monster.Hp.Average
                       : int.TryParse(monster.Hp.Special, out var parsed) ? parsed : 0;

                data.Tokens.Add(new Token
                {
                    Name = monster.Name,
                    Size = size,
                    Source = source,
                    Hp = hp,
                    TokenUrl = EscapeTokenUrl(monster.TokenHref.Url),
                    StatblockUrl = $"https://5e.tools/bestiary.html#{Uri.EscapeDataString(monster.Name)}_{source}"
                });
            }

            data.FillSources();
            return data;
        }

        private static string EscapeTokenUrl(string url)
        {
            // Split the URL at the last '/' to separate the path from the filename
            var lastSlashIndex = url.LastIndexOf('/');
            if (lastSlashIndex == -1)
                return url;

            var basePath = url.Substring(0, lastSlashIndex + 1);
            var filename = url.Substring(lastSlashIndex + 1);

            // Escape the filename part (converts spaces to %20, etc.)
            return basePath + Uri.EscapeDataString(filename);
        }
    }

    public class Book
    {
        public string Source { get; set; } = "";
        public string JsonLink { get; set; } = "";
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
        public Hp Hp { get; set; } = new();
        public Copy _copy { get; set; } = new();
    }

    public class Hp
    {
        public int Average { get; set; }
        public string Formula { get; set; }
        public string Special { get; set; }
    }

    public class Copy
    {
        public string Name { get; set; }
        public string Source { get; set; }
    }

    public class Token
    {
        public string Name { get; set; } = "";
        public string LegacyName { get; set; } = null;
        public string Size { get; set; } = "";
        public string TokenUrl { get; set; } = "";
        public string StatblockUrl { get; set; } = "";
        public string Source { get; set; } = "";
        public int Hp { get; set; }
    }

    public class TokenData
    {
        public List<Token> Tokens { get; set; } = new List<Token>();
        public List<string> Sources { get; set; } = new List<string>();

        public bool ContainsToken(string name)
        {
            return Tokens.SingleOrDefault(t => t.Name == name) != null;
        }

        public void Sort()
        {
            Tokens = Tokens.OrderBy(t => t.Source).ToList();
        }

        public void FillSources()
        {
            Sources = Tokens.DistinctBy(t => t.Source).Select(t => t.Source).ToList();
        }
    }

    public class MonsterInfo
    {
        public MonsterInfo(Monster monster, string source)
        {
            Source = source;
            CopyName = monster._copy.Name;

            if (monster.Size.Count != 0)
            {
                Size = monster.Size.First();
            }
            else
            {
                Size = "M";
            }

            Hp = monster.Hp.Average;
            if (monster.Hp.Average == 0)
            {
                if (int.TryParse(monster.Hp.Special, out var parsedHp))
                {
                    Hp = parsedHp;
                }
            }
        }

        public string Size { get; set; }
        public string Source { get; set; }
        public int Hp { get; set; }

        [JsonIgnore]
        public string CopyName { get; set; }
    }

    public class ExtraBookFile
    {
        [JsonProperty("_meta")]
        public ExtraBookMeta Meta { get; set; } = new();
        public List<ExtraMonster> Monster { get; set; } = new();
    }

    public class ExtraBookMeta
    {
        public List<ExtraBookSource> Sources { get; set; } = new();
    }

    public class ExtraBookSource
    {
        public string Json { get; set; } = "";   // e.g. "ToB1-2023"
    }

    public class ExtraMonster
    {
        public string Name { get; set; } = "";
        public string Source { get; set; } = "";
        public List<string> Size { get; set; } = new();
        public Hp Hp { get; set; } = new();
        public TokenHref? TokenHref { get; set; }
    }

    public class TokenHref
    {
        public string Type { get; set; } = "";
        public string Url { get; set; } = "";
    }
}
