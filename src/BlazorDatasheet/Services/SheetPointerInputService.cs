using BlazorDatasheet.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorDatasheet.Services;

public class SheetPointerInputService : IAsyncDisposable
{
    private readonly ElementReference _sheetElement;
    private IJSObjectReference _inputJs = null!;
    private IJSRuntime Js { get; }

    public EventHandler<SheetPointerEventArgs>? PointerDown;
    public EventHandler<SheetPointerEventArgs>? PointerUp;
    public EventHandler<SheetPointerEventArgs>? PointerMove;
    public EventHandler<SheetPointerEventArgs>? PointerEnter;
    public EventHandler<SheetPointerEventArgs>? PointerDoubleClick;

    private DotNetObjectReference<SheetPointerInputService> _dotNetObjectReference = null!;

    public SheetPointerInputService(IJSRuntime js, ElementReference sheetElement)
    {
        _sheetElement = sheetElement;
        Js = js;
    }

    public async Task Init()
    {
        _dotNetObjectReference = DotNetObjectReference.Create(this);
        var module =
            await Js.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorDatasheet/js/sheet-pointer-input.js");

        _inputJs = await module.InvokeAsync<IJSObjectReference>(
            "getInputService",
            _sheetElement,
            _dotNetObjectReference);

        await _inputJs.InvokeVoidAsync(
            "registerPointerEvents",
            nameof(HandlePointerUp),
            nameof(HandlePointerDown),
            nameof(HandlePointerMove),
            nameof(HandlePointerCellEnter),
            nameof(HandlePointerDoubleClick));

        await module.DisposeAsync();
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
        try
        {
            await _inputJs.InvokeVoidAsync("dispose");
            await _inputJs.DisposeAsync();
            _dotNetObjectReference.Dispose();
        }
        catch (Exception e)
        {
            // ignored
        }
    }
}