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

    public static Workbook FromModel(WorkbookModel workbookModel, FormulaOptions? formulaOptions)
    {
        var workbook = new Workbook(formulaOptions);
        var sheets = new List<(SheetModel Model, Sheet Sheet)>();

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
                workbook.GetFormulaEngine().SetVariable(variable.Name, variable.Formula);
            else if (!variable.Value.IsEmpty)
                workbook.GetFormulaEngine().SetVariable(variable.Name, variable.Value);
        }

        workbook.GetFormulaEngine().CalculateSheet(true);

        return workbook;
    }
}
