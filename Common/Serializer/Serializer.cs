using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Serializer
{
    public class Serializer
    {
        public static AbstractModel? DeserializeConfig(string jsonFile)
        {
            try
            {
                string jsonContent = String.Empty;

                using (StreamReader sr = new StreamReader(jsonFile))
                {
                    jsonContent = sr.ReadToEnd();
                }

                return (AbstractModel) Deserialize<AbstractModel>(jsonContent, true);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static string Serialize<T>(object fileContent)
        {
            try
            {
                return JsonSerializer.Serialize((T)fileContent);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static object Deserialize<T>(string jsonContent, bool convertEnum)
        {
            try
            {
                var jsonOptions = new JsonSerializerOptions();
                if(convertEnum) jsonOptions.Converters.Add(new JsonStringEnumConverter());
                if (convertEnum) jsonOptions.Converters.Add(new RegexStringJsonConverter());
                if (convertEnum) jsonOptions.Converters.Add(new DictionaryRegexStringJsonConverter());

                return JsonSerializer.Deserialize<T>(jsonContent, jsonOptions);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

    public class RegexStringJsonConverter : JsonConverter<RegexString>
    {
        public override RegexString Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
                new RegexString(reader.GetString());

        public override void Write(
            Utf8JsonWriter writer,
            RegexString regexString,
            JsonSerializerOptions options) =>
                writer.WriteStringValue(regexString.ToString());
    }
    public class DictionaryRegexStringJsonConverter : JsonConverter<Dictionary<RegexString, RegexString?>>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(Dictionary<RegexString, RegexString>)
                   || typeToConvert == typeof(Dictionary<RegexString, RegexString?>);
        }

        public override Dictionary<RegexString, RegexString?> Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"JsonTokenType was of type {reader.TokenType}, only objects are supported");
            }

            var dictionary = new Dictionary<RegexString, RegexString?>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return dictionary;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("JsonTokenType was not PropertyName");
                }

                var propertyName = reader.GetString();

                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    throw new JsonException("Failed to get property name");
                }

                reader.Read();

                dictionary.Add(propertyName!, ExtractValue(ref reader, options));
            }

            return dictionary;
        }

        public override void Write(
            Utf8JsonWriter writer, Dictionary<RegexString, RegexString?> value, JsonSerializerOptions options)
        {
            // We don't need any custom serialization logic for writing the json.
            // Ideally, this method should not be called at all. It's only called if you
            // supply JsonSerializerOptions that contains this JsonConverter in it's Converters list.
            // Don't do that, you will lose performance because of the cast needed below.
            // Cast to avoid infinite loop: https://github.com/dotnet/docs/issues/19268
            JsonSerializer.Serialize(writer, (IDictionary<RegexString, RegexString?>)value, options);
        }

        private RegexString? ExtractValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Null:
                    return null;
                default:
                    throw new JsonException($"'{reader.TokenType}' is not supported");
            }
        }
    }
}
