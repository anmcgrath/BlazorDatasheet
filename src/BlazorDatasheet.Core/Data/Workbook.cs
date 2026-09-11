using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.FormulaEngine;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Serialization.Json.Mappers;
using BlazorDatasheet.Core.Serialization.Models;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatashet.Formula.Functions;

namespace BlazorDatasheet.Core.Data;

public class Workbook
{
    private readonly List<Sheet> _sheets = new();
    public IEnumerable<Sheet> Sheets => _sheets;
    private readonly FormulaEngine.FormulaEngine _formulaEngine;
    internal WorkbookEnvironment Environment { get; }

    /// <summary>
    /// Named ranges defined in the workbook. Named ranges are workbook-wide.
    /// </summary>
    public NamedRangeManager NamedRanges { get; }

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
        var registry = BuildDefaultRegistry(options);
        Environment = new WorkbookEnvironment(this, registry);
        _formulaEngine = new FormulaEngine.FormulaEngine(Environment, options);
        NamedRanges = new NamedRangeManager(this);
    }

    public static FunctionRegistry BuildDefaultRegistry(FormulaOptions? options)
    {
        var builder = new FunctionRegistryBuilder();
        builder.RegisterLogicalFunctions();
        builder.RegisterMathFunctions();
        builder.RegisterLookupFunctions();
        options?.ConfigureFunctions?.Invoke(builder);
        return builder.Build();
    }

    public Sheet AddSheet(int numRows, int numColumns, int defaultWidth = 105, int defaultHeight = 24)
    {
        var sheetName = GenerateNewSheetName();
        return AddSheet(sheetName, numRows, numColumns, defaultWidth, defaultHeight);
    }

    /// <summary>
    /// Creates a new, un-used sheet name of the form SheetN 
    /// </summary>
    /// <returns></returns>
    public string GenerateNewSheetName()
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


    /// <summary>
    /// Adds a copy of a sheet - its cells, formulas, formats, conditional formats, validation,
    /// metadata, merges and sizing - under a new name.
    /// </summary>
    /// <param name="sheetName">The sheet to copy.</param>
    /// <param name="newSheetName">The copy's name, or null for the next un-used SheetN.</param>
    /// <returns>The new sheet.</returns>
    public Sheet DuplicateSheet(string sheetName, string? newSheetName = null)
    {
        var source = GetSheet(sheetName);
        if (source is null)
            throw new Exception($"Sheet {sheetName} does not exist");

        var name = string.IsNullOrWhiteSpace(newSheetName) ? GenerateNewSheetName() : newSheetName!.Trim();
        if (GetSheet(name) is not null)
            throw new Exception($"Sheet {name} already exists");

        // The serialization model is the one complete description of a sheet, so the copy is
        // mapped out and back rather than assembled store by store - anything that survives a
        // save survives a duplicate.
        var formats = new List<CellFormat>();
        var model = SheetMapper.FromSheet(source, formats);
        model.Name = name;

        // The mapper hands back the source's own mutable objects. Formats and conditional
        // format rules carry per-sheet state - a rule caches the sheet it parsed against - so
        // the copy gets its own, while a rule shared across several regions stays one rule.
        var rules = new Dictionary<object, ConditionalFormatAbstractBase>(ReferenceEqualityComparer.Instance);
        model.ConditionalFormats = model.ConditionalFormats
            .Select(cf => new ConditionalFormatModel
            {
                RegionString = cf.RegionString,
                RuleType = cf.RuleType,
                Rule = Cloned(cf.Rule)
            }).ToList();

        ConditionalFormatAbstractBase Cloned(ConditionalFormatAbstractBase rule)
        {
            if (!rules.TryGetValue(rule, out var clone))
            {
                clone = rule.Clone();
                rules[rule] = clone;
            }

            return clone;
        }

        var copy = new Sheet(model.NumRows, model.NumCols, model.DefaultWidth, model.DefaultHeight, this);
        AddSheet(name, copy);
        SheetMapper.PopulateFromModel(model, formats.Select(f => f.Clone()).ToList(), copy);
        _formulaEngine.CalculateSheet(true);

        return copy;
    }

    public void RemoveSheet(string sheetName)
    {
        var sheetIndex = _sheets.FindIndex(s => s.Name == sheetName);
        if (sheetIndex >= 0)
        {
            var sheet = _sheets[sheetIndex];
            _formulaEngine.RemoveSheet(_sheets[sheetIndex]);
            _sheets.RemoveAt(sheetIndex);
            _formulaEngine.CalculateSheet(true);
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