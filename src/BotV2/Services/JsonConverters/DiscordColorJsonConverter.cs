using System;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BotV2.Services.JsonConverters
{
    public class DiscordColorJsonConverter : JsonConverter<DiscordColor>
    {
        public override void WriteJson(JsonWriter writer, DiscordColor value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.Value);
        }

        public override DiscordColor ReadJson(JsonReader reader, Type objectType, DiscordColor existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return JToken.ReadFrom(reader) switch
            {
                JValue hexValue when hexValue.Type == JTokenType.String => new DiscordColor(hexValue.ToObject<string>(serializer)),
                JValue packedValue when packedValue.Type == JTokenType.Integer => new DiscordColor(packedValue.ToObject<int>(serializer)),
                _ => throw new InvalidOperationException($"Unable to parse as a {nameof(DiscordColor)}")
            };
        }
    }
}