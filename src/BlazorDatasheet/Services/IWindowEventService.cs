using Microsoft.AspNetCore.Components.Web;
using ClipboardEventArgs = BlazorDatasheet.Core.Events.ClipboardEventArgs;

namespace BlazorDatasheet.Services;

internal interface IWindowEventService : IAsyncDisposable
{
    /// <summary>
    /// Registers a window mouse event.
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    Task RegisterMouseEvent(string eventType, Func<MouseEventArgs, Task<bool>> handler);

    /// <summary>
    /// Registers a window key event
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    Task RegisterKeyEvent(string eventType, Func<KeyboardEventArgs, Task<bool>> handler);

    /// <summary>
    /// Registers a window clipboard event
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    Task RegisterClipboardEvent(string eventType, Func<ClipboardEventArgs, Task<bool>> handler);

    /// <summary>
    /// Causes the window events to prevent the default behaviour for type <paramref name="eventType"/>
    /// </summary>
    /// <param name="eventType">The type of events to prevent default behaviour for.</param>
    /// <param name="exclusions">If the event matches all the properties of any of the exclusions, prevent default will not be called.</param>
    /// <returns></returns>
    Task PreventDefault(string eventType, List<object>? exclusions = null);

    /// <summary>
    /// Stops the effect of <seealso cref="PreventDefault"/>
    /// </summary>
    /// <param name="eventType"></param>
    /// <returns></returns>
    Task CancelPreventDefault(string eventType);
}