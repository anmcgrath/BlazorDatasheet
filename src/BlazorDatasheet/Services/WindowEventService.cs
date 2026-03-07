using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ClipboardEventArgs = BlazorDatasheet.Core.Events.ClipboardEventArgs;
using static BlazorDatasheet.Util.JsInteropHelper;

namespace BlazorDatasheet.Services;

/// <summary>
/// Assigns keyboard & mouse events to the browser "window" and allows us to respond to the events
/// </summary>
public class WindowEventService : IWindowEventService
{
    private readonly IJSRuntime? _js;
    private IJSObjectReference? _windowEventObj;
    private bool _isDisposed;
    private bool _isInitializing;

    private DotNetObjectReference<WindowEventService>? _dotNetHelper;

    private Dictionary<string, Func<MouseEventArgs, Task<bool>>>? _mouseEventListeners;
    private Dictionary<string, Func<KeyboardEventArgs, Task<bool>>>? _keyEventListeners;
    private Dictionary<string, Func<ClipboardEventArgs, Task<bool>>>? _clipboardEventListeners;

    public WindowEventService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task RegisterMouseEvent(string eventType, Func<MouseEventArgs, Task<bool>> handler,
        int throttleInMs = 0)
    {
        await CreateDotnetHelperIfNotExists();
        _mouseEventListeners ??= new();
        _mouseEventListeners.TryAdd(eventType, handler);
        await AddWindowEvent(eventType, nameof(HandleWindowMouseEvent), throttleInMs);
    }

    public async Task RegisterKeyEvent(string eventType, Func<KeyboardEventArgs, Task<bool>> handler)
    {
        await CreateDotnetHelperIfNotExists();
        _keyEventListeners ??= new();
        _keyEventListeners.TryAdd(eventType, handler);
        await AddWindowEvent(eventType, nameof(HandleWindowKeyEvent));
    }

    public async Task RegisterClipboardEvent(string eventType, Func<ClipboardEventArgs, Task<bool>> handler)
    {
        await CreateDotnetHelperIfNotExists();
        _clipboardEventListeners ??= new();
        _clipboardEventListeners.TryAdd(eventType, handler);
        await AddWindowEvent(eventType, nameof(HandleWindowClipboardEvent));
    }

    public async Task PreventDefault(string eventType)
    {
        await CreateDotnetHelperIfNotExists();
        if (_windowEventObj == null)
            return;
        await _windowEventObj.InvokeVoidAsync("preventDefault", eventType);
    }

    public async Task CancelPreventDefault(string eventType)
    {
        await CreateDotnetHelperIfNotExists();
        if (_windowEventObj == null)
            return;
        await _windowEventObj.InvokeVoidAsync("cancelPreventDefault", eventType);
    }

    private async Task CreateDotnetHelperIfNotExists()
    {
        if (_windowEventObj == null && _js != null && !_isDisposed && !_isInitializing)
        {
            _isInitializing = true;
            DotNetObjectReference<WindowEventService>? dotNetHelper = null;
            IJSObjectReference? module = null;

            try
            {
                module =
                    await _js.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorDatasheet/js/window-events.js");

                if (_isDisposed)
                    return;

                dotNetHelper = DotNetObjectReference.Create(this);
                var windowEventObj = await module.InvokeAsync<IJSObjectReference>("createWindowEventsService", dotNetHelper);

                if (_isDisposed)
                {
                    await DisposeJsObjectReferenceAsync(windowEventObj);
                    return;
                }

                _dotNetHelper = dotNetHelper;
                _windowEventObj = windowEventObj;
                dotNetHelper = null;
            }
            finally
            {
                _isInitializing = false;
                dotNetHelper?.Dispose();

                if (module != null)
                    await DisposeJsObjectReferenceAsync(module);
            }
        }
    }


    private async ValueTask AddWindowEvent(string evType, string jsInvokableName, int throttleInMs = 0)
    {
        if (_windowEventObj == null)
            return;

        await _windowEventObj.InvokeVoidAsync("registerEvent", evType, jsInvokableName, throttleInMs);
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
        _isDisposed = true;
        var windowEventObj = _windowEventObj;
        _windowEventObj = null;
        var dotNetHelper = _dotNetHelper;
        _dotNetHelper = null;

        try
        {
            if (windowEventObj != null)
            {
                try
                {
                    await windowEventObj.InvokeVoidAsync("dispose");
                }
                catch (JSDisconnectedException)
                {
                    // Ignore disconnects during server-side component teardown.
                }

                await DisposeJsObjectReferenceAsync(windowEventObj);
            }
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            dotNetHelper?.Dispose();
        }
    }

}
