using System.Diagnostics;
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
        _ = AlertAndReportAsync(message);
    }

    private async Task AlertAndReportAsync(string message)
    {
        try
        {
            await _js.InvokeVoidAsync("alert", message);
        }
        catch (Exception ex)
        {
            // IDialogService has a synchronous contract; observe failures from the async interop call.
            Trace.TraceError($"BlazorDatasheet alert failed: {ex}");
        }
    }
}
