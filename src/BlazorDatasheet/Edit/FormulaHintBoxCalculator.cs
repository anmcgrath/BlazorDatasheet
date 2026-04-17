using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.FormulaEngine;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;

namespace BlazorDatasheet.Edit;

internal class FormulaHintBoxCalculator
{
    private static readonly ParsingEnvironment s_parsingEnvironment =
        new(Workbook.BuildDefaultRegistry(options: null));

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
        var parser = new Parser(s_parsingEnvironment, _options);
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
