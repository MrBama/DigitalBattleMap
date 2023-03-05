using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DigitalBattleMap
{
    public static class MonsterTokens
    {
        public static MonsterTokenData GetRawData()
        {
            var json = "";
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DigitalBattleMap.Resources.MonsterTokens.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }
            }

            var data = JsonConvert.DeserializeObject<MonsterTokenData>(json);
            return data ?? new MonsterTokenData();
        }

        public static List<Token> GetTokens()
        {
            var tokens = new List<Token>();
            var rawData = GetRawData();
            foreach (var file in Directory.GetFiles(Constants.MonsterTokensPath))
            {
                var tokenName = Path.GetFileNameWithoutExtension(file);
                var monsterToken = rawData.Tokens.SingleOrDefault(t => t.Name == tokenName);
                if(monsterToken != null)
                {
                    var token = new Token();
                    token.Name = tokenName;
                    token.Size = ConvertSize(monsterToken.Size);
                    token.ImagePath = file;

                    tokens.Add(token);
                }
            }

            return tokens;
        }

        private static TokenSize ConvertSize(string size)
        {
            switch(size)
            {
                case "T":
                    return TokenSize.Tiny;
                case "S":
                    return TokenSize.Small;
                case "M":
                    return TokenSize.Medium;
                case "L":
                    return TokenSize.Large;
                case "H":
                    return TokenSize.Huge;
                case "G":
                    return TokenSize.Gargantuan;
                default:
                    return TokenSize.Medium;
            }
        }
    }

    public class MonsterToken
    {
        public string Name { get; set; } = "";
        public string Size { get; set; } = "";
        public string TokenUrl { get; set; } = "";
        public string Source { get; set; } = "";
    }

    public class MonsterTokenData
    {
        public List<MonsterToken> Tokens { get; set; } = new List<MonsterToken>();
        public List<string> Sources { get; set; } = new List<string>();
    }
}
