using BlazorDatasheet.Core.Interfaces;
using Microsoft.JSInterop;

namespace BlazorDatasheet.Services;

public class SimpleDialogService : IDialogService
{
    private readonly IJSRuntime _js;

    public SimpleDialogService(IJSRuntime js)
    {
        _js = js;
    }


    public void Alert(string message)
    {
        _js.InvokeVoidAsync("alert", message);
    }
}