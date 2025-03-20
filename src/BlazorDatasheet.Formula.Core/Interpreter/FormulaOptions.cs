using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Formula.Core.Interpreter;

public class FormulaOptions
{
    public SeparatorSettings SeparatorSettings { get; init; } = new();
}