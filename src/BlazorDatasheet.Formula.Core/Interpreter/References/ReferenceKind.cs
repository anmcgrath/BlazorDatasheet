namespace BlazorDatasheet.Formula.Core.Interpreter.References;

public enum ReferenceKind
{
    Cell,
    Range,
    Named,
    /// <summary>A1# - the cells an array formula at A1 has spilled into.</summary>
    SpillRange
}