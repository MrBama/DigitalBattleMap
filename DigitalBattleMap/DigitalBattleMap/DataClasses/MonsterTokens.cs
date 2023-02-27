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
