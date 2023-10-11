using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Commands;

public class SetValidatorCommand : IUndoableCommand
{
    private readonly IRegion _region;
    private readonly IDataValidator _validator;

    public SetValidatorCommand(IRegion region, IDataValidator validator)
    {
        _region = region;
        _validator = validator;
    }

    public bool Execute(Sheet sheet)
    {
        sheet.Validation.Add(_validator, _region);
        sheet.ValidateRegion(_region);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Validation.Remove(_validator, _region);
        sheet.ValidateRegion(_region);
        return true;
    }
}