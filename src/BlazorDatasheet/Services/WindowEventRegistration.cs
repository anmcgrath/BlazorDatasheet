using Microsoft.AspNetCore.Components.Web;
using ClipboardEventArgs = BlazorDatasheet.Core.Events.ClipboardEventArgs;

namespace BlazorDatasheet.Services;

/// <summary>
/// One window event to register, for <see cref="IWindowEventService.RegisterEvents"/>. Exactly one
/// of the handlers is set; which one decides the JS invokable the event is dispatched to.
/// </summary>
public readonly struct WindowEventRegistration
{
    public string EventType { get; private init; }
    public int ThrottleInMs { get; private init; }
    public Func<MouseEventArgs, Task<bool>>? MouseHandler { get; private init; }
    public Func<KeyboardEventArgs, Task<bool>>? KeyHandler { get; private init; }
    public Func<ClipboardEventArgs, Task<bool>>? ClipboardHandler { get; private init; }

    public static WindowEventRegistration Mouse(string eventType, Func<MouseEventArgs, Task<bool>> handler,
        int throttleInMs = 0) =>
        new() { EventType = eventType, MouseHandler = handler, ThrottleInMs = throttleInMs };

    public static WindowEventRegistration Key(string eventType, Func<KeyboardEventArgs, Task<bool>> handler,
        int throttleInMs = 0) =>
        new() { EventType = eventType, KeyHandler = handler, ThrottleInMs = throttleInMs };

    public static WindowEventRegistration Clipboard(string eventType, Func<ClipboardEventArgs, Task<bool>> handler,
        int throttleInMs = 0) =>
        new() { EventType = eventType, ClipboardHandler = handler, ThrottleInMs = throttleInMs };
}
