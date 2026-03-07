using Microsoft.JSInterop;

namespace BlazorDatasheet.Util;

internal static class JsInteropHelper
{
    internal static async Task DisposeJsObjectReferenceAsync(IJSObjectReference jsObjectReference)
    {
        try
        {
            await jsObjectReference.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Ignore disconnects during server-side component teardown.
        }
    }
}
