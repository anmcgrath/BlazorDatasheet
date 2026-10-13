using BlazorDatasheet.Core.Clipboard;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;
using Microsoft.JSInterop;

namespace BlazorDatasheet.Services;

public class Clipboard : IClipboard
{
    private readonly IJSRuntime _js;

    public Clipboard(IJSRuntime jsRuntime)
    {
        _js = jsRuntime;
    }

    public async Task Copy(IRegion region, Sheet sheet)
    {
        var snapshot = ClipboardSnapshotBuilder.FromRange(sheet.Range(region));
        await Write(new ClipboardWriteData(DelimitedTextWriter.Write(snapshot), ClipboardHtmlWriter.Write(snapshot)));
    }

    public async Task<bool> Write(ClipboardWriteData data)
    {
        try
        {
            return await _js.InvokeAsync<bool>("bdsWriteClipboard", data.Text, data.Html);
        }
        catch (JSException)
        {
            return false;
        }
    }

    public async Task<ClipboardReadData?> Read()
    {
        try
        {
            return await ClipboardStream.Read(await _js.InvokeAsync<IJSStreamReference?>("bdsReadClipboardStream"));
        }
        catch (JSException)
        {
            return null;
        }
    }
}
