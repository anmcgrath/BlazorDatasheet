using BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

namespace BlazorDatasheet.FormulaEngine.Interpreter;

public class TreeToText
{
    public static string ToFormulaText(SyntaxTree tree)
    {
        var root = tree.Root;
        return root.ToExpressionText();
    }
}