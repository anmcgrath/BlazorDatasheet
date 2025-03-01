using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Formula.Core.Dependencies;

public class FormulaVertex : Vertex, IEquatable<FormulaVertex>
{
    public string SheetName { get; private set; }

    public FormulaVertex(string name, CellFormula? formula)
    {
        _key = name;
        Formula = formula;
        VertexType = VertexType.Named;
    }

    public FormulaVertex(IRegion region, string sheetName, CellFormula? formula)
    {
        SheetName = sheetName;
        Region = region;
        Formula = formula;
        UpdateKey();
        if (region.Width == 1 && region.Height == 1)
            VertexType = VertexType.Cell;
        else
            VertexType = VertexType.Region;
    }

    public FormulaVertex(int row, int col, string sheetName, CellFormula? formula) : this(
        new Region(row, row, col, col), sheetName, formula)
    {
        VertexType = VertexType.Cell;
    }

    private string _key = string.Empty;
    public override string Key => _key;

    public sealed override void UpdateKey()
    {
        if (VertexType != VertexType.Named)
            _key = $"'{SheetName}'!" + RangeText.RegionToText(Region!);
    }

    public IRegion? Region { get; }
    public CellFormula? Formula { get; set; }

    public int? Row => Region?.Top;
    public int? Col => Region?.Left;

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