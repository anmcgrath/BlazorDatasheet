using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Core.Data;

/// <summary>
/// A snapshot of a table: a region whose columns can be referred to by name in formulas, e.g. Sales[Amount].
/// <para>
/// A region table's first row is its header row, which holds the column names, and when <see cref="ShowTotals"/> its
/// last row is the totals row. The rows between are data. A sheet table has no header or totals rows: its data is
/// every row of the sheet and its column names are the sheet's column headings.
/// </para>
/// </summary>
public sealed record SheetTable
{
    /// <summary>
    /// The table's name, unique (ignoring case) within the workbook.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The name of the sheet the table is on, as it is now. The workbook may have no sheet by this name, in which case
    /// the table is detached until it does.
    /// </summary>
    public string SheetName { get; }

    /// <summary>
    /// The whole table including its header and totals rows, or the region it last occupied if it has been deleted.
    /// Null for a sheet table.
    /// </summary>
    public IRegion? Region { get; }

    /// <summary>
    /// Whether the table is a sheet table, whose data is every row of the sheet and whose column names are the
    /// sheet's column headings.
    /// </summary>
    public bool IsSheetTable { get; }

    /// <summary>
    /// Whether the last row of the table is a totals row. Always false for a sheet table.
    /// </summary>
    public bool ShowTotals { get; }

    /// <summary>
    /// True if every row or column of the table has been removed. A deleted table cannot be referred to.
    /// </summary>
    public bool IsDeleted { get; }

    /// <summary>
    /// The names of the table's columns, from left to right. See <see cref="BuildColumnNames"/>.
    /// </summary>
    public IReadOnlyList<string> ColumnNames { get; }

    /// <summary>
    /// The header row, or null for a sheet table (which has no header cells).
    /// </summary>
    public IRegion? HeaderRegion { get; }

    /// <summary>
    /// The data rows, or null when there are none. For a sheet table this is every row of the sheet when the
    /// snapshot was taken.
    /// </summary>
    public IRegion? DataRegion { get; }

    /// <summary>
    /// The totals row, or null if there isn't one.
    /// </summary>
    public IRegion? TotalsRegion { get; }

    // What the table covers for resolving references: the region of a region table, the data of a sheet table.
    private readonly IRegion? _extent;

    private SheetTable(string name, string sheetName, IRegion? region, bool isSheetTable, bool showTotals,
        bool isDeleted, IReadOnlyList<string> columnNames, IRegion? headerRegion, IRegion? dataRegion,
        IRegion? totalsRegion, IRegion? extent)
    {
        Name = name;
        SheetName = sheetName;
        Region = region;
        IsSheetTable = isSheetTable;
        ShowTotals = showTotals;
        IsDeleted = isDeleted;
        ColumnNames = columnNames;
        HeaderRegion = headerRegion;
        DataRegion = dataRegion;
        TotalsRegion = totalsRegion;
        _extent = extent;
    }

    internal static SheetTable ForRegion(string name, string sheetName, IRegion region, bool showTotals,
        bool isDeleted, IReadOnlyList<string> columnNames)
    {
        var header = new Region(region.Top, region.Top, region.Left, region.Right);
        var totals = showTotals ? new Region(region.Bottom, region.Bottom, region.Left, region.Right) : null;
        var dataTop = region.Top + 1;
        var dataBottom = showTotals ? region.Bottom - 1 : region.Bottom;
        var data = dataBottom >= dataTop ? new Region(dataTop, dataBottom, region.Left, region.Right) : null;
        return new SheetTable(name, sheetName, region.Clone(), false, showTotals, isDeleted, columnNames, header,
            data, totals, region.Clone());
    }

    internal static SheetTable ForSheet(string name, string sheetName, int numRows, IReadOnlyList<string> columnNames)
    {
        IRegion? data = numRows > 0 && columnNames.Count > 0
            ? new Region(0, numRows - 1, 0, columnNames.Count - 1)
            : null;
        return new SheetTable(name, sheetName, null, true, false, false, columnNames, null, data, null, data);
    }

    /// <summary>
    /// Makes column names from raw header values: an empty or whitespace value becomes Column{n}, where n is its
    /// (one-based) position, and a name already used (ignoring case and surrounding whitespace) is trimmed and gets
    /// the smallest numeric suffix, from 2, that is free. Other names are kept as they are, whitespace included.
    /// </summary>
    public static List<string> BuildColumnNames(IEnumerable<string?> raw)
    {
        var names = new List<string>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in raw)
        {
            var name = string.IsNullOrWhiteSpace(value) ? $"Column{names.Count + 1}" : value;
            // names that differ only by surrounding whitespace refer to the same column, so are duplicates
            var trimmed = name.Trim();
            if (!used.Add(trimmed))
            {
                var suffix = 2;
                while (!used.Add(trimmed + suffix))
                    suffix++;
                name = trimmed + suffix;
            }

            names.Add(name);
        }

