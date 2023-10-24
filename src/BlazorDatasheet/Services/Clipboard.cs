using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;
using Microsoft.JSInterop;

namespace BlazorDatasheet.Services;

public class Clipboard : IClipboard
{
    public IJSRuntime JS;

    private string lastCopiedText;
    private IRegion lastCopiedRegion;

    public Clipboard(IJSRuntime jsRuntime)
    {
        JS = jsRuntime;
    }

    public async Task Copy(IRegion region, Sheet sheet)
    {
        if (region == null)
            return;
        
        var text = sheet.GetRegionAsDelimitedText(region);
        
        await JS.InvokeVoidAsync("writeTextToClipboard", text);
        
        lastCopiedRegion = region;
        lastCopiedText = text;

    }
}