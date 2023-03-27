using BlazorDatasheet.Events;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorDatasheet.Interfaces;

public interface IWindowEventService : IDisposable
{
    Task Init();
    event Func<KeyboardEventArgs, bool?> OnKeyDown;
    event Func<MouseEventArgs, bool>? OnMouseDown;
    event Func<MouseEventArgs, bool>? OnMouseUp;
    event Func<PasteEventArgs, Task>? OnPaste;
}