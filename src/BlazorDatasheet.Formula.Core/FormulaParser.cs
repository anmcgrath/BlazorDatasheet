using BlazorDatasheet.Formula.Core.Interpreter.Syntax;

namespace BlazorDatasheet.Formula.Core;

public class FormulaParser
{
    private Parser _parser;
    private Lexer _lexer;

    public FormulaParser()
    {
        _parser = new();
        _lexer = new();
    }

    public CellFormula FromString(string formulaString)
    {
        var syntaxTree = _parser.Parse(_lexer, formulaString);
        return new CellFormula(syntaxTree);
    }
}