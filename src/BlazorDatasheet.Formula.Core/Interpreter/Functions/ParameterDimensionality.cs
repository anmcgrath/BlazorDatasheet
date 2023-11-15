namespace BlazorDatasheet.Formula.Core.Interpreter.Functions;

public enum ParameterDimensionality
{
    /// <summary>
    /// Only one value is accepted, ranges of values are not.
    /// </summary>
    Scalar,
    /// <summary>
    /// Multiple values (one or more) are accepted.
    /// </summary>
    Range
}