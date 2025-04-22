using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.Core.FormulaEngine;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Core.Data;

public class Workbook
{
    private readonly List<Sheet> _sheets = new();
    public IEnumerable<Sheet> Sheets => _sheets;
    private readonly FormulaEngine.FormulaEngine _formulaEngine;
    internal WorkbookEnvironment Environment { get; }

    public event EventHandler<WorkbookSheetAddedEventArgs>? SheetAdded;
    public event EventHandler<WorkbookSheetRemovedEventArgs>? SheetRemoved;
    public event EventHandler<WorkbookSheetRenamedEventArgs>? SheetRenamed;


    internal Workbook(Sheet sheet, FormulaOptions? options = null) : this(options)
    {
        AddSheet(sheet);
    }

    internal Workbook(IEnumerable<Sheet> sheets, FormulaOptions? options = null) : this(options)
    {
        foreach (var sheet in sheets)
            AddSheet(sheet);
    }

    public Workbook(FormulaOptions? options = null)
    {
        Environment = new WorkbookEnvironment(this);
        _formulaEngine = new FormulaEngine.FormulaEngine(Environment, options);
    }

    public Sheet AddSheet(int numRows, int numColumns, int defaultWidth = 105, int defaultHeight = 24)
    {
        var sheetName = GenerateNewSheetName();
        return AddSheet(sheetName, numRows, numColumns, defaultWidth, defaultHeight);
    }

    private string GenerateNewSheetName()
    {
        int index = 1;
        var name = $"Sheet{index}";
        while (GetSheet(name) is not null)
        {
            index++;
            name = $"Sheet{index}";
        }

        return name;
    }

    public Sheet AddSheet(string sheetName, int numRows, int numColumns, int defaultWidth = 105, int defaultHeight = 24)
    {
        var sheet = new Sheet(numRows, numColumns, defaultWidth, defaultHeight, this);
        if (GetSheet(sheetName) is not null)
            throw new Exception($"Sheet {sheetName} already exists");
        sheet.Name = sheetName;
        AddSheet(sheet);
        return sheet;
    }

    private void AddSheet(Sheet sheet)
    {
        sheet.Workbook = this;
        _sheets.Add(sheet);
        SheetAdded?.Invoke(this, new WorkbookSheetAddedEventArgs(sheet));
        _formulaEngine.AddSheet(sheet);
    }

    internal void AddSheet(string sheetName, Sheet sheet)
    {
        sheet.Name = sheetName;
        AddSheet(sheet);
    }


    public void RemoveSheet(string sheetName)
    {
        var sheetIndex = _sheets.FindIndex(s => s.Name == sheetName);
        if (sheetIndex >= 0)
        {
            var sheet = _sheets[sheetIndex];
            _formulaEngine.RemoveSheet(_sheets[sheetIndex]);
            _sheets.RemoveAt(sheetIndex);
            SheetRemoved?.Invoke(this, new WorkbookSheetRemovedEventArgs(sheet));
        }
    }

    public void RenameSheet(string oldName, string newName)
    {
        var sheet = _sheets.FirstOrDefault(s => s.Name == oldName);
        var nameExists = _sheets.Any(x => x.Name == newName);

        if (nameExists)
            throw new Exception("Sheet name already exists");

        if (sheet != null)
        {
            sheet.Name = newName;
            _formulaEngine.RenameSheet(oldName, newName);
            SheetRenamed?.Invoke(this, new WorkbookSheetRenamedEventArgs(sheet, oldName, newName));
        }
    }

    public Sheet? GetSheet(string name)
    {
        return Sheets.FirstOrDefault(x => x.Name == name);
    }

    public FormulaEngine.FormulaEngine GetFormulaEngine() => _formulaEngine;
}