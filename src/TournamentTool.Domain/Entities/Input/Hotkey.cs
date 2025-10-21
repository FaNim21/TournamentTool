using TournamentTool.Domain.Enums;

namespace TournamentTool.Domain.Entities.Input;

public class Hotkey
{
    public KeyCode Key { get; }
    public ModifierKeys Modifiers { get; }
    public string Description { get; set; }

    
    public Hotkey(KeyCode key, ModifierKeys modifiers = ModifierKeys.None, string description = "")
    {
        Key = key;
        Modifiers = modifiers;
        Description = description;
    }

    public bool Matches(Hotkey other) => Matches(other.Key, other.Modifiers);
    public bool Matches(KeyCode key, ModifierKeys modifiers) => Key == key && Modifiers == modifiers;

    private string ToDisplayString()
    {
        if (Modifiers == ModifierKeys.None) return Key.ToString();

        var parts = new List<string>();
        if (Modifiers.HasFlag(ModifierKeys.Ctrl)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(ModifierKeys.Super)) parts.Add("Win");
        
        parts.Add(Key.ToString());
        return string.Join(" + ", parts);
    }
    public override string ToString() => ToDisplayString();   
}