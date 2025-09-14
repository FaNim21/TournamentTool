using System.Text.RegularExpressions;

namespace TournamentTool.Utils;

public static partial class RegexPatterns
{
    public static void cos()
    {
        //Regex regex = new("");
    }
    

    [GeneratedRegex("[<>:\"/\\|?*]")]
    public static partial Regex SpecialCharacterPattern();

    [GeneratedRegex(@"^-?\d*$")] 
    public static partial Regex NumbersPattern();
    
    [GeneratedRegex(@"^\d*$")]
    public static partial Regex NumbersPatternDigitOnly();
    
    [GeneratedRegex(@"chunk_0:\((\d+),")]
    public static partial Regex LuaErrorLogPattern();
}