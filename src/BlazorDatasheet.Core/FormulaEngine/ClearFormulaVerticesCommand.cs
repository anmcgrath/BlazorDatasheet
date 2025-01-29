using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Dependencies;

namespace BlazorDatasheet.Core.FormulaEngine;

internal class ClearFormulaVerticesCommand : BaseCommand, IUndoableCommand
{
    public IRegion Region { get; }
    private FormulaEngineRestoreData _restoreData = new();
    private List<FormulaVertex> _removedVertices = new();

    public ClearFormulaVerticesCommand(IRegion region)
    {
        Region = region;
    }

    public override bool Execute(Sheet sheet)
    {
        var formulaEngine = sheet.FormulaEngine;
        _removedVertices = formulaEngine.GetVerticesInRegion(Region);
        formulaEngine.PauseCalculating = true;
        foreach (var vertex in _removedVertices)
        {
            formulaEngine.RemoveFormulaVertex(vertex);
        }

        formulaEngine.PauseCalculating = false;
        return true;
    }

    public override bool CanExecute(Sheet sheet)
    {
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.FormulaEngine.PauseCalculating = true;
        foreach (var vertex in _removedVertices)
        {
            sheet.FormulaEngine.AddFormulaVertex(vertex);
        }

        sheet.FormulaEngine.PauseCalculating = false;
        return true;
    }
}