using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Validation;

public class ValidatorChangedEventArgs
{
    public IEnumerable<IDataValidator> ValidatorsRemoved { get; }
    public IEnumerable<IDataValidator> ValidatorsAdded { get; }
    public IEnumerable<IRegion> RegionsAffected { get; }

    public ValidatorChangedEventArgs(IEnumerable<IDataValidator> validatorsRemoved,
        IEnumerable<IDataValidator> validatorsAdded, IEnumerable<IRegion> regionsAffected)
    {
        ValidatorsRemoved = validatorsRemoved;
        ValidatorsAdded = validatorsAdded;
        RegionsAffected = regionsAffected;
    }
}