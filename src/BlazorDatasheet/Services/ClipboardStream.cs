using System.Text.Json;
using BlazorDatasheet.Core.Interfaces;
using Microsoft.JSInterop;

namespace BlazorDatasheet.Services;

/// <summary>
/// Reads clipboard content the browser sent as a stream (see bdsClipboardStream), so that a large copy isn't
/// sent as one interop message over the Blazor Server size limit.
/// </summary>
internal static class ClipboardStream
{
    private const long MaxSize = 256L * 1024 * 1024;

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static async Task<ClipboardReadData?> Read(IJSStreamReference? reference)
    {
        if (reference == null)
            return null;

        await using (reference)
        {
            await using var stream = await reference.OpenReadStreamAsync(MaxSize);
            return await JsonSerializer.DeserializeAsync<ClipboardReadData>(stream, Options);
        }
    }
}
