using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Formatting;

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
        sheet.Validators.AddImpl(_validator, _region);
        sheet.Cells.ValidateRegion(_region);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Validators.Clear(_validator, _region);
        sheet.Cells.ValidateRegion(_region);
        return true;
    }
}