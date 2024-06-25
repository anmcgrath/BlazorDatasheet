using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Core.FormulaEngine;

public class SheetEnvironment : IEnvironment
{
    // only one sheet for now...
    private readonly Sheet _sheet;
    private readonly Dictionary<string, CellValue> _variables = new();
    private readonly Dictionary<string, ISheetFunction> _functions = new();

    public SheetEnvironment(Sheet sheet)
    {
        _sheet = sheet;
    }

    public bool VariableExists(string name)
    {
        return _variables.ContainsKey(name);
    }

    public CellValue GetVariable(string name)
    {
        return _variables[name];
    }

    public void SetVariable(string name, object value)
    {
        SetVariable(name, new CellValue(value));
    }

    public void SetVariable(string name, CellValue cellValue)
    {
        if (!_variables.TryAdd(name, cellValue))
            _variables[name] = cellValue;
    }

    public bool FunctionExists(string name)
    {
        return _functions.ContainsKey(name.ToLower());
    }

    public ISheetFunction GetFunctionDefinition(string name)
    {
        return _functions[name.ToLower()];
    }

    public void RegisterFunction(string name, ISheetFunction value)
    {
        if (!_functions.ContainsKey(name.ToLower()))
            _functions.Add(name.ToLower(), value);
        else
            _functions[name.ToLower()] = value;
    }

    public IEnumerable<CellValue> GetNonEmptyInRange(Reference reference)
    {
        return _sheet.Cells.GetNonEmptyCellValues(reference.Region)
            .Select(x => x.value).ToArray();
    }

    public CellValue GetCellValue(int row, int col) => _sheet.Cells.GetCellValue(row, col);

    public CellValue[][] GetRangeValues(Reference reference)
    {
        if (reference.Kind == ReferenceKind.Range)
        {
            var r = reference.Region;
            return GetValuesInRange(_sheet.Range(r.Top, r.Bottom, r.Left, r.Right));
        }

        if (reference.Kind == ReferenceKind.Cell)
        {
            var cellRef = (CellReference)reference;
            return new[] { new[] { GetCellValue(cellRef.RowIndex, cellRef.ColIndex) } };
        }

        return Array.Empty<CellValue[]>();
    }

    private CellValue[][] GetValuesInRange(SheetRange range)
    {
        var region = range.Region.GetIntersection(range.Sheet.Region);
        if (region == null)
            return Array.Empty<CellValue[]>();
        return range.Sheet.Cells.GetCellDataStore().GetData(region);
    }
}