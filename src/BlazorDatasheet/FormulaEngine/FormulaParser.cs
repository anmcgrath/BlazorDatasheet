using BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

namespace BlazorDatasheet.FormulaEngine;

public class FormulaParser
{
    private Parser _parser;
    private Lexer _lexer;

    public FormulaParser()
    {
        _parser = new();
        _lexer = new();
    }

    public Formula FromString(string formulaString)
    {
        var syntaxTree = _parser.Parse(_lexer, formulaString);
        return new Formula(syntaxTree);
    }
}