        return names;
    }

    /// <summary>
    /// The index of the column named <paramref name="columnName"/> (ignoring case and surrounding whitespace, as Excel
    /// does), or -1 if there is none.
    /// </summary>
    public int IndexOfColumn(string columnName)
    {
        for (var i = 0; i < ColumnNames.Count; i++)
        {
            if (ColumnNames[i].AsSpan().Trim().Equals(columnName.AsSpan().Trim(), StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Resolves <paramref name="descriptor"/> against this table. The table name in the descriptor is not checked.
    /// </summary>
    /// <param name="descriptor">The structured reference.</param>
    /// <param name="callerRow">The row of the cell containing the formula, if any.</param>
    /// <param name="callerSheet">The sheet of the cell containing the formula, if any.</param>
    public StructuredReferenceResolution Resolve(StructuredReferenceDescriptor descriptor, int? callerRow,
        string? callerSheet)
    {
        if (IsDeleted || _extent == null)
            return StructuredReferenceResolution.FromError(ErrorType.Ref, Name);

        // columns, relative to the table
        var colStart = 0;
        var colEnd = ColumnNames.Count - 1;
        if (descriptor.HasColumns)
        {
            colStart = IndexOfColumn(descriptor.ColumnStart!);
            colEnd = descriptor.ColumnEnd == null ? colStart : IndexOfColumn(descriptor.ColumnEnd);
            if (colStart < 0 || colEnd < 0)
                return StructuredReferenceResolution.FromError(ErrorType.Ref, Name);
            if (colStart > colEnd)
                (colStart, colEnd) = (colEnd, colStart);
        }

        var items = descriptor.Items;
        bool Has(TableItemSpecifier item) => items.Contains(item);

        if (Has(TableItemSpecifier.ThisRow))
        {
            if (callerRow == null || callerSheet != SheetName || DataRegion == null ||
                callerRow < DataRegion.Top || callerRow > DataRegion.Bottom)
                return StructuredReferenceResolution.FromError(ErrorType.Value, Name);
            return Rows(callerRow.Value, callerRow.Value, colStart, colEnd);
        }

        if (IsSheetTable && Has(TableItemSpecifier.Headers) && !Has(TableItemSpecifier.Data))
        {
            // a sheet table's headers are the column headings, which aren't in any cell
            var headings = new CellValue[1][];
            headings[0] = ColumnNames.Skip(colStart).Take(colEnd - colStart + 1).Select(CellValue.Text).ToArray();
            return StructuredReferenceResolution.FromValues(headings, SheetName, Name);
        }

        IRegion? top, bottom;
        if (Has(TableItemSpecifier.All))
        {
            top = IsSheetTable ? DataRegion : HeaderRegion;
            bottom = TotalsRegion ?? DataRegion ?? HeaderRegion;
        }
        else if (Has(TableItemSpecifier.Headers))
        {
            // #Headers alone or [#Headers],[#Data]; a sheet table has no header cells so only its data is left
            top = HeaderRegion ?? DataRegion;
            bottom = Has(TableItemSpecifier.Data) ? DataRegion ?? HeaderRegion : HeaderRegion;
        }
        else if (Has(TableItemSpecifier.Totals))
        {
            // #Totals alone requires a totals row; [#Data],[#Totals] is just the data without one
            if (!Has(TableItemSpecifier.Data) && TotalsRegion == null)
                return StructuredReferenceResolution.FromError(ErrorType.Ref, Name);
            top = Has(TableItemSpecifier.Data) ? DataRegion ?? TotalsRegion : TotalsRegion;
            bottom = TotalsRegion ?? DataRegion;
        }
        else
        {
            // #Data, or no specifier
            top = DataRegion;
            bottom = DataRegion;
        }

        if (top == null || bottom == null)
            return StructuredReferenceResolution.FromError(ErrorType.Ref, Name);

        return Rows(top.Top, bottom.Bottom, colStart, colEnd);
    }

    private StructuredReferenceResolution Rows(int rowStart, int rowEnd, int colStart, int colEnd)
    {
        // A fixed region, even for a sheet table: references are resolved again when rows or columns are inserted
        // or removed, so they keep up with the sheet.
        var left = _extent!.Left + colStart;
        var right = _extent.Left + colEnd;
        return StructuredReferenceResolution.FromRegion(new Region(rowStart, rowEnd, left, right), SheetName, Name);
    }
}
