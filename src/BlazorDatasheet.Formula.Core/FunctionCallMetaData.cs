namespace BlazorDatasheet.Formula.Core;

public readonly record struct FunctionCallMetaData(
    int? CallingRowIndex = null,
    int? CallingColumnIndex = null,
    string? CallingSheetName = null)
{
    public bool HasCaller => CallingRowIndex != null && CallingColumnIndex != null && CallingSheetName != null;
}
