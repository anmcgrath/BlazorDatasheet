using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Regression;

namespace BlazorDatasheet.Core.Patterns;

public class NumberRegressionPattern : IAutoFillPattern
{
    public ICollection<int> Offsets { get; }
    public LinearFunction LinearFunction { get; }

    public NumberRegressionPattern(List<int> offsets, List<double> values)
    {
        Offsets = offsets;

        if (values.Count == 1)
        {
            LinearFunction = new LinearFunction(1, values[0] - 1);
            return;
        }

        var x = Enumerable.Range(1, values.Count).Select(p => (double)p).ToList();
        var regression = new LinearRegression();
        LinearFunction = regression.Calculate(x, values);
    }

    public ICommand GetCommand(int offset, int repeatNo, IReadOnlyCell cellData, CellPosition newDataPosition)
    {
        var regressionOffset = offset + (repeatNo + 1) * Offsets.Count + 1;
        var val = LinearFunction.ComputeY(regressionOffset);
        var setNumCommand = new SetCellValueCommand(newDataPosition.row, newDataPosition.col, CellValue.Number(val));
        return setNumCommand;
    }
}