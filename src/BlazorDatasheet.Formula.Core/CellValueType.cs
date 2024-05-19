namespace BlazorDatasheet.Formula.Core;

public enum CellValueType
{
    // order here is important for cell value comparisons
    // excel and google sheets seem to use COMPLEX > BOOL > TEXT > NUM
    Empty = 0,
    Error = 1,
    Array = 2,
    Unknown = 3,
    Sequence = 4,
    Reference = 5,
    Number = 6,
    Date = 7,
    Text = 8,
    Logical = 9,
}