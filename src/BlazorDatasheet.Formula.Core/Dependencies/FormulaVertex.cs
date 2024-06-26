using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Formula.Core.Dependencies;

public class FormulaVertex : Vertex, IEquatable<FormulaVertex>
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
        if (region.Width == 1 && region.Height == 1)
            VertexType = VertexType.Cell;
        else
            VertexType = VertexType.Region;
    }

    public FormulaVertex(int row, int col, CellFormula? formula) : this(new Region(row, row, col, col), formula)
    {
        VertexType = VertexType.Cell;
    }

    private string _key;
    public override string Key => _key;

    public override void UpdateKey()
    {
        if (VertexType != VertexType.Named)
            _key = RangeText.ToRegionText(Region);
    }

    public IRegion? Region { get; }
    public CellFormula? Formula { get; private set; }

    public VertexType VertexType { get; private set; }

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