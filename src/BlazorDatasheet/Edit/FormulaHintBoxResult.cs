using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Edit;

internal class FormulaHintBoxResult
{
    /// <summary>
    /// The function name
    /// </summary>
    public string FunctionName { get; }

    /// <summary>
    /// The parameter index of the result. If -1, the cursor is inside the function.
    /// </summary>
    public int ParameterIndex { get; }

    public FormulaHintBoxResult(string functionName, int parameterIndex)
    {
        FunctionName = functionName;
        ParameterIndex = parameterIndex;
    }
}