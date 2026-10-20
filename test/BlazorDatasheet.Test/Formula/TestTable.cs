using System;
using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Test.Formula;

/// <summary>
/// An in-memory table for <see cref="TestEnvironment"/> and the reference implementation of structured
/// reference resolution. A region table's first row is its header row and, when <see cref="ShowTotals"/>,
/// its last row is the totals row; the rows between are data. A sheet table has no header or totals rows:
/// its data is every row of the sheet and its column names are the sheet's column headings.
/// </summary>
public class TestTable
{
    public string Name { get; }
    public string SheetName { get; }
    public bool IsSheetTable { get; }
    public bool ShowTotals { get; }
    public IReadOnlyList<string> ColumnNames { get; }

    /// <summary>
    /// The whole table. For a sheet table this is the same as <see cref="DataRegion"/>.
    /// </summary>
    public IRegion Region { get; }

    /// <summary>
    /// The header row, or null for a sheet table (which has no header cells).
    /// </summary>
    public IRegion? HeaderRegion { get; }

    /// <summary>
    /// The data rows, or null when a region table has no data rows.
    /// </summary>
    public IRegion? DataRegion { get; }

    /// <summary>
    /// The totals row, or null if there isn't one.
    /// </summary>
    public IRegion? TotalsRegion { get; }

    private TestTable(string name, string sheetName, bool isSheetTable, bool showTotals,
        IReadOnlyList<string> columnNames, IRegion region, IRegion? headerRegion, IRegion? dataRegion,
        IRegion? totalsRegion)
    {
        Name = name;
        SheetName = sheetName;
        IsSheetTable = isSheetTable;
        ShowTotals = showTotals;
        ColumnNames = columnNames;
        Region = region;
        HeaderRegion = headerRegion;
        DataRegion = dataRegion;
        TotalsRegion = totalsRegion;
    }

    public static TestTable CreateRegionTable(string name, string sheetName, IRegion region, bool showTotals,
        IEnumerable<string?> headerValues)
    {
        var header = new Region(region.Top, region.Top, region.Left, region.Right);
        var totals = showTotals ? new Region(region.Bottom, region.Bottom, region.Left, region.Right) : null;
        var dataTop = region.Top + 1;
        var dataBottom = showTotals ? region.Bottom - 1 : region.Bottom;
        var data = dataBottom >= dataTop ? new Region(dataTop, dataBottom, region.Left, region.Right) : null;
        return new TestTable(name, sheetName, false, showTotals, BuildColumnNames(headerValues), region, header,
            data, totals);
    }

    /// <summary>
    /// Creates a sheet table. With <paramref name="numRows"/> the data is the first <paramref name="numRows"/>
    /// rows, otherwise it is whole columns (like a sheet table in a workbook).
    /// </summary>
    public static TestTable CreateSheetTable(string name, string sheetName, IEnumerable<string?> headings,
        int? numRows)
    {
        var columnNames = BuildColumnNames(headings);
        IRegion data = numRows == null
            ? new ColumnRegion(0, columnNames.Count - 1)
            : new Region(0, numRows.Value - 1, 0, columnNames.Count - 1);
        return new TestTable(name, sheetName, true, false, columnNames, data, null, data, null);
    }

    /// <summary>
    /// A copy of the table over <paramref name="region"/>, e.g. after the table has been shifted. A region table
    /// keeps its header row at the top (and totals row at the bottom).
    /// </summary>
    public TestTable WithRegion(IRegion region) => With(Name, ColumnNames, region);

    public TestTable WithName(string name) => With(name, ColumnNames, Region);

    public TestTable WithColumnNames(IEnumerable<string?> columnNames) =>
        With(Name, BuildColumnNames(columnNames), Region);

    private TestTable With(string name, IReadOnlyList<string> columnNames, IRegion region)
    {
        if (IsSheetTable)
            return new TestTable(name, SheetName, true, false, columnNames, region, null, region, null);

        var table = CreateRegionTable(name, SheetName, region, ShowTotals, columnNames);
        return new TestTable(name, SheetName, false, ShowTotals, columnNames, table.Region, table.HeaderRegion,
            table.DataRegion, table.TotalsRegion);
    }

    /// <summary>
    /// Empty names become Column{n}; a duplicate name gets the smallest numeric suffix (from 2) that is free.
    /// </summary>
    public static IReadOnlyList<string> BuildColumnNames(IEnumerable<string?> rawNames)
    {
        var names = new List<string>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in rawNames)
        {
            var name = string.IsNullOrWhiteSpace(raw) ? $"Column{names.Count + 1}" : raw;
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
    /// Whether the cell is part of the table, i.e. whether an unqualified reference written there refers
    /// to this table.
    /// </summary>
    public bool Contains(int row, int col, string sheetName)
    {
        return sheetName == SheetName && Region.Contains(row, col);
    }

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
    /// Resolves <paramref name="descriptor"/> against this table. The table name in the descriptor is not
    /// checked; the caller has already found the table.
    /// </summary>
    public StructuredReferenceResolution Resolve(StructuredReferenceDescriptor descriptor, int? callerRow,
        string? callerSheet)
    {
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
        var has = (TableItemSpecifier item) => items.Contains(item);

        if (has(TableItemSpecifier.ThisRow))
        {
            if (callerRow == null || callerSheet != SheetName || DataRegion == null ||
                callerRow < DataRegion.Top || callerRow > DataRegion.Bottom)
                return StructuredReferenceResolution.FromError(ErrorType.Value, Name);
            return Rows(callerRow.Value, callerRow.Value, colStart, colEnd);
        }

        if (IsSheetTable && has(TableItemSpecifier.Headers) && !has(TableItemSpecifier.Data))
        {
            // a sheet table's headers are the column headings, which aren't in any cell
            var headings = new CellValue[1][];
            headings[0] = ColumnNames.Skip(colStart).Take(colEnd - colStart + 1).Select(CellValue.Text).ToArray();
            return StructuredReferenceResolution.FromValues(headings, SheetName, Name);
        }

        IRegion? top, bottom;
        if (has(TableItemSpecifier.All))
        {
            top = IsSheetTable ? DataRegion : HeaderRegion;
            bottom = TotalsRegion ?? DataRegion ?? HeaderRegion;
        }
        else if (has(TableItemSpecifier.Headers))
        {
            // #Headers alone or [#Headers],[#Data]; a sheet table has no header cells so only its data is left
            top = HeaderRegion ?? DataRegion;
            bottom = has(TableItemSpecifier.Data) ? DataRegion ?? HeaderRegion : HeaderRegion;
        }
        else if (has(TableItemSpecifier.Totals))
        {
            // #Totals alone requires a totals row; [#Data],[#Totals] is just the data without one
            if (!has(TableItemSpecifier.Data) && TotalsRegion == null)
                return StructuredReferenceResolution.FromError(ErrorType.Ref, Name);
            top = has(TableItemSpecifier.Data) ? DataRegion ?? TotalsRegion : TotalsRegion;
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
        var left = Region.Left + colStart;
        var right = Region.Left + colEnd;
        // whole columns stay whole columns so they keep up with the sheet's rows
        IRegion region = Region is ColumnRegion && rowStart == Region.Top && rowEnd == Region.Bottom
            ? new ColumnRegion(left, right)
            : new Region(rowStart, rowEnd, left, right);
        return StructuredReferenceResolution.FromRegion(region, SheetName, Name);
    }
}
