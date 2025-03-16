using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Serialization.Json.Models;

namespace BlazorDatasheet.Core.Serialization.Json.Mappers;

internal class WorkbookMapper
{
    public static WorkbookModel FromWorkbook(Workbook workbook)
    {
        var workbookModel = new WorkbookModel();

        foreach (var sheet in workbook.Sheets)
        {
            workbookModel.Sheets.Add(SheetMapper.FromSheet(sheet, workbookModel.Formats));
        }


        foreach (var namedVariable in workbook.GetFormulaEngine().GetVariables())
            workbookModel.Variables.Add(namedVariable);

        return workbookModel;
    }

    public static Workbook FromModel(WorkbookModel workbookModel)
    {
        var workbook = new Workbook();
        foreach (var sheetModel in workbookModel.Sheets)
        {
            workbook.AddSheet(sheetModel.Name, SheetMapper.FromModel(sheetModel, workbookModel.Formats));
        }


        foreach (var variable in workbookModel.Variables)
        {
            if (variable.Formula != null)
                workbook.GetFormulaEngine().SetVariable(variable.Name, variable.Formula);
            else if (variable.Value != null)
                workbook.GetFormulaEngine().SetVariable(variable.Name, variable.Value);
        }

        return workbook;
    }
}