using BlazorDatasheet.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static BlazorDatasheet.Util.JsInteropHelper;

namespace BlazorDatasheet.Services;

public class SheetPointerInputService : IAsyncDisposable
{
    private readonly ElementReference _sheetElement;
    private IJSObjectReference? _inputJs;
    private bool _isDisposed;
    private IJSRuntime Js { get; }

    public EventHandler<SheetPointerEventArgs>? PointerDown;
    public EventHandler<SheetPointerEventArgs>? PointerUp;
    public EventHandler<SheetPointerEventArgs>? PointerMove;
    public EventHandler<SheetPointerEventArgs>? PointerEnter;
    public EventHandler<SheetPointerEventArgs>? PointerDoubleClick;

    private DotNetObjectReference<SheetPointerInputService>? _dotNetObjectReference;

    public SheetPointerInputService(IJSRuntime js, ElementReference sheetElement)
    {
        _sheetElement = sheetElement;
        Js = js;
    }

    public async Task Init()
    {
        if (_isDisposed)
            return;

        var module =
            await Js.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorDatasheet/js/sheet-pointer-input.js");

        if (_isDisposed)
        {
            await DisposeJsObjectReferenceAsync(module);
            return;
        }

        DotNetObjectReference<SheetPointerInputService>? dotNetObjectReference = DotNetObjectReference.Create(this);

        try
        {
            var inputJs = await module.InvokeAsync<IJSObjectReference>(
                "getInputService",
                _sheetElement,
                dotNetObjectReference);

            if (_isDisposed)
            {
                await SafeDisposeInputJsAsync(inputJs);
                return;
            }

            await inputJs.InvokeVoidAsync(
                "registerPointerEvents",
                nameof(HandlePointerUp),
                nameof(HandlePointerDown),
                nameof(HandlePointerMove),
                nameof(HandlePointerCellEnter),
                nameof(HandlePointerDoubleClick));

            if (_isDisposed)
            {
                await SafeDisposeInputJsAsync(inputJs);
                return;
            }

            _inputJs = inputJs;
            _dotNetObjectReference = dotNetObjectReference;
            dotNetObjectReference = null;
        }
        finally
        {
            dotNetObjectReference?.Dispose();
            await DisposeJsObjectReferenceAsync(module);
        }
    }

    [JSInvokable(nameof(HandlePointerMove))]
    public void HandlePointerMove(SheetPointerEventArgs args)
    {
        PointerMove?.Invoke(this, args);
    }

    [JSInvokable(nameof(HandlePointerDown))]
    public void HandlePointerDown(SheetPointerEventArgs args)
    {
        PointerDown?.Invoke(this, args);
    }

    [JSInvokable(nameof(HandlePointerUp))]
    public void HandlePointerUp(SheetPointerEventArgs args)
    {
        PointerUp?.Invoke(this, args);
    }

    [JSInvokable(nameof(HandlePointerCellEnter))]
    public void HandlePointerCellEnter(SheetPointerEventArgs args)
    {
        PointerEnter?.Invoke(this, args);
    }

    [JSInvokable(nameof(HandlePointerDoubleClick))]
    public void HandlePointerDoubleClick(SheetPointerEventArgs args)
    {
        PointerDoubleClick?.Invoke(this, args);
    }

    public async ValueTask DisposeAsync()
    {
        _isDisposed = true;

        var inputJs = _inputJs;
        _inputJs = null;
        var dotNetObjectReference = _dotNetObjectReference;
        _dotNetObjectReference = null;

        try
        {
            if (inputJs != null)
                await SafeDisposeInputJsAsync(inputJs);
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            dotNetObjectReference?.Dispose();
        }
    }

    private static async Task SafeDisposeInputJsAsync(IJSObjectReference inputJs)
    {
        try
        {
            await inputJs.InvokeVoidAsync("dispose");
        }
        catch (JSDisconnectedException)
        {
            // Ignore disconnects during server-side component teardown.
        }

        await DisposeJsObjectReferenceAsync(inputJs);
    }
}