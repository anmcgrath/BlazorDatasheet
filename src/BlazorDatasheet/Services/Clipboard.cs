using BlazorDatasheet.Interfaces;
using Microsoft.JSInterop;

namespace BlazorDatasheet.Services;

public class Clipboard : IClipboard
{
    public IJSRuntime JS;

    public Clipboard(IJSRuntime jsRuntime)
    {
        JS = jsRuntime;
    }

    public async Task<string> ReadTextAsync()
    {
        Console.WriteLine("Attempting to read text async");
        return await JS.InvokeAsync<string>("readTextAsync");
    }

    public async Task WriteTextAsync(string text)
    {
        throw new NotImplementedException();
    }
}