namespace BlazorDatasheet.Formula.Core;

public enum ErrorType
{
    Null = 1,
    Div0 = 2,
    Value = 3,
    Ref = 4,
    Name = 5,
    Num = 6,
    Na = 7,
    Circular = 9,
    None = 0,
}

public static class ErrorTypes
{
    public static string ToErrorString(this ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Null => "#NULL!",
            ErrorType.Div0 => "#DIV/0!",
            ErrorType.Value => "#VALUE!",
            ErrorType.Ref => "#REF!",
            ErrorType.Name => "#NAME?",
            ErrorType.Num => "#NUM!",
            ErrorType.Na => "#N/A",
            ErrorType.Circular => "#CIRCULAR",
            _ => "Invalid"
        };
    }
    
    public static ErrorType FromErrorString(ReadOnlySpan<char> errorString)
    {
        return errorString switch
        {
            "#NULL!" => ErrorType.Null,
            "#DIV/0!" => ErrorType.Div0,
            "#VALUE!" => ErrorType.Value,
            "#REF!" => ErrorType.Ref,
            "#NAME?" => ErrorType.Name,
            "#NUM!" => ErrorType.Num,
            "#N/A" => ErrorType.Na,
            "#CIRCULAR" => ErrorType.Circular,
            _ => ErrorType.None
        };
    }
}