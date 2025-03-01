using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Core.FormulaEngine;

public class WorkbookEnvironment : IEnvironment
{
    private readonly Workbook _workbook;
    private readonly Dictionary<string, CellValue> _variables = new();
    private readonly Dictionary<string, ISheetFunction> _functions = new();

    public WorkbookEnvironment(Workbook workbook)
    {
        _workbook = workbook;
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

    public ISheetFunction? GetFunctionDefinition(string name)
    {
        return _functions.GetValueOrDefault(name.ToLower());
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
        var sheet = _workbook.GetSheet(reference.SheetName);
        if (sheet == null)
            return [CellValue.Error(ErrorType.Ref)];

        return sheet.Cells.GetNonEmptyCellValues(reference.Region)
            .Select(x => x.value).ToArray();
    }

    public void SetCellValue(int row, int col, string sheetName, CellValue value)
    {
        var sheet = _workbook.GetSheet(sheetName);
        sheet?.Cells.SetValueImpl(row, col, value);
        sheet?.MarkDirty(row, col);
    }

    public CellValue GetCellValue(int row, int col, string sheetName)
    {
        var sheet = _workbook.GetSheet(sheetName);
        if (sheet == null)
            return CellValue.Error(ErrorType.Ref);
        return sheet?.Cells.GetCellValue(row, col) ?? CellValue.Empty;
    }

    public CellFormula? GetFormula(int row, int col, string sheetName)
    {
        var sheet = _workbook.GetSheet(sheetName);
        return sheet?.Cells.GetFormula(row, col);
    }

    public CellValue[][] GetRangeValues(Reference reference)
    {
        var sheet = _workbook.GetSheet(reference.SheetName);
        if (sheet == null)
            return [];

        if (reference.Kind == ReferenceKind.Range)
        {
            var r = reference.Region;
            return GetValuesInRange(sheet.Range(r.Top, r.Bottom, r.Left, r.Right));
        }

        if (reference.Kind == ReferenceKind.Cell)
        {
            var cellRef = (CellReference)reference;
            return new[] { new[] { GetCellValue(cellRef.RowIndex, cellRef.ColIndex, sheet.Name) } };
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