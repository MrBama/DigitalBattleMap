using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Formatting = Newtonsoft.Json.Formatting;

namespace DigitalBattleMap.Utilities;

public static class FileManager
{
    public static bool OpenFile<T>(string path, out T data)
    {
        string text;
        try
        {
            text = IO.File.ReadAllText(path);
        }
        catch
        {
            data = default;
            return false;
        }

        data = JsonConvert.DeserializeObject<T>(text);
        return true;
    }

    public static bool OpenFile<T>(string path, out T data, params JsonConverter[] jsonConverters)
    {
        string text;
        try
        {
            text = IO.File.ReadAllText(path);
        }
        catch
        {
            data = default;
            return false;
        }

        data = JsonConvert.DeserializeObject<T>(text, jsonConverters);
        return true;
    }

    public static void SaveFile(object data, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!IO.Directory.Exists(directory))
        {
            IO.Directory.CreateDirectory(directory);
        }

        IO.File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
    }

    public abstract class CustomJsonConverter<T> : JsonConverter<T>
    {
    }

    public class DerivedClassJsonConverter<T> : CustomJsonConverter<T>
    {
        public override T? ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.Null)
            {
                var jObject = JObject.Load(reader);
                var type = jObject["Type"]!.ToObject<Type>();
                var jsonString = jObject.ToString();

                return (T)JsonConvert.DeserializeObject(jsonString, type)!;
            }

            return default;
        }

        public override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
