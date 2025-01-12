using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Dependencies;

namespace BlazorDatasheet.Core.FormulaEngine;

internal class DeleteFormulaCommand : RegionCommand
{
    private FormulaEngineRestoreData _restoreData = new();

    public DeleteFormulaCommand(IRegion region) : base(region)
    {
    }

    protected override bool DoExecute(Sheet sheet)
    {
        return true;
    }

    public override bool CanExecute(Sheet sheet) => true;

    protected override bool DoUndo(Sheet sheet)
    {
        return true;
    }
}