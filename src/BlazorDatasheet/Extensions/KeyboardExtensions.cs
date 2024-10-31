using BlazorDatasheet.KeyboardInput;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorDatasheet.Extensions;

public static class KeyboardExtensions
{
    public static KeyboardModifiers GetModifiers(this KeyboardEventArgs args)
    {
        var modifiers = KeyboardModifiers.None;
        if (args.CtrlKey)
            modifiers |= KeyboardModifiers.Ctrl;
        if (args.ShiftKey)
            modifiers |= KeyboardModifiers.Shift;
        if (args.MetaKey)
            modifiers |= KeyboardModifiers.Meta;
        if (args.AltKey)
            modifiers |= KeyboardModifiers.Alt;
        return modifiers;
    }
}