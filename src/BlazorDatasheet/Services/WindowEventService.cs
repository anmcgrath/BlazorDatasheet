using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Interfaces;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorDatasheet.Services;

/// <summary>
/// Assigns keyboard & mouse events to the browser "window" and allows us to respond to the events
/// </summary>
public class WindowEventService : IWindowEventService
{
    private readonly IJSRuntime _js;
    private IJSObjectReference _windowEventObj = null!;

    private DotNetObjectReference<WindowEventService> _dotNetHelper;
    public event Func<KeyboardEventArgs, Task<bool>>? OnKeyDown;
    public event Func<MouseEventArgs, Task<bool>>? OnMouseDown;
    public event Func<MouseEventArgs, Task<bool>>? OnMouseUp;
    public event Func<MouseEventArgs, Task<bool>>? OnMouseMove;
    public event Func<PasteEventArgs, Task>? OnPaste;

    public WindowEventService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task Init()
    {
        _dotNetHelper = DotNetObjectReference.Create(this);
        var module =
            await _js.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorDatasheet/js/window-events.js");
        _windowEventObj = await module.InvokeAsync<IJSObjectReference>("createWindowEvents", _dotNetHelper);

        await AddWindowEvent("keydown", nameof(HandleWindowKeyDown));
        await AddWindowEvent("mousedown", nameof(HandleWindowMouseDown));
        await AddWindowEvent("mouseup", nameof(HandleWindowMouseUp));
        await AddWindowEvent("mousemove", nameof(HandleWindowMouseMove));
        await AddWindowEvent("paste", nameof(HandleWindowPaste));

        await module.DisposeAsync();
    }

    private async ValueTask AddWindowEvent(string evType, string jsInvokableName)
    {
        await _windowEventObj.InvokeVoidAsync("registerEvent", evType, jsInvokableName);
    }

    [JSInvokable]
    public async Task<bool?> HandleWindowKeyDown(KeyboardEventArgs e)
    {
        if (OnKeyDown == null)
            return false;

        var result = await OnKeyDown.Invoke(e);
        return result;
    }

    [JSInvokable]
    public async Task<bool> HandleWindowMouseDown(MouseEventArgs e)
    {
        if (OnMouseDown == null)
            return false;

        var result = await OnMouseDown.Invoke(e);
        return result;
    }

    [JSInvokable]
    public async Task<bool> HandleWindowMouseUp(MouseEventArgs e)
    {
        if (OnMouseUp == null)
            return false;

        var result = await OnMouseUp.Invoke(e);
        return result;
    }


    [JSInvokable]
    public async Task<bool> HandleWindowMouseMove(MouseEventArgs e)
    {
        if (OnMouseMove == null)
            return false;

        var result = await OnMouseMove.Invoke(e);
        return result;
    }

    [JSInvokable]
    public async Task HandleWindowPaste(PasteEventArgs e)
    {
        if (OnPaste == null)
            return;

        if (OnPaste is not null)
        {
            await OnPaste.Invoke(e);
        }
    }

    public async Task DisposeAsync()
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

    public async void Dispose()
    {
        await DisposeAsync();
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