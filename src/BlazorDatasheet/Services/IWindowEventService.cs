using BlazorDatasheet.Core.Events;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorDatasheet.Services;

public interface IWindowEventService : IDisposable
{
    Task Init();
    event Func<KeyboardEventArgs, bool?> OnKeyDown;
    event Func<MouseEventArgs, bool>? OnMouseDown;
    event Func<MouseEventArgs, bool>? OnMouseUp;
    event Func<PasteEventArgs, Task>? OnPaste;
    event Func<MouseEventArgs, bool>? OnMouseMove;
}