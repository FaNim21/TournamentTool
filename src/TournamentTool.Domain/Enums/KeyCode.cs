namespace TournamentTool.Domain.Enums;

[Flags]
public enum ModifierKeys
{
    None = 0,
    Alt = 1 << 0,
    Ctrl = 1 << 1,
    Shift = 1 << 2,
    Super = 1 << 3,
}

public enum KeyCode
{
    None,

    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    Up, Down, Left, Right,
    Enter, Escape, Space, Tab, Backspace, Delete,
    CapsLock, Insert, Home, End, PageUp, PageDown,
    OemTilde 
}