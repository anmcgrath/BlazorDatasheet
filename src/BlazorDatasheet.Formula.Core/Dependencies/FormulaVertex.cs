using System.Runtime.CompilerServices;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Formula.Core.Dependencies;

public class FormulaVertex : Vertex, IEquatable<FormulaVertex>
{
    public string SheetName { get; private set; }

    public FormulaVertex(string name, CellFormula? formula)
    {
        _key = GetKey(name);
        Formula = formula;
        VertexType = VertexType.Named;
    }

    public FormulaVertex(int row, int col, string sheetName, CellFormula? formula)
    {
        SheetName = sheetName;
        Position = new CellPosition(row, col);
        Formula = formula;
        // set before UpdateKey - it keys off the vertex type
        VertexType = VertexType.Cell;
        UpdateKey();
    }

    private VertexKey _key;
    public override VertexKey Key => _key;

    public sealed override void UpdateKey()
    {
        if (VertexType == VertexType.Cell)
            _key = GetKey(Row, Col, SheetName);
    }

    internal static VertexKey GetKey(int row, int col, string sheetName)
    {
        return VertexKey.ForCell(row, col, sheetName);
    }

    internal static VertexKey GetKey(string name)
    {
        return VertexKey.ForName(name);
    }

    public CellPosition? Position { get; internal set; }
    public CellFormula? Formula { get; set; }
    public VertexType VertexType { get; private set; }

    public int Row => Position?.row ?? int.MinValue;
    public int Col => Position?.col ?? int.MinValue;

    public bool Equals(FormulaVertex? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _key.Equals(other._key);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((FormulaVertex)obj);
    }

    public override int GetHashCode()
    {
        return _key.GetHashCode();
    }
}