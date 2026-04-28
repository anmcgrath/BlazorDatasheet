namespace BlazorDatasheet.Formula.Core;

public readonly record struct FunctionCallMetaData(
    int? CallingRowIndex = null,
    int? CallingColumnIndex = null,
    string? CallingSheetName = null)
{
}
