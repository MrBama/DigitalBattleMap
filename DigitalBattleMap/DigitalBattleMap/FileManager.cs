using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace DigitalBattleMap
{
    public static class FileManager
    {
        public static bool OpenFile<T>(string path, out T data)
        {
            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch
            {
                data = default(T);
                return false;
            }

            data = JsonConvert.DeserializeObject<T>(text);
            return true;
        }

        public static void SaveFile(object data, string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
        }
    }
}
