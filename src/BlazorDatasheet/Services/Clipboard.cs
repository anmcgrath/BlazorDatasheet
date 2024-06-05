using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;
using Microsoft.JSInterop;

namespace BlazorDatasheet.Services;

public class Clipboard : IClipboard
{
    private readonly IJSRuntime _js;

    private string _lastCopiedText;
    private IRegion _lastCopiedRegion;

    public Clipboard(IJSRuntime jsRuntime)
    {
        _js = jsRuntime;
    }

    public async Task Copy(IRegion? region, Sheet sheet)
    {
        if (region == null)
            return;
        
        var text = sheet.GetRegionAsDelimitedText(region);
        
        await _js.InvokeVoidAsync("writeTextToClipboard", text);
        
        _lastCopiedRegion = region;
        _lastCopiedText = text;

    }
}