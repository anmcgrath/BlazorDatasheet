using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.FormulaEngine;

internal class DependencyManager
{
    
    public void AddFormula(IRegion region, string formula)
    {
    }

    public bool HasDependents(IRegion region)
    {
        return true;
    }

    public bool HasDependents(int row, int col)
    {
        return true;
    }

    public void InsertRows(int row, int nRows)
    {
    }

    public void InsertCols(int row, int nRows)
    {
    }

    public void RemoveCols(int row, int nRows)
    {
    }

    public void RemoveRows(int row, int nRows)
    {
    }
}