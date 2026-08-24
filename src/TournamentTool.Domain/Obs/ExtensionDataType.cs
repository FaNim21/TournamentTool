namespace TournamentTool.Domain.Obs;

public enum ExtensionDataType
{
    sourceType,
    inputKind,
}

public enum SourceType
{
    OBS_SOURCE_TYPE_SCENE,
    OBS_SOURCE_TYPE_INPUT,
}

public enum InputKind
{
    unsupported,
    browser_source,
    text_gdiplus_v2,
    text_gdiplus_v3,
    image_source,
    group_source,
    
    // custom
    tt_point_of_view = 100,
    
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
        InputKind.text_gdiplus_v2,
        InputKind.text_gdiplus_v3,
    ];
    
    public static IEnumerable<InputKind> GetSupportedInputKinds(this InputKind inputKind) =>
        inputKind switch
        {
            InputKind.browser_source or InputKind.tt_point_of_view => Browser,
            InputKind.text_gdiplus_v2 or InputKind.text_gdiplus_v3 => Text,
            _ => []
        };

    public static bool IsSupportingBinding(this InputKind inputKind) =>
        inputKind switch
        {
            InputKind.tt_point_of_view => false,
            _ => true
        };
}