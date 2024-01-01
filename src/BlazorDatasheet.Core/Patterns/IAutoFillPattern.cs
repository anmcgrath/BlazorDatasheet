using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Patterns;

public interface IAutoFillPattern
{
    /// <summary>
    /// Each pattern is applied to a set of data, but only applies to a subset of the data.
    /// The offsets specifies this subset of *original* data.
    /// </summary>
    public ICollection<int> Offsets { get; }

    /// <summary>
    /// Returns the command that should be applied to apply the pattern
    /// to the cell position given, with the offset from the start of the pattern.
    /// </summary>
    /// <param name="offset">The offset in the original data that the pattern applies to.</param>
    /// <param name="repeatNo">The number of times the pattern has repeated, starting from 0</param>
    /// <param name="cellData">The data that corresponds to the offset.</param>
    /// <param name="newDataPosition">The cell position that the pattern should be applied to.</param>
    /// <returns></returns>
    public ICommand GetCommand(int offset, int repeatNo, IReadOnlyCell cellData, CellPosition newDataPosition);
}