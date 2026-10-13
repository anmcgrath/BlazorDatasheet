using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Clipboard;

/// <summary>
/// One cell of a <see cref="ClipboardSnapshot"/>.
/// </summary>
public sealed class ClipboardCell
{
    /// <summary>
    /// The typed value, when the source gave one. Empty when only <see cref="RawText"/> is known.
    /// </summary>
    public CellValue Value { get; init; } = CellValue.Empty;

    /// <summary>
    /// Untyped text from an external source. It is converted to a value at the cell it is pasted into, so
    /// the cell's type and the sheet's conversion rules apply.
    /// </summary>
    public string? RawText { get; init; }

    /// <summary>
    /// The formula text, including the leading '='.
    /// </summary>
    public string? Formula { get; init; }

    public CellFormat? Format { get; init; }

    /// <summary>
    /// The cell type, when it is not the default.
    /// </summary>
    public string? Type { get; init; }

    public bool HasValue => !Value.IsEmpty || !string.IsNullOrEmpty(RawText) || Formula != null;

    public bool IsEmpty => !HasValue && (Format == null || Format.IsDefaultFormat()) && Type == null;
}

/// <summary>
/// A detached grid of cells, with merges relative to its top-left. It is what goes to and comes from the
/// system clipboard; the workbook's own copy/paste works on the live cells instead.
/// </summary>
public sealed class ClipboardSnapshot
{
    public int Height { get; }
    public int Width { get; }

    private readonly ClipboardCell?[][] _cells;

    /// <summary>
    /// Merged regions, relative to the top-left of the snapshot.
    /// </summary>
    public List<IRegion> Merges { get; } = new();

    /// <summary>
    /// The id of the workbook clipboard entry the snapshot was taken from, if any.
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// Where the snapshot's top-left came from on its source sheet, if known. Formulas are shifted by the
    /// difference between this and where they are pasted, so relative references behave as in Excel.
    /// </summary>
    public CellPosition? Origin { get; set; }

    public ClipboardSnapshot(int height, int width)
    {
        Height = Math.Max(0, height);
        Width = Math.Max(0, width);
        _cells = new ClipboardCell?[Height][];
        for (var r = 0; r < Height; r++)
            _cells[r] = new ClipboardCell?[Width];
    }

    public ClipboardCell? this[int row, int col]
    {
        get => row >= 0 && row < Height && col >= 0 && col < Width ? _cells[row][col] : null;
        set
        {
            if (row >= 0 && row < Height && col >= 0 && col < Width)
                _cells[row][col] = value;
        }
    }

    /// <summary>
    /// The region the snapshot covers when its top-left is at 0,0.
    /// </summary>
    public Region Region => new(0, Height - 1, 0, Width - 1);

    public bool IsEmpty
    {
        get
        {
            if (Height == 0 || Width == 0)
                return true;
            for (var r = 0; r < Height; r++)
            for (var c = 0; c < Width; c++)
                if (_cells[r][c]?.IsEmpty == false)
                    return false;
            return Merges.Count == 0;
        }
    }
}
