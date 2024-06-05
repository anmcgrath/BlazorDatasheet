using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Interfaces;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ClipboardEventArgs = BlazorDatasheet.Core.Events.ClipboardEventArgs;

namespace BlazorDatasheet.Services;

/// <summary>
/// Assigns keyboard & mouse events to the browser "window" and allows us to respond to the events
/// </summary>
public class WindowEventService : IWindowEventService
{
    private readonly IJSRuntime _js;
    private IJSObjectReference _windowEventObj = null!;

    private DotNetObjectReference<WindowEventService> _dotNetHelper;

    private Dictionary<string, Func<MouseEventArgs, Task<bool>>>? _mouseEventListeners;
    private Dictionary<string, Func<KeyboardEventArgs, Task<bool>>>? _keyEventListeners;
    private Dictionary<string, Func<ClipboardEventArgs, Task<bool>>>? _clipboardEventListeners;

    public WindowEventService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task RegisterMouseEvent(string eventType, Func<MouseEventArgs, Task<bool>> handler)
    {
        if (_mouseEventListeners == null)
            _mouseEventListeners = new();

        _mouseEventListeners.TryAdd(eventType, handler);
        await AddWindowEvent(eventType, nameof(HandleWindowMouseEvent));
    }

    public async Task RegisterKeyEvent(string eventType, Func<KeyboardEventArgs, Task<bool>> handler)
    {
        if (_keyEventListeners == null)
            _keyEventListeners = new();

        _keyEventListeners.TryAdd(eventType, handler);
        await AddWindowEvent(eventType, nameof(HandleWindowKeyEvent));
    }

    public async Task RegisterClipboardEvent(string eventType, Func<ClipboardEventArgs, Task<bool>> handler)
    {
        if (_clipboardEventListeners == null)
            _clipboardEventListeners = new();

        _clipboardEventListeners.TryAdd(eventType, handler);
        await AddWindowEvent(eventType, nameof(HandleWindowClipboardEvent));
    }


    private async ValueTask AddWindowEvent(string evType, string jsInvokableName)
    {
        if (_windowEventObj == null)
        {
            _dotNetHelper = DotNetObjectReference.Create(this);
            var module =
                await _js.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorDatasheet/js/window-events.js");
            _windowEventObj = await module.InvokeAsync<IJSObjectReference>("createWindowEvents", _dotNetHelper);
            await module.DisposeAsync();
        }

        await _windowEventObj.InvokeVoidAsync("registerEvent", evType, jsInvokableName);
    }

    [JSInvokable]
    public async Task<bool> HandleWindowMouseEvent(MouseEventArgs e)
    {
        if (_mouseEventListeners == null)
            return false;

        var hasListener = _mouseEventListeners.TryGetValue(e.Type, out var listener);
        if (!hasListener)
            return false;

        var result = await listener!.Invoke(e);
        return result;
    }

    [JSInvokable]
    public async Task<bool> HandleWindowKeyEvent(KeyboardEventArgs e)
    {
        if (_keyEventListeners == null)
            return false;

        var hasListener = _keyEventListeners.TryGetValue(e.Type, out var listener);
        if (!hasListener)
            return false;

        var result = await listener!.Invoke(e);
        return result;
    }

    [JSInvokable]
    public async Task<bool> HandleWindowClipboardEvent(ClipboardEventArgs e)
    {
        if (_clipboardEventListeners == null)
            return false;

        var hasListener = _clipboardEventListeners.TryGetValue(e.Type, out var listener);
        if (!hasListener)
            return false;

        var result = await listener!.Invoke(e);
        return result;
    }


    public async ValueTask DisposeAsync()
    {
        try
        {
            await _windowEventObj.InvokeVoidAsync("dispose");
            await _windowEventObj.DisposeAsync();
            _dotNetHelper?.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}

internal class WindowEventOptions
{
    public string MouseDownCallbackName { get; set; }
    public string MouseUpCallbackName { get; set; }
    public string MouseMoveCallbackName { get; set; }
    public string KeyDownCallbackName { get; set; }
    public string PasteCallbackName { get; set; }
}