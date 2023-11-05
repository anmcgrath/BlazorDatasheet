using BlazorDatasheet.Core.Events;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorDatasheet.Services;

public interface IWindowEventService : IDisposable
{
    Task Init();
    event Func<KeyboardEventArgs, Task<bool>> OnKeyDown;
    event Func<MouseEventArgs, Task<bool>>? OnMouseDown;
    event Func<MouseEventArgs, Task<bool>>? OnMouseUp;
    event Func<PasteEventArgs, Task>? OnPaste;
    event Func<MouseEventArgs, Task<bool>>? OnMouseMove;
}