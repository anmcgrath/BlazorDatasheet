namespace BlazorDatasheet.Formula.Core;

public enum ParameterType
{
    Number,
    Integer,
    Logical,
    Any,
    Text,
    NumberSequence,
    LogicalSequence,
    Date,
    DateSequence,
    Array,
}

public static class ParameterTypExtensions
{
    public static bool IsScalar(this ParameterType type)
    {
        return type == ParameterType.Date ||
               type == ParameterType.Number ||
               type == ParameterType.Text ||
               type == ParameterType.Integer ||
               type == ParameterType.Logical;
    }
}