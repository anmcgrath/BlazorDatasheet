using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core;

/// <summary>
/// Describes how a variable is defined.
/// </summary>
public enum VariableKind
{
    /// <summary>
    /// The variable holds a literal value that was set directly.
    /// </summary>
    Value,

    /// <summary>
    /// The variable is defined by a formula, and its value is the calculated result of that formula.
    /// </summary>
    Formula
}

/// <summary>
/// A read-only snapshot of a variable defined in the formula engine. References, regions, arrays and
/// sequences are detached from the engine. Mutable custom objects stored in a <see cref="CellValue"/>
/// remain the caller's responsibility.
/// </summary>
public sealed class VariableInfo
{
    /// <summary>
    /// The name of the variable.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Whether the variable is a literal value or defined by a formula.
    /// </summary>
    public VariableKind Kind { get; }

    /// <summary>
    /// The formula string (including the leading '='), or null if the variable is a literal value.
    /// </summary>
    public string? Formula { get; }

    /// <summary>
    /// The current value of the variable. For formula variables this is the calculated result.
    /// </summary>
    public CellValue Value { get; }

    /// <summary>
    /// The type of the current value.
    /// </summary>
    public CellValueType ValueType => Value.ValueType;

    /// <summary>
    /// True if the variable's formula resolves to a range/cell reference, i.e. it is a named range.
    /// The <see cref="Value"/> of a named range is the resolved value of the range (e.g. an array), and
    /// the range itself is described by <see cref="Region"/> and <see cref="SheetName"/>.
    /// </summary>
    public bool IsRange => Region != null;

    /// <summary>
    /// Detached copies of the references used in the variable's formula. Empty for literal values.
    /// </summary>
    public IReadOnlyList<Reference> References { get; }

    /// <summary>
    /// The region the variable resolves to when <see cref="IsRange"/> is true, otherwise null.
    /// </summary>
    public IRegion? Region { get; }

    /// <summary>
    /// The sheet the variable's range is on when <see cref="IsRange"/> is true, otherwise null.
    /// </summary>
    public string? SheetName { get; }

    public VariableInfo(string name, VariableKind kind, string? formula, CellValue value,
        IReadOnlyList<Reference> references, IRegion? region, string? sheetName)
    {
        Name = name;
        Kind = kind;
        Formula = formula;
        Value = value;
        References = references;
        Region = region;
        SheetName = sheetName;
    }
}
