namespace BlazorDatasheet.Formula.Core;

public class Variable
{
    public string? Formula { get; }
    public string Name { get; }
    public CellValue? Value { get; }

    public string? SheetName { get; }

    public Variable(string name, string? formula, string? sheetName, CellValue? value)
    {
        Formula = formula;
        SheetName = sheetName;
        Name = name;
        Value = value;
    }
}