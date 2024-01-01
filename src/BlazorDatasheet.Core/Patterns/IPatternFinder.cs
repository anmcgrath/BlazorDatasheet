using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Patterns;

public interface IPatternFinder
{
    public IEnumerable<IAutoFillPattern> Find(IReadOnlyCell[] cells);
}