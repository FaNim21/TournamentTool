using TournamentTool.Modules.Lua;

namespace TournamentTool.Models.Ranking;

public class LeaderboardSubRule
{
    public string LuaPath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Time { get; set; }
    public int BasePoints { get; set; }
    public int MaxWinners { get; set; } = -1;
    public bool Repeatable { get; set; }

    public Dictionary<string, LuaCustomVariable> CustomVariables { get; init; } = [];


    public bool EvaluateTime(int time)
    {
        return time <= Time;
    }

    public object? GetVariable(string name)
    {
        var variable = GetCustomVariable(name);

        return variable?.Type.ToLowerInvariant() switch
        {
            "number" => Convert.ToDouble(variable.Value),
            "string" => variable.Value ?? string.Empty,
            "bool" or "boolean" => Convert.ToBoolean(variable.Value),
            _ => variable?.Value
        };
    }
    public void SetVariable(string name, object value)
    {
        var variable = GetCustomVariable(name);
        if (variable == null) return;

        if (value == null)
        {
            variable.Value = variable.DefaultValue;
            return;
        }

        variable.Value = variable.Type.ToLowerInvariant() switch
        {
            "number" => value.ToString(),
            "string" => value.ToString() ?? string.Empty,
            "bool" or "boolean" => value.ToString(),
            _ => value.ToString()
        } ?? string.Empty;
    }

    public void UpdateCustomVariables(IReadOnlyList<LuaCustomVariable> customVariables)
    {
        HashSet<string> updatedVariables = [];
        
        foreach (var variable in customVariables)
        {
            if (CustomVariables.TryGetValue(variable.Name, out var existing))
            {
                existing.Update(variable);
            }
            else
            {
                CustomVariables[variable.Name] = new LuaCustomVariable(variable.Name, variable.Type, variable.DefaultValue, variable.Value);
            }

            updatedVariables.Add(variable.Name);
        }

        var variablesToRemove = CustomVariables.Keys.Where(k => !updatedVariables.Contains(k)).ToList();
        foreach (var variable in variablesToRemove)
        {
            CustomVariables.Remove(variable);
        }
    }
    
    private LuaCustomVariable? GetCustomVariable(string name)
    {
        return CustomVariables.GetValueOrDefault(name);
    }
}