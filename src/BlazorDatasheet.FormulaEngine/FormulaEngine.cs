using BlazorDatasheet.FormulaEngine.Interfaces;
using BlazorDatasheet.FormulaEngine.Interpreter;
using BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

namespace BlazorDatasheet.FormulaEngine;

public class FormulaEngine
{
    private readonly ISheet _sheet;
    private Environment _environment;
    private readonly Parser _parser = new Parser();
    private readonly Lexer _lexer = new Lexer();
    private readonly FormulaEvaluator _evaluator;
    private readonly Dictionary<(int row, int col), Formula> _formula = new();

    public FormulaEngine(ISheet sheet)
    {
        _sheet = sheet;
        _environment = new Environment(sheet);
        _evaluator = new FormulaEvaluator(_environment);
    }

    public void SetFormula(int row, int col, Formula formula)
    {
        if (!_formula.TryAdd((row, col), formula))
            _formula[(row, col)] = formula;


        _sheet.SetValue(row, col, Evaluate(row, col));
        OnUpdateValue(row, col);
    }

    public object Evaluate(int row, int col)
    {
        if (!_formula.ContainsKey((row, col)))
            return null;
        return _evaluator.Evaluate(_formula[(row, col)].ExpressionTree);
    }

    public Formula Parse(string formulaText)
    {
        return new Formula(_parser.Parse(_lexer, formulaText));
    }

    public void OnUpdateValue(int row, int col)
    {
        // Find dependencies and calculate them, and set sheet values
        
    }
}