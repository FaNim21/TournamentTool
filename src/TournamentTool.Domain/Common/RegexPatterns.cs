using System.Text.RegularExpressions;

namespace TournamentTool.Domain.Common;

public static partial class RegexPatterns
{
    public static void cos()
    {
        // Regex regex = MyRegex();
    }
    

    [GeneratedRegex("[<>:\"/\\|?*]")]
    public static partial Regex SpecialCharacterPattern();

    [GeneratedRegex(@"^-?\d*$")] 
    public static partial Regex NumbersPattern();
    
    [GeneratedRegex(@"^\d*$")]
    public static partial Regex NumbersPatternDigitOnly();
    
    [GeneratedRegex(@"^-?\d*(\.\d*)?$")]
    public static partial Regex DecimalWithNegativePattern();

    [GeneratedRegex(@"^\d*(\.\d*)?$")]
    public static partial Regex DecimalPattern();
    
    [GeneratedRegex(@"chunk_0:\((\d+),")]
    public static partial Regex LuaErrorLogPattern();
    
    [GeneratedRegex(@"^[a-z0-9_]+$")]
    public static partial Regex TwitchValidLoginPattern();
}