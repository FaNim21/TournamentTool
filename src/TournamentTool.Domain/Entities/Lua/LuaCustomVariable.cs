namespace TournamentTool.Domain.Entities.Lua;

public class LuaCustomVariable
{
    public string Name { get; init; }
    public string Type { get; private set; }
    public string DefaultValue { get; private set; }
    
    public string Value { get; set; }


    public LuaCustomVariable(string name, string type, string defaultValue, string value)
    {
        Name = name;
        Type = type;
        DefaultValue = defaultValue;
        Value = value;
    }
    
    public void Update(LuaCustomVariable otherVariable)
    {
        if (Type != otherVariable.Type)
        {
            Value = otherVariable.DefaultValue;
        }
        Type = otherVariable.Type;
        DefaultValue = otherVariable.DefaultValue;
    }
}

