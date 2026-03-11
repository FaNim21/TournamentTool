namespace TournamentTool.Domain.Obs;

public enum ExtensionDataType
{
    sourceType,
    inputKind,
}

public enum SourceType
{
    OBS_SOURCE_TYPE_SCENE,
}

public enum InputKind
{
    unsupported,
    browser_source,
    text_gdiplus_v2,
    text_gdiplus_v3,
    
    // custom
    tt_point_of_view,
    
}

public static class InputKindSupportedGroupValues
{
    public static IEnumerable<InputKind> Browser =>
    [
        InputKind.browser_source,
        InputKind.tt_point_of_view
    ];
    
    public static IEnumerable<InputKind> Text =>
    [
        InputKind.text_gdiplus_v3,
    ];
}