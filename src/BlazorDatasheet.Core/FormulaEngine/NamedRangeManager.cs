using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.FormulaEngine;

/// <summary>
/// Manages named ranges for a workbook. Named ranges are workbook-wide variables whose value is a
/// reference to a range; the formula engine is the single source of truth for them.
/// </summary>
public class NamedRangeManager
{
    private readonly Workbook? _workbook;
    private readonly Sheet? _sheet;

    public NamedRangeManager(Workbook workbook)
    {
        _workbook = workbook;
    }

    /// <summary>
    /// Creates a sheet-scoped view of the workbook's named ranges. This constructor is retained for
    /// compatibility; named ranges themselves remain workbook-wide.
    /// </summary>
    public NamedRangeManager(Sheet sheet)
    {
        _sheet = sheet;
    }

    private Workbook Workbook => _sheet?.Workbook ?? _workbook!;
    private FormulaEngine Engine => Workbook.GetFormulaEngine();

    /// <summary>
    /// Set a named range.
    /// </summary>
    /// <param name="name">The name of the range.</param>
    /// <param name="rangeString">The range, e.g. "A1:B2" or "Sheet2!A1:B2".</param>
    /// <returns>Whether the name was set successfully.</returns>
    public bool Set(string name, string rangeString) => Set(name, rangeString, null);

    /// <summary>
    /// Set a named range, resolving un-qualified ranges against <paramref name="callingSheetName"/>.
    /// </summary>
    public bool Set(string name, string rangeString, string? callingSheetName)
    {
        if (string.IsNullOrEmpty(rangeString))
            return false;

        if (!RangeText.IsValidName(name))
            return false;

        callingSheetName ??= _sheet?.Name ?? Workbook.Sheets.FirstOrDefault()?.Name;
        if (callingSheetName == null)
            return false;

        var formula = Engine.ParseFormula($"={rangeString}", callingSheetName, true);
        var evaluatedValue = Engine.EvaluateFormula(formula, resolveReferences: false);
        if (evaluatedValue.ValueType != CellValueType.Reference ||
            evaluatedValue.GetValue<Formula.Core.Interpreter.References.Reference>()?.Region == null)
            return false;

        Engine.SetVariable(name, formula.ToFormulaString());
        return true;
    }

    /// <summary>
    /// Clears a named range. Variables that are not named ranges are left untouched.
    /// </summary>
    /// <param name="name"></param>
    public void Clear(string name)
    {
        if (IsNamedRange(name))
            Engine.ClearVariable(name);
    }

    /// <summary>
    /// Whether a named range with the name <paramref name="name"/> exists.
    /// </summary>
    public bool IsNamedRange(string name)
    {
        return Engine.GetVariableInfo(name)?.IsRange == true;
    }

    /// <summary>
    /// Returns the names of all named ranges in the workbook.
    /// </summary>
    public IReadOnlyList<string> GetNames()
    {
        return Engine.GetVariableInfos().Where(x => x.IsRange).Select(x => x.Name).ToList();
    }

    /// <summary>
    /// Returns detached snapshots of all named ranges in the workbook.
    /// </summary>
    public IReadOnlyList<VariableInfo> GetAll()
    {
        var ranges = Engine.GetVariableInfos().Where(x => x.IsRange);
        if (_sheet != null)
            ranges = ranges.Where(x => x.SheetName == _sheet.Name);
        return ranges.ToList();
    }

    /// <summary>
    /// Returns the name for the region, if any. Returns null if no name is found.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public string? GetRegionName(IRegion region) => GetRegionName(region, null);

    /// <summary>
    /// Returns the name for the region on <paramref name="sheetName"/>, if any.
    /// </summary>
    public string? GetRegionName(IRegion region, string? sheetName)
    {
        sheetName ??= _sheet?.Name;
        foreach (var info in Engine.GetVariableInfos())
        {
            if (!info.IsRange || info.References.Count != 1)
                continue;
            if (sheetName != null && info.SheetName != sheetName)
                continue;
            if (!info.References[0].IsInvalid && info.References[0].Region.Equals(region))
                return info.Name;
        }

        return null;
    }

    /// <summary>
    /// Returns the named range <paramref name="name"/> as a string, e.g. "Sheet1!A1:B2".
    /// Returns null if the name is not a named range.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string? GetRangeString(string name)
    {
        var info = Engine.GetVariableInfo(name);
        if (info is not { IsRange: true, Formula: not null })
            return null;

        return info.Formula.StartsWith('=') ? info.Formula.Substring(1) : info.Formula;
    }
}
