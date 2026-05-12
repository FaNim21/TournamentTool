namespace TournamentTool.Domain.Obs;

//TODO: 3 Zrobic BindingScheme jako abstract i wtedy dzieci beda definiowac binding scheme zaleznie od pov, leaderboard, itp itd w przyszlosci
// to trzeba jednak zrobic pozniej przez to jak czytane jest to w edytorze itp itd

//Punkt jest taki, ze jak zrobie klasy zaleznie od typu schema to moge wtedy ewentualnie ustalac mozliwosci w xaml ze strony typu zaladowanej schemy czy bindingu
// poniewaz i tak wtedy scene item type mialby support ogolnie do bindingu, a reszta by sie przelaczala ze wzgledu na typ bindingu

public sealed record BindingSchema(string Source, string Field, bool haveName, bool haveIndex)
{
    public static BindingSchema New(string Source, string Field, bool haveName = false, bool haveIndex = false) => new(Source, Field.ToLower(), haveName, haveIndex);
}

public sealed record BindingKey(string Source, string Field, string? Name = null, int? Index = null)
{
    public static BindingKey New(string Source, string Field, string? Pov = null, int? Index = null) => new(Source, Field.ToLower(), Pov?.ToLower(), Index);
    
    public static BindingKey Empty() => new(string.Empty, string.Empty);
}

public static class BindingKeyHelper
{
    public static bool IsEmpty(this BindingKey bindingKey)
    {
        return bindingKey == null || 
               (string.IsNullOrEmpty(bindingKey.Source) &&
                string.IsNullOrEmpty(bindingKey.Field) &&
                bindingKey.Name == null &&
                bindingKey.Index == null);
    }
}