using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.FormulaEngine;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;

namespace BlazorDatasheet.Edit;

internal class FormulaHintBoxCalculator
{
    private FormulaOptions _options;

    public FormulaHintBoxCalculator(FormulaOptions options)
    {
        _options = options;
    }

    public FormulaHintBoxResult? Calculate(string inputString, int cursorPosition)
    {
        if (!FormulaEngine.IsFormula(inputString))
            return null;

        var lexer = new Lexer();
        var tokens = lexer.Lex(inputString, _options);
        if (tokens.Count <= 2) // equals and EoF
            return null;

        return ExtractResult(tokens, cursorPosition);
    }

    private FormulaHintBoxResult? ExtractResult(List<Token> tokens, int cursorPosition)
    {
        // now we have a position start, so we can find the function node in the parsed expression
        // we don't care if the function exists, so we can have a dummy environment
        var parser = new Parser(new WorkbookEnvironment(new Workbook(_options)), _options);
        var tree = parser.Parse(tokens, []);
        var funcNodes = tree.Root.FindNodes(node =>
        {
            if (node.Kind != NodeKind.FunctionCall)
                return false;
            var funcExpr = (FunctionExpression)node;
            if (funcExpr.FunctionToken.PositionStart <= cursorPosition)
                return true;
            return false;
        }).Cast<FunctionExpression>();

        var funcNode = funcNodes
            .OrderByDescending(x => x.FunctionToken.PositionStart)
            .FirstOrDefault(x => x.IsPositionInsideFunc(cursorPosition));

        if (funcNode == null)
            return null;

        if (!funcNode.IsPositionInsideFunc(cursorPosition))
            return null;

        return new FormulaHintBoxResult(funcNode.FunctionToken.Value, funcNode.GetArgIndex(cursorPosition));
    }
}