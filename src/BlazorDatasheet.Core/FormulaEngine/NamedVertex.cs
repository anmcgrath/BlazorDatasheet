using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Core.FormulaEngine;

public class NamedVertex : Vertex<CellFormula?>
{
    private readonly string _name;

    public NamedVertex(string name, CellFormula? formula)
    {
        _name = name;
        Data = formula;
    }

    public override string Key => _name;
    public override CellFormula? Data { get; }
}