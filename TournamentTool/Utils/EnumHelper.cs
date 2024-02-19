using System.ComponentModel;
using System.Globalization;

namespace TournamentTool.Utils
{
    public class ValueDescription
    {
        public object? Value { get; set; }
        public object? Description { get; set; }
    }

    public static class EnumHelper
    {
        public static string Description(this Enum value)
        {
            var attributes = value.GetType().GetField(value.ToString())?.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes is { } && attributes.Any())
                return ((DescriptionAttribute)attributes.First()).Description;

            // If no description is found, the least we can do is replace underscores with spaces
            // You can add your own custom default formatting logic here
            TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
            return ti.ToTitleCase(ti.ToLower(value.ToString().Replace("_", " ")));
        }

        public static IEnumerable<ValueDescription> GetAllValuesAndDescriptions(Type t)
        {
            if (!t.IsEnum)
                throw new ArgumentException($"{nameof(t)} must be an enum type");

            return Enum.GetValues(t).Cast<Enum>().Select((e) => new ValueDescription() { Value = e, Description = e.Description() }).ToList();
        }
    }
}
