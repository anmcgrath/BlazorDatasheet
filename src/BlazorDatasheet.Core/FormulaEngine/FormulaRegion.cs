using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Core.FormulaEngine;

public class RegionVertex : Vertex<CellFormula?>
{
    public IRegion Region { get; }
    private string _regionName;

    public override string Key => _regionName;
    public override CellFormula? Data { get; }

    public RegionVertex(IRegion region, CellFormula? formula)
    {
        Region = region;
        Data = formula;
        _regionName = RangeText.ToRegionText(Region);
    }

    public RegionVertex(int row, int col, CellFormula? formula) : this(new Region(row, row, col, col), formula)
    {
    }
}