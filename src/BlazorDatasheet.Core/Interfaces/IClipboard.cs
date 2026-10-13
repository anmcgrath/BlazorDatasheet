using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Interfaces;

/// <summary>
/// What is written to the system clipboard: plain text and, optionally, an HTML table.
/// </summary>
public record ClipboardWriteData(string Text, string? Html);

/// <summary>
/// What was read from the system clipboard.
/// </summary>
public class ClipboardReadData
{
    public string? Text { get; set; }
    public string? Html { get; set; }
}

public interface IClipboard
{
    /// <summary>
    /// Writes the region's cells to the system clipboard as text and HTML.
    /// </summary>
    public Task Copy(IRegion region, Sheet sheet);

    /// <summary>
    /// Writes to the system clipboard. Returns whether anything was written: the browser may refuse,
    /// for example outside a secure context.
    /// </summary>
    public Task<bool> Write(ClipboardWriteData data);

    /// <summary>
    /// Reads the system clipboard. Returns null when the browser refused.
    /// </summary>
    public Task<ClipboardReadData?> Read();
}
