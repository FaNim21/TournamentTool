using System.Text.Json.Serialization;

namespace TournamentTool.Domain.Entities.Preset;

public class TournamentPreset
{
    public string Name { get; set; }

    
    [JsonConstructor]
    public TournamentPreset(string name)
    {
        Name = name;
    }
}