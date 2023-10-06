using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Events.Validation;
using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Validation;

public class ValidationManager
{
    public ConsolidatedDataStore<int> Store { get; }
    private readonly Dictionary<int, IDataValidator> _validators;

    /// <summary>
    /// Fired when a validator is added or removed from a region(s).
    /// </summary>
    public event EventHandler<ValidatorChangedEventArgs> ValidatorChanged;

    public ValidationManager()
    {
        Store = new ConsolidatedDataStore<int>();
        _validators = new Dictionary<int, IDataValidator>();
    }

    /// <summary>
    /// Adds the data validator to the region
    /// </summary>
    /// <param name="validator"></param>
    /// <param name="region"></param>
    public void Add(IDataValidator validator, IRegion region)
    {
        // if the validator already exists, find the index
        int? index = GetValidatorIndex(validator);

        if (index == null)
        {
            index = _validators.Count;
            _validators.Add(index.Value, validator);
        }

        Store.Add(region, index.Value);
        var args = new ValidatorChangedEventArgs(Array.Empty<IDataValidator>(),
                                                 new List<IDataValidator>() { validator },
                                                 Array.Empty<IRegion>());

        ValidatorChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Removes the data validator from the region
    /// </summary>
    /// <param name="validator"></param>
    /// <param name="???"></param>
    public void Remove(IDataValidator validator, IRegion region)
    {
        var index = GetValidatorIndex(validator);
        if (index == null)
            return;

        var result = Store.Cut(region, index.Value);
        var validatorsRemoved = new List<IDataValidator>();

        if (!Store.GetRegionsForData(index.Value).Any())
        {
            validatorsRemoved.Add(_validators[index.Value]);
            _validators.Remove(index.Value);
        }

        var args = new ValidatorChangedEventArgs(validatorsRemoved, Array.Empty<IDataValidator>(),
                                                 result.regionsRemoved.Concat(result.regionsAdded));
        ValidatorChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Adds the data validator to a cell position
    /// </summary>
    /// <param name="validator"></param>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void Add(IDataValidator validator, int row, int col)
    {
        Add(validator, new Region(row, col));
    }

    /// <summary>
    /// Returns all data validators for a cell position.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public IEnumerable<IDataValidator> GetValidators(int row, int col)
    {
        return Store.GetDataOverlapping(row, col)
                    .Where(x => _validators.ContainsKey(x))
                    .Select(x => _validators[x]);
    }

    /// <summary>
    /// Validates a value against validators defined at a row/column.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public ValidationResult Validate(object? value, int row, int col)
    {
        var validators = GetValidators(row, col);
        bool isStrict = false;
        bool isValid = true;
        var failMessages = new List<string>();
        foreach (var validator in validators)
        {
            if (validator.IsValid(value))
                continue;
            failMessages.Add(validator.Message);
            isValid = false;
            isStrict |= validator.IsStrict;
        }

        return new ValidationResult(failMessages, isStrict, isValid);
    }

    /// <summary>
    /// Returns the index of a data validator. Returns null if it doesn't exist.
    /// </summary>
    /// <param name="validator"></param>
    /// <returns></returns>
    private int? GetValidatorIndex(IDataValidator validator)
    {
        foreach (var kp in _validators)
        {
            if (kp.Value == validator)
                return kp.Key;
        }

        return null;
    }
}