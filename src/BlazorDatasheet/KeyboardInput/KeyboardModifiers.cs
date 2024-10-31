namespace BlazorDatasheet.KeyboardInput;

[Flags]
public enum KeyboardModifiers : short
{
    None = 0,
    Ctrl = 1,
    Shift = 2,
    Alt = 4,
    Meta = 8,
    Any = 16
}