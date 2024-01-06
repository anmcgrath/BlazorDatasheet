namespace BlazorDatasheet.Formula.Core;

public readonly struct FormulaError
{
    public string Message { get; }
    public ErrorType ErrorType { get; }

    public FormulaError(ErrorType errorType, string message = "")
    {
        Message = message;
        ErrorType = errorType;
    }

    public override string ToString()
    {
        return $"#{ErrorType.ToString().ToUpper()}";
    }
}