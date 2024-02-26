using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Input;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Events;
using BlazorDatasheet.Render;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorDatasheet.Services;

public class MouseInputService : IInputService, IAsyncDisposable
{
    private readonly Sheet _sheet;
    private readonly ElementReference _innerSheetRef;
    private readonly IJSRuntime _js;
    private readonly Viewport _viewport;
    public event EventHandler<InputOverCellEventArgs>? InputOverCell;

    private readonly DotNetObjectReference<MouseInputService> _dotnetHelper;

    public MouseInputService(Sheet sheet, ElementReference innerSheetRef, IJSRuntime js, Viewport viewport)
    {
        _sheet = sheet;
        _innerSheetRef = innerSheetRef;
        _js = js;
        _viewport = viewport;
        _dotnetHelper = DotNetObjectReference.Create(this);
    }

    public async Task Init()
    {
    }

    public async Task<Point2d> GetInputPositionAsync()
    {
        var res = await _js.InvokeAsync<InnerSheetMouseEventArgs>("getRelativeMousePosition", _innerSheetRef);
        return new Point2d(res.X + _viewport.Left, res.Y + _viewport.Top);
    }

    public InputOverCellEventArgs OnMouseOverCell(int row, int col)
    {
        var args = new InputOverCellEventArgs(row, col);
        InputOverCell?.Invoke(this, args);
        return args;
    }

    public async ValueTask DisposeAsync()
    {
        _dotnetHelper.Dispose();
        await _js.InvokeAsync<string>("removeSheetMousePositionListener", _innerSheetRef);
    }
}