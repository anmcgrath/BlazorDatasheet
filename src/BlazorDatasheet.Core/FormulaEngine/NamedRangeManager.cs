using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Core.FormulaEngine;

public class NamedRangeManager
{
    private readonly Sheet _sheet;

    /// <summary>
    /// Stores named ranges - we store in a computed formula rather than a range to allow for dynamic ranges
    /// </summary>
    private Dictionary<string, CellFormula> _namedRanges = new();

    public NamedRangeManager(Sheet sheet)
    {
        _sheet = sheet;
    }

    /// <summary>
    /// Set a named range.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="rangeString"></param>
    /// <returns>Whether the name was set successfully.</returns>
    public bool Set(string name, string rangeString)
    {
        if (string.IsNullOrEmpty(rangeString))
            return false;

        if (!RangeText.IsValidName(name))
        {
            return false;
        }

        if (_namedRanges.ContainsKey(name))
            Clear(name);

        var rangeStrFormula = $"={rangeString}";
        var formula = _sheet.FormulaEngine.ParseFormula(rangeStrFormula, _sheet.Name, true);

        var evaluatedValue = _sheet.FormulaEngine.Evaluate(formula, resolveReferences: false);
        if (evaluatedValue.ValueType == CellValueType.Reference && evaluatedValue.GetValue<Reference>()?.Region != null)
        {
            _sheet.FormulaEngine.SetVariable(name, formula.ToFormulaString());
            _namedRanges[name] = _sheet.FormulaEngine.DependencyManager.GetVertex(name)!.Formula!;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Clears a named range.
    /// </summary>
    /// <param name="name"></param>
    public void Clear(string name)
    {
        if (_namedRanges.ContainsKey(name))
        {
            _namedRanges.Remove(name);
            _sheet.FormulaEngine.ClearVariable(name);
        }
    }

    /// <summary>
    /// Returns the name for the region, if any. Returns null if no name is found.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public string? GetRegionName(IRegion region)
    {
        foreach (var kp in _namedRanges)
        {
            var formula = kp.Value;
            if (formula.References.Count() == 1)
            {
                if (formula.References.First().Region.Equals(region))
                {
                    return kp.Key;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the named range <paramref name="name"/> as  as string.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string? GetRangeString(string name)
    {
        if (_namedRanges.TryGetValue(name, out var formula))
        {
            return formula.ToFormulaString(includeEquals: false);
        }

        return null;
    }
}