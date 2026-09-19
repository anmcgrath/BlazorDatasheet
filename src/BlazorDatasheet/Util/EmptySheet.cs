using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Util;

/// <summary>
/// The placeholder a component holds until its real sheet parameter arrives. Shared, because a
/// <see cref="Sheet"/> builds a whole workbook and its stores behind it and a page can hold many
/// components - and because nothing ever writes to the placeholder or subscribes to its events:
/// components subscribe in the "sheet changed" branch, which only ever runs for the real sheet.
/// </summary>
internal static class EmptySheet
{
    public static readonly Sheet Instance = new(0, 0);
}
