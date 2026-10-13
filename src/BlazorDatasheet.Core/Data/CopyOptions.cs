namespace BlazorDatasheet.Core.Data;

/// <summary>
/// How the references of copied formulas are adjusted for their new position.
/// </summary>
public enum ReferenceAdjustment
{
    /// <summary>
    /// References shift with the formula, honouring $ fixity, as when a formula is copied or filled.
    /// </summary>
    Relative,

    /// <summary>
    /// Only references to cells inside the copied region move, ignoring $ fixity, and every other reference
    /// is left pointing where it was, as when cells are cut and pasted.
    /// </summary>
    MoveWithSource
}

public class CopyOptions
{
    public bool CopyFormula { get; set; } = true;
    public bool CopyValues { get; set; } = true;
    public bool CopyFormat { get; set; } = true;

    /// <summary>
    /// Whether merged cells fully inside the source are reproduced at the target. Merges at the target are
    /// removed first.
    /// </summary>
    public bool CopyMerges { get; set; } = true;

    /// <summary>
    /// Whether cell types are copied.
    /// </summary>
    public bool CopyTypes { get; set; } = true;

    /// <summary>
    /// How formula references are adjusted. Only applies when <see cref="CopyFormula"/> is true.
    /// </summary>
    public ReferenceAdjustment FormulaReferences { get; set; } = ReferenceAdjustment.Relative;

    public static CopyOptions DefaultCopyOptions => new CopyOptions();

    /// <summary>
    /// Options for pasting only the (calculated) values of the source.
    /// </summary>
    public static CopyOptions ValuesOnly => new()
        { CopyValues = true, CopyFormula = false, CopyFormat = false, CopyMerges = false, CopyTypes = false };

    /// <summary>
    /// Options for pasting only the formatting (and merges) of the source.
    /// </summary>
    public static CopyOptions FormatsOnly => new()
        { CopyValues = false, CopyFormula = false, CopyFormat = true, CopyMerges = true, CopyTypes = false };
}
