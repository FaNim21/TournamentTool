using System.Text.Json;
using System.Text.Json.Serialization;
using TournamentTool.Models;

namespace TournamentTool.Converters.JSON;

public class PrivRoomMatchStatusConverter: JsonConverter<MatchStatus>
{
    public override MatchStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string enumString = reader.GetString()!;
            if (Enum.TryParse(typeof(MatchStatus), enumString, ignoreCase: true, out object? result))
            {
                return (MatchStatus)result;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            int enumValue = reader.GetInt32();
            if (Enum.IsDefined(typeof(MatchStatus), enumValue))
            {
                return (MatchStatus)enumValue;
            }
        }
                                                 
        return MatchStatus.idle;
    }
                                         
    public override void Write(Utf8JsonWriter writer, MatchStatus value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}