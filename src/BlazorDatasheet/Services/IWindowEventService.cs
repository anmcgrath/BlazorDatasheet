using BlazorDatasheet.Core.Events;
using Microsoft.AspNetCore.Components.Web;
using ClipboardEventArgs = BlazorDatasheet.Core.Events.ClipboardEventArgs;

namespace BlazorDatasheet.Services;

public interface IWindowEventService : IAsyncDisposable
{
    Task RegisterMouseEvent(string eventType, Func<MouseEventArgs, Task<bool>> handler);
    Task RegisterKeyEvent(string eventType, Func<KeyboardEventArgs, Task<bool>> handler);
    Task RegisterClipboardEvent(string eventType, Func<ClipboardEventArgs, Task<bool>> handler);
}