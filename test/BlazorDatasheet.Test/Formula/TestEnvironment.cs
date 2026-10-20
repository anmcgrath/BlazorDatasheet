using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Test.Formula;

public class TestEnvironment : IEnvironment
{
    private Dictionary<CellPosition, CellValue> _cellValues = new();
    private Dictionary<string, FunctionDescriptor> _functions = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, CellValue> _variables = new();
    private Dictionary<CellPosition, CellFormula> _formulas = new();
    private Dictionary<CellPosition, IRegion> _spillRegions = new();
    private readonly Dictionary<string, TestTable> _tables = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds a table over <paramref name="region"/>, taking the column names from the cell values in its
    /// first row. Set the header values before adding the table.
    /// </summary>
    public TestTable AddTable(string name, string sheetName, IRegion region, bool showTotals = false)
    {
        var headerValues = Enumerable.Range(region.Left, region.Width)
            .Select(col => GetCellValue(region.Top, col, sheetName).Data?.ToString());
        var table = TestTable.CreateRegionTable(name, sheetName, region, showTotals, headerValues);
        _tables.Add(name, table);
        return table;
    }

    /// <summary>
    /// Adds a sheet table whose column names are <paramref name="headings"/>. Its data is the first
    /// <paramref name="numRows"/> rows, or whole columns when null.
    /// </summary>
    public TestTable AddSheetTable(string name, string sheetName, IEnumerable<string> headings, int? numRows = null)
    {
        var table = TestTable.CreateSheetTable(name, sheetName, headings, numRows);
        _tables.Add(name, table);
        return table;
    }

    public TestTable? GetTable(string name) => _tables.GetValueOrDefault(name);

    public void RenameTable(string oldName, string newName)
    {
        var table = _tables[oldName];
        _tables.Remove(oldName);
        _tables.Add(newName, table.WithName(newName));
    }

    public void RenameTableColumn(string tableName, string oldColumn, string newColumn)
    {
        var table = _tables[tableName];
        _tables[tableName] = table.WithColumnNames(table.ColumnNames.Select(c =>
            string.Equals(c, oldColumn, StringComparison.OrdinalIgnoreCase) ? newColumn : c));
    }

    /// <summary>
    /// Shifts the tables on <paramref name="sheetName"/> for rows/columns inserted at <paramref name="index"/>,
    /// like a sheet's table store: a table after the index moves, a table the index falls inside grows, and a
    /// table ending just before the index is unchanged. Call before the dependency manager's insert.
    /// </summary>
    public void InsertRowColIntoTables(int index, int count, Axis axis, string sheetName)
    {
        foreach (var table in _tables.Values.Where(t => t.SheetName == sheetName).ToList())
        {
            var start = table.Region.GetLeadingEdgeOffset(axis);
            var end = table.Region.GetTrailingEdgeOffset(axis);
            if (index > end || (axis == Axis.Row && table.Region is ColumnRegion))
                continue;

            var region = table.Region.Clone();
            if (index <= start)
                region.Shift(axis == Axis.Row ? count : 0, axis == Axis.Col ? count : 0);
            else
                region.Expand(axis == Axis.Row ? Edge.Bottom : Edge.Right, count);
            _tables[table.Name] = table.WithRegion(region);
        }
    }

    /// <summary>
    /// Shifts the tables on <paramref name="sheetName"/> for rows/columns removed at <paramref name="index"/>.
    /// A table that is removed entirely is deleted. Call before the dependency manager's remove.
    /// </summary>
    public void RemoveRowColFromTables(int index, int count, Axis axis, string sheetName)
    {
        var last = index + count - 1;
        foreach (var table in _tables.Values.Where(t => t.SheetName == sheetName).ToList())
        {
            var start = table.Region.GetLeadingEdgeOffset(axis);
            var end = table.Region.GetTrailingEdgeOffset(axis);
            if (index > end || (axis == Axis.Row && table.Region is ColumnRegion))
                continue;

            if (index <= start && last >= end)
            {
                _tables.Remove(table.Name);
                continue;
            }

            var removedInside = Math.Min(last, end) - Math.Max(index, start) + 1;
            var newStart = start > last ? start - count : Math.Min(start, index);
            var newEnd = start > last ? end - count : end - removedInside;
            var r = table.Region;
            IRegion region = axis == Axis.Row
                ? new Region(newStart, newEnd, r.Left, r.Right)
                : new Region(r.Top, r.Bottom, newStart, newEnd);
            _tables[table.Name] = table.WithRegion(region);
        }
    }

    /// <summary>
    /// Replaces a table, e.g. to put back a table as it was before a shift when restoring.
    /// </summary>
    public void SetTable(TestTable table)
    {
        _tables[table.Name] = table;
    }

    public StructuredReferenceResolution Resolve(StructuredReferenceDescriptor descriptor, int? callerRow,
        int? callerCol, string? callerSheet)
    {
        // without a calling cell there is no row for @ to refer to
        if (descriptor.IsThisRow && (callerRow == null || callerCol == null || callerSheet == null))
            return StructuredReferenceResolution.FromError(ErrorType.Value);

        TestTable? table;
        if (descriptor.TableName != null)
            _tables.TryGetValue(descriptor.TableName, out table);
        else
            table = GetTableAt(callerRow, callerCol, callerSheet);

        if (table == null)
            return StructuredReferenceResolution.FromError(ErrorType.Ref);

        return table.Resolve(descriptor, callerRow, callerSheet);
    }

