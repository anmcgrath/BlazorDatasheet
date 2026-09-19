using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Serialization.Models;
using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Core.Serialization.Json.Mappers;

internal class WorkbookMapper
{
    public static WorkbookModel FromWorkbook(Workbook workbook, Action<string>? onWarning = null)
    {
        var workbookModel = new WorkbookModel();

        foreach (var sheet in workbook.Sheets)
        {
            workbookModel.Sheets.Add(SheetMapper.FromSheet(sheet, workbookModel.Formats, onWarning));
        }


        foreach (var namedVariable in workbook.GetFormulaEngine().GetVariables())
            workbookModel.Variables.Add(namedVariable);

        return workbookModel;
    }

    /// <param name="calculate">
    /// When false the workbook is loaded without evaluating any formula - neither the per-sheet
    /// passes that each sheet's batched cell changes would trigger, nor the full pass at the end.
    /// The caller must then run <c>workbook.GetFormulaEngine().CalculateSheet(true)</c> itself
    /// before reading any value that comes from a formula.
    /// </param>
    public static Workbook FromModel(WorkbookModel workbookModel, FormulaOptions? formulaOptions,
        bool calculate = true)
    {
        var workbook = new Workbook(formulaOptions);
        var engine = workbook.GetFormulaEngine();
        var sheets = new List<(SheetModel Model, Sheet Sheet)>();

        if (!calculate)
            engine.PauseCalculation();

        try
        {
            foreach (var sheetModel in workbookModel.Sheets)
            {
                var sheet = new Sheet(sheetModel.NumRows, sheetModel.NumCols, sheetModel.DefaultWidth,
                    sheetModel.DefaultHeight, workbook);
                workbook.AddSheet(sheetModel.Name, sheet);
                sheets.Add((sheetModel, sheet));
            }

            foreach (var (sheetModel, sheet) in sheets)
                SheetMapper.PopulateFromModel(sheetModel, workbookModel.Formats, sheet);

            foreach (var variable in workbookModel.Variables)
            {
                if (variable.Formula != null)
                    engine.SetVariable(variable.Name, variable.Formula);
                else if (!variable.Value.IsEmpty)
                    engine.SetVariable(variable.Name, variable.Value);
            }
        }
        finally
        {
            if (!calculate)
                engine.ResumeCalculation();
        }

        if (calculate)
            engine.CalculateSheet(true);

        return workbook;
    }
}
