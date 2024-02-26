using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter;

public class CellFormula
{
    internal readonly SyntaxTree ExpressionTree;
    public IEnumerable<Reference> References => ExpressionTree.References;

    internal CellFormula(SyntaxTree expressionTree)
    {
        ExpressionTree = expressionTree;
    }

    public bool IsValid()
    {
        return !ExpressionTree.Errors.Any();
    }

    public string ToFormulaString() => "=" + ExpressionTree.Root.ToExpressionText();

    public void ShiftReferences(int offsetRow, int offsetCol)
    {
        foreach (var reference in References)
        {
            reference.Shift(offsetRow, offsetCol);
        }
    }

    public CellFormula Clone()
    {
        var parser = new Parser();
        return new CellFormula(parser.Parse(ToFormulaString()));
    }
}