    /// <summary>
    /// The table that an unqualified reference at the calling cell refers to: the region table containing it,
    /// otherwise the sheet table of its sheet.
    /// </summary>
    private TestTable? GetTableAt(int? row, int? col, string? sheetName)
    {
        if (row == null || col == null || sheetName == null)
            return null;

        return _tables.Values.FirstOrDefault(t => !t.IsSheetTable && t.Contains(row.Value, col.Value, sheetName))
               ?? _tables.Values.FirstOrDefault(t => t.IsSheetTable && t.SheetName == sheetName);
    }

    public IEnumerable<string> GetTableNames()
    {
        return _tables.Values.Select(t => t.Name);
    }

    public void SetSpillRegion(int row, int col, IRegion? region)
    {
        if (region == null)
            _spillRegions.Remove(new CellPosition(row, col));
        else
            _spillRegions[new CellPosition(row, col)] = region;
    }

    public IRegion? GetSpillRegion(int row, int col, string sheetName)
    {
        return _spillRegions.GetValueOrDefault(new CellPosition(row, col));
    }

    public void SetCellValue(int row, int col, object val)
    {
        SetCellValue(new CellPosition(row, col), val);
    }

    public void SetCellValue(CellPosition position, object val)
    {
        _cellValues.TryAdd(position, new CellValue(val));
        _cellValues[position] = new CellValue(val);
    }

    public void SetCellFormula(int row, int col, CellFormula formula)
    {
        var position = new CellPosition(row, col);
        if (_formulas.TryAdd(position, formula))
            _formulas[position] = formula;
    }

    public void RegisterFunction(FunctionDescriptor functionDefinition)
    {
        if (!_functions.TryAdd(functionDefinition.Name, functionDefinition))
            throw new InvalidOperationException($"Function '{functionDefinition.Name}' has already been registered.");
    }

    public IEnumerable<FunctionDefinition> SearchForFunctions(string functionName)
    {
        return _functions.Where(x => x.Key.StartsWith(functionName, StringComparison.OrdinalIgnoreCase))
            .Select(x => new FunctionDefinition(x.Key, x.Value));
    }

    public IEnumerable<CellValue> GetNonEmptyInRange(Reference reference)
    {
        return GetRangeValues(reference)
            .SelectMany(x => x);
    }

    public void SetCellValue(int row, int col, string sheetName, CellValue value)
    {
        _cellValues.TryAdd(new CellPosition(row, col), value);
    }

    public SpillResult SetSpillResult(int row, int col, string sheetName, CellValue[][] values)
    {
        var old = GetSpillRegion(row, col, sheetName);
        var region = new Region(row, row + values.Length - 1, col, col + values[0].Length - 1);
        for (var i = 0; i < values.Length; i++)
        for (var j = 0; j < values[i].Length; j++)
            _cellValues[new CellPosition(row + i, col + j)] = values[i][j];
        SetSpillRegion(row, col, region);
        return new SpillResult(old, region, false);
    }

    public void ClearVariable(string varName)
    {
        _variables.Remove(varName);
    }

    public IEnumerable<string> GetVariableNames()
    {
        return _variables.Keys;
    }

    public void SetVariable(string name, object variable)
    {
        SetVariable(name, new CellValue(variable));
    }

    public void SetVariable(string name, CellValue value)
    {
        if (!_variables.ContainsKey(name))
            _variables.Add(name, value);
        _variables[name] = value;
    }

    public CellValue GetCellValue(int row, int col, string sheetName)
    {
        var hasVal = _cellValues.TryGetValue(new CellPosition(row, col), out var val);
        if (hasVal)
            return val;
        return CellValue.Empty;
    }

    public CellFormula? GetFormula(int row, int col, string sheetName)
    {
        var hasVal = _formulas.TryGetValue(new CellPosition(row, col), out var val);
        if (hasVal)
            return val;
        return null;
    }

    public CellValue[][] GetRangeValues(Reference reference)
    {
        if (reference.Kind == ReferenceKind.Range)
        {
            var rangeRef = (RangeReference)reference;
            var r = reference.Region;
            return GetValuesInRange(r.Top, r.Bottom, r.Left, r.Right);
        }

        if (reference.Kind == ReferenceKind.Cell)
        {
            var cellRef = (CellReference)reference;
            return new[] { new[] { GetCellValue(cellRef.RowIndex, cellRef.ColIndex, cellRef.SheetName) } };
        }

        return Array.Empty<CellValue[]>();
    }

    private CellValue[][] GetValuesInRange(int r0, int r1, int c0, int c1)
    {
        r0 = Math.Clamp(r0, 0, RangeText.MaxRows);
        r1 = Math.Clamp(r1, 0, RangeText.MaxRows);

        c0 = Math.Clamp(c0, 0, RangeText.MaxCols);
        c1 = Math.Clamp(c1, 0, RangeText.MaxCols);

        var h = (r1 - r0) + 1;
        var w = (c1 - c0) + 1;
        var arr = new CellValue[h][];

        for (int i = 0; i < h; i++)
        {
            arr[i] = new CellValue[w];
            for (int j = 0; j < w; j++)
            {
                arr[i][j] = GetCellValue(r0 + i, c0 + j, "Sheet1");
            }
        }

        return arr;
    }

    public bool TryGetFunction(string identifierText, [MaybeNullWhen(false)] out FunctionDescriptor functionDescriptor)
    {
        return _functions.TryGetValue(identifierText, out functionDescriptor);
    }

    public bool VariableExists(string variableIdentifier)
    {
        return _variables.ContainsKey(variableIdentifier);
    }

    public CellValue GetVariable(string variableIdentifier)
    {
        return _variables[variableIdentifier];
    }

    public bool TryGetVariable(string variableIdentifier, out CellValue value)
    {
        return _variables.TryGetValue(variableIdentifier, out value);
    }
}
