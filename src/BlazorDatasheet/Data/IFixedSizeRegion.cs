namespace BlazorDatasheet.Data;

public interface IFixedSizeRegion : IRegion, IEnumerable<CellPosition>
{
    /// <summary>
    /// Moves the entire region by the specified amount
    /// </summary>
    /// <param name="dRow"></param>
    /// <param name="dCol"></param>
    /// <param name="regionLimit">The limiting region that the region cannot move outside of</param>
    public void Move(int dRow, int dCol, IRegion? regionLimit = null);
    
}