using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Clipboard;

public static class ClipboardSnapshotBuilder
{
    /// <summary>
    /// Captures the cells of <paramref name="range"/> (clipped to its sheet) with their values, formulas,
    /// visible formats, types and merges.
    /// </summary>
    /// <param name="includeFormats">Whether cell formats are gathered. Delimited text needs none.</param>
    public static ClipboardSnapshot FromRange(SheetRange range, Guid? sourceId = null, bool includeFormats = true)
    {
        var sheet = range.Sheet;
        var region = range.Region.GetIntersection(sheet.Region);
        if (region == null)
            return new ClipboardSnapshot(0, 0) { SourceId = sourceId };

        var snapshot = new ClipboardSnapshot(region.Height, region.Width)
            { SourceId = sourceId, Origin = region.TopLeft };
        for (var row = region.Top; row <= region.Bottom; row++)
        for (var col = region.Left; col <= region.Right; col++)
        {
            var type = sheet.Cells.GetCellType(row, col);
            var cell = new ClipboardCell
            {
                Value = sheet.Cells.GetCellValue(row, col),
                Formula = sheet.Cells.GetFormulaString(row, col),
                Format = includeFormats ? ClipboardFormats.Portable(sheet.GetFormat(row, col)) : null,
                Type = type == "default" ? null : type
            };
            if (!cell.IsEmpty)
                snapshot[row - region.Top, col - region.Left] = cell;
        }

        foreach (var merge in sheet.Cells.GetMerges(region))
        {
            if (!region.Contains(merge))
                continue;
            var relative = merge.Clone();
            relative.Shift(-region.Top, -region.Left);
            snapshot.Merges.Add(relative);
        }

        return snapshot;
    }
}
