using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core;

/// <summary>
/// The result of resolving a structured reference. Exactly one of <see cref="Region"/>, <see cref="Values"/>
/// or <see cref="Error"/> is set.
/// </summary>
/// <param name="Region">The cells the reference refers to, on <paramref name="SheetName"/>.</param>
/// <param name="SheetName">The sheet the table is on.</param>
/// <param name="TableName">The name of the table that the reference was resolved against.</param>
/// <param name="Values">
/// Values for parts of a table that have no cells, e.g. the headers of a sheet table, which are the
/// sheet's column headings.
/// </param>
/// <param name="Error">The error when the reference cannot be resolved, otherwise <see cref="ErrorType.None"/>.</param>
public readonly record struct StructuredReferenceResolution(
    IRegion? Region,
    string? SheetName,
    string? TableName,
    CellValue[][]? Values,
    ErrorType Error)
{
    public bool IsError => Error != ErrorType.None;

    public static StructuredReferenceResolution FromRegion(IRegion region, string sheetName, string tableName) =>
        new(region, sheetName, tableName, null, ErrorType.None);

    public static StructuredReferenceResolution FromValues(CellValue[][] values, string sheetName, string tableName) =>
        new(null, sheetName, tableName, values, ErrorType.None);

    public static StructuredReferenceResolution FromError(ErrorType error, string? tableName = null) =>
        new(null, null, tableName, null, error);
}

/// <summary>
/// Resolves structured references (e.g. Sales[Amount] or [@Qty]) against the tables in a workbook.
/// </summary>
public interface IStructuredReferenceResolver
{
    /// <summary>
    /// Resolves <paramref name="descriptor"/> to the region (or values) it refers to.
    /// An unknown table or column, #Totals on a table without a totals row (or on a sheet table), or an
    /// unqualified reference whose caller is not in a table resolves to <see cref="ErrorType.Ref"/>.
    /// A this-row reference (@) without a caller, or whose caller is not in a data row of the table,
    /// resolves to <see cref="ErrorType.Value"/>.
    /// </summary>
    /// <param name="descriptor">The structured reference.</param>
    /// <param name="callerRow">The row of the cell containing the formula, if any.</param>
    /// <param name="callerCol">The column of the cell containing the formula, if any.</param>
    /// <param name="callerSheet">The sheet of the cell containing the formula, if any.</param>
    StructuredReferenceResolution Resolve(StructuredReferenceDescriptor descriptor, int? callerRow, int? callerCol,
        string? callerSheet);

    /// <summary>
    /// The names of all tables.
    /// </summary>
    IEnumerable<string> GetTableNames();
}
