using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Formatting;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Validation;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Validation;

public class ValidationManager
{
    private readonly Sheet _sheet;
    internal ConsolidatedDataStore<int> Store { get; } = new();
    private readonly Dictionary<int, IDataValidator> _validators = new();

    public ValidationManager(Sheet sheet)
    {
        _sheet = sheet;
    }

    /// <summary>
    /// Fired when a validator is added or removed from a region(s).
    /// </summary>
    public event EventHandler<ValidatorChangedEventArgs> ValidatorChanged;

    /// <summary>
    /// Add a <see cref="IDataValidator"> to a cell.
    /// </summary>
    /// <param name="col"></param>
    /// <param name="validator"></param>
    /// <param name="row"></param>
    public void Add(int row, int col, IDataValidator validator) =>
        Add(new Region(row, col), validator);

    /// <summary>
    /// Adds multiple validators to a region.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="validators"></param>
    public void Add(IRegion region, IEnumerable<IDataValidator> validators)
    {
        _sheet.Commands.BeginCommandGroup();
        foreach (var validator in validators)
        {
            _sheet.Validators.Add(region, validator);
        }

        _sheet.Commands.EndCommandGroup();
    }

    /// <summary>
    /// Add a <see cref="IDataValidator"> to a region with the <see cref="SetValidatorCommand"/>
    /// </summary>
    /// <param name="region"></param>
    /// <param name="validator"></param>
    public void Add(IRegion region, IDataValidator validator)
    {
        var cmd = new SetValidatorCommand(region, validator);
        _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Adds the data validator to the region
    /// </summary>
    /// <param name="validator"></param>
    /// <param name="region"></param>
    internal void AddImpl(IDataValidator validator, IRegion region)
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
    /// Adds the data validator to a cell position
    /// </summary>
    /// <param name="validator"></param>
    /// <param name="row"></param>
    /// <param name="col"></param>
    internal void AddImpl(IDataValidator validator, int row, int col)
    {
        AddImpl(validator, new Region(row, col));
    }


    /// <summary>
    /// Clears the data validator from the region
    /// </summary>
    /// <param name="validator"></param>
    /// <param name="???"></param>
    public void Clear(IDataValidator validator, IRegion region)
    {
        var index = GetValidatorIndex(validator);
        if (index == null)
            return;

        var restoreData = Store.Clear(region, index.Value);
        var validatorsRemoved = new List<IDataValidator>();

        if (!Store.GetRegions(index.Value).Any())
        {
            validatorsRemoved.Add(_validators[index.Value]);
            _validators.Remove(index.Value);
        }

        var args = new ValidatorChangedEventArgs(validatorsRemoved, Array.Empty<IDataValidator>(),
            restoreData.RegionsRemoved.Concat(restoreData.RegionsAdded).Select(x => x.Region));
        ValidatorChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Returns all data validators for a cell position.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public IEnumerable<IDataValidator> Get(int row, int col)
    {
        return Store.GetData(row, col)
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
    public ValidationResult Validate(CellValue value, int row, int col)
    {
        var validators = Get(row, col);
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