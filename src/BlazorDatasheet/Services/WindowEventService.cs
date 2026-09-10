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
    private Task? _initTask;

    private DotNetObjectReference<WindowEventService>? _dotNetHelper;

    private Dictionary<string, Func<MouseEventArgs, Task<bool>>>? _mouseEventListeners;
    private Dictionary<string, Func<KeyboardEventArgs, Task<bool>>>? _keyEventListeners;
    private Dictionary<string, Func<ClipboardEventArgs, Task<bool>>>? _clipboardEventListeners;

    public WindowEventService(IJSRuntime js)
    {
        _js = js;
    }

    private Func<SheetFocusEventArgs, Task>? _focusHandler;

    public async Task ConfigureFocus(Microsoft.AspNetCore.Components.ElementReference container, Func<SheetFocusEventArgs, Task> handler)
    {
        await CreateDotnetHelperIfNotExists();
        if (_isDisposed || _windowEventObj == null) return;
        _focusHandler = handler;
        await _windowEventObj.InvokeVoidAsync("configureFocus", container, nameof(HandleFocusChanged));
    }

    [JSInvokable]
    public Task HandleFocusChanged(SheetFocusEventArgs focus) =>
        !_isDisposed && _focusHandler != null ? _focusHandler(focus) : Task.CompletedTask;

    public async Task SetInputState(bool active, bool editing, long revision, long focusVersion)
    {
        await CreateDotnetHelperIfNotExists();
        if (!_isDisposed && _windowEventObj != null)
            await _windowEventObj.InvokeVoidAsync("setInputState", active, editing, revision, focusVersion);
    }

    public async Task RestoreFocus()
    {
        if (!_isDisposed && _windowEventObj != null)
            await _windowEventObj.InvokeVoidAsync("restoreFocus");
    }

    public async Task RegisterMouseEvent(string eventType, Func<MouseEventArgs, Task<bool>> handler,
        int throttleInMs = 0)
    {
        await CreateDotnetHelperIfNotExists();
        if (_isDisposed) return;
        _mouseEventListeners ??= new();
        _mouseEventListeners[eventType] = handler;
        await AddWindowEvent(eventType, nameof(HandleWindowMouseEvent), throttleInMs);
    }

    public async Task RegisterKeyEvent(string eventType, Func<KeyboardEventArgs, Task<bool>> handler)
    {
        await CreateDotnetHelperIfNotExists();
        if (_isDisposed) return;
        _keyEventListeners ??= new();
        _keyEventListeners[eventType] = handler;
        await AddWindowEvent(eventType, nameof(HandleWindowKeyEvent));
    }

    public async Task RegisterClipboardEvent(string eventType, Func<ClipboardEventArgs, Task<bool>> handler)
    {
        await CreateDotnetHelperIfNotExists();
        if (_isDisposed) return;
        _clipboardEventListeners ??= new();
        _clipboardEventListeners[eventType] = handler;
        await AddWindowEvent(eventType, nameof(HandleWindowClipboardEvent));
    }

    // Cached so that callers arriving while initialisation is in flight wait for it to
    // finish rather than skipping registration with a null _windowEventObj.
    private Task CreateDotnetHelperIfNotExists() => _initTask ??= InitCoreAsync();

    private async Task InitCoreAsync()
    {
        if (_windowEventObj != null || _js == null || _isDisposed)
            return;

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
            dotNetHelper?.Dispose();

            if (module != null)
                await DisposeJsObjectReferenceAsync(module);
        }
    }


    private async ValueTask AddWindowEvent(string evType, string jsInvokableName, int throttleInMs = 0)
    {
        if (_isDisposed || _windowEventObj == null)
            return;

        await _windowEventObj.InvokeVoidAsync("registerEvent", evType, jsInvokableName, throttleInMs);
    }

    [JSInvokable]
    public async Task<bool> HandleWindowMouseEvent(MouseEventArgs e)
    {
        if (_isDisposed || _mouseEventListeners == null)
            return false;

        var hasListener = _mouseEventListeners.TryGetValue(e.Type, out var listener);
        if (!hasListener)
            return false;

        var result = await listener!.Invoke(e);
        return result;
    }

    [JSInvokable]
    public async Task<bool> HandleWindowKeyEvent(SheetKeyboardEventArgs e)
    {
        if (_isDisposed || _keyEventListeners == null)
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
        if (_isDisposed || _clipboardEventListeners == null)
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
        _initTask = null;
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
