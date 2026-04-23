using System.Diagnostics.CodeAnalysis;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter;

/// <summary>
/// A lightweight <see cref="IEnvironment"/> for parse-only scenarios (e.g. hint calculation).
/// Data-access members throw because parsing never reads cell or variable state.
/// </summary>
public sealed class ParsingEnvironment : IEnvironment
{
    private readonly FunctionRegistry _functionRegistry;

    public ParsingEnvironment(FunctionRegistry functionRegistry)
    {
        _functionRegistry = functionRegistry;
    }

    public bool TryGetFunction(string functionIdentifier, [MaybeNullWhen(false)] out FunctionDescriptor functionDescriptor)
        => _functionRegistry.TryGetFunction(functionIdentifier, out functionDescriptor);

    public IEnumerable<FunctionDefinition> SearchForFunctions(string functionName)
        => _functionRegistry.SearchForFunctions(functionName);

    public CellValue GetCellValue(int row, int col, string sheetName) => throw NotSupported();
    public CellFormula? GetFormula(int row, int col, string sheetName) => throw NotSupported();
    public CellValue[][] GetRangeValues(Reference reference) => throw NotSupported();
    public bool VariableExists(string variableIdentifier) => throw NotSupported();
    public CellValue GetVariable(string variableIdentifier) => throw NotSupported();
    public bool TryGetVariable(string variableIdentifier, out CellValue value) => throw NotSupported();
    public void SetVariable(string name, CellValue value) => throw NotSupported();
    public IEnumerable<CellValue> GetNonEmptyInRange(Reference reference) => throw NotSupported();
    public void SetCellValue(int row, int col, string sheetName, CellValue value) => throw NotSupported();
    public void ClearVariable(string varName) => throw NotSupported();
    public IEnumerable<string> GetVariableNames() => throw NotSupported();

    private static NotSupportedException NotSupported() =>
        new("ParsingEnvironment does not support runtime data access.");
}
