using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Core.FormulaEngine;

internal class FormulaVertex : Vertex, IEquatable<FormulaVertex>
{
    public FormulaVertex(string name, CellFormula? formula)
    {
        _key = name;
        Formula = formula;
        VertexType = VertexType.Named;
    }

    public FormulaVertex(IRegion region, CellFormula? formula)
    {
        Region = region;
        Formula = formula;
        _key = RangeText.ToRegionText(Region);
        VertexType = VertexType.Region;
    }

    public FormulaVertex(int row, int col, CellFormula? formula) : this(new Region(row, row, col, col), formula)
    {
        VertexType = VertexType.Cell;
    }

    private readonly string _key;
    public override string Key => _key;
    public IRegion? Region { get; }
    public CellFormula? Formula { get; private set; }

    internal VertexType VertexType { get; private set; }

    public bool Equals(FormulaVertex? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _key == other._key;
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