using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Patterns;

public class NumberPatternFinder : IPatternFinder
{
    public IEnumerable<IAutoFillPattern> Find(IReadOnlyCell[] cells)
    {
        if (cells.Length <= 1)
            return Enumerable.Empty<IAutoFillPattern>();
        
        var currentOffsets = new List<int>();
        var currentValues = new List<double>();
        var patterns = new List<NumberRegressionPattern>();

        bool inPattern = false;
        for (int i = 0; i < cells.Length; i++)
        {
            var isNumber = !cells[i].HasFormula() && 
                           cells[i].ValueType == CellValueType.Number;
            
            if (!isNumber && inPattern)
            {
                patterns.Add(new NumberRegressionPattern(currentOffsets.ToList(), currentValues.ToList()));
                currentOffsets.Clear();
                currentValues.Clear();
                inPattern = false;
                continue;
            }

            if (isNumber)
            {
                inPattern = true;
                currentOffsets.Add(i);
                currentValues.Add(cells[i].GetValue<double>());
            }
        }

        if (inPattern)
        {
            patterns.Add(new NumberRegressionPattern(currentOffsets.ToList(), currentValues.ToList()));
        }

        return patterns;
    }
}