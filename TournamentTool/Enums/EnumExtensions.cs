using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TournamentTool.Enums;

public static class EnumExtensions
{
    public static DisplayAttribute? GetDisplay(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field!.GetCustomAttribute<DisplayAttribute>();
        return attribute;
    } 
    
    public static T FromDescription<T>(string description) where T : Enum
    {
        foreach (var field in typeof(T).GetFields())
        {
            var attribute = field.GetCustomAttribute<DisplayAttribute>();
            if (attribute != null && attribute.Description == description)
            {
                return (T)field.GetValue(null)!;
            }
        }
        throw new ArgumentException($"No enum found for description: {description}", nameof(description));
    }
    
    public static T FromShortName<T>(string description) where T : Enum
    {
        foreach (var field in typeof(T).GetFields())
        {
            var attribute = field.GetCustomAttribute<DisplayAttribute>();
            if (attribute != null && attribute.ShortName == description)
            {
                return (T)field.GetValue(null)!;
            }
        }
        throw new ArgumentException($"No enum found for shortName: {description}", nameof(description));
    }
}