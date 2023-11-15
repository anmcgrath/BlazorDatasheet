using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Cells;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;

namespace BlazorDatasheet.Core.FormulaEngine;

public class SheetEnvironment : IEnvironment
{
    // only one sheet for now...
    private readonly Sheet _sheet;
    private readonly Dictionary<string, object> _variables = new();
    private readonly Dictionary<string, ISheetFunction> _functions = new();

    public SheetEnvironment(Sheet sheet)
    {
        _sheet = sheet;
    }

    public bool VariableExists(string name)
    {
        return _variables.ContainsKey(name);
    }

    public object GetVariable(string name)
    {
        return _variables[name];
    }

    public void SetVariable(string name, object value)
    {
        if (!_variables.TryAdd(name, value))
            _variables[name] = value;
    }

    public bool FunctionExists(string name)
    {
        return _functions.ContainsKey(name.ToLower());
    }

    public ISheetFunction GetFunctionDefinition(string name)
    {
        return _functions[name.ToLower()];
    }

    public void SetFunction(string name, ISheetFunction value)
    {
        if (!_functions.ContainsKey(name.ToLower()))
            _functions.Add(name.ToLower(), value);
        else
            _functions[name.ToLower()] = value;
    }

    public CellValue GetCellValue(int row, int col) => _sheet.Cells.GetCellValue(row, col);

    public CellValue[][] GetRangeValues(RangeAddress rangeAddress)
    {
        return GetValuesInRange(
            _sheet.Range(rangeAddress.RowStart, rangeAddress.RowEnd, rangeAddress.ColStart, rangeAddress.ColEnd));
    }

    public CellValue[][] GetRangeValues(ColumnAddress rangeAddress)
    {
        return GetValuesInRange(_sheet.Range(Axis.Col, rangeAddress.Start, rangeAddress.End));
    }

    public CellValue[][] GetRangeValues(RowAddress rangeAddress)
    {
        return GetValuesInRange(_sheet.Range(Axis.Row, rangeAddress.Start, rangeAddress.End));
    }

    private CellValue[][] GetValuesInRange(SheetRange range)
    {
        return range.Sheet.Cells.GetStore().GetData(range.Region);
    }
}