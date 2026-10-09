using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Globalization;

public partial class MainResponse
{
    [JsonProperty("response")]
    public Response Response { get; set; }


}

public partial class Response
{
    [JsonProperty("success")]
    [JsonConverter(typeof(ParseStringConverter))]
    public bool Success { get; set; }

    [JsonProperty("ticket")]
    public string Ticket { get; set; }

    public bool UseSandbox { get; set; }
    public string StoreId { get; set; }
    public string ApiToken { get; set; }
    public string Sandbox { get; set; }
}

public partial class MainResponse
{
    public static MainResponse FromJson(string json) => JsonConvert.DeserializeObject<MainResponse>(json, Converter.Settings);
}

public static class Serialize
{
    public static string ToJson(this MainResponse self) => JsonConvert.SerializeObject(self, Converter.Settings);
}

internal static class Converter
{
    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters =
      {
        new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
      },
    };
}

internal class ParseStringConverter : JsonConverter
{
    public override bool CanConvert(Type t) => t == typeof(bool) || t == typeof(bool?);

    public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;
        var value = serializer.Deserialize<string>(reader);
        bool b;
        if (Boolean.TryParse(value, out b))
        {
            return b;
        }
        throw new Exception("Cannot unmarshal type bool");
    }

    public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
    {
        if (untypedValue == null)
        {
            serializer.Serialize(writer, null);
            return;
        }
        var value = (bool)untypedValue;
        var boolString = value ? "true" : "false";
        serializer.Serialize(writer, boolString);
        return;
    }

    public static readonly ParseStringConverter Singleton = new ParseStringConverter();
}