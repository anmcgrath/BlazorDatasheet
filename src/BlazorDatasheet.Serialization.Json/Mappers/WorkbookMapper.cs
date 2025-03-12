using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Serialization.Json.Models;

namespace BlazorDatasheet.Serialization.Json.Mappers;

internal class WorkbookMapper
{
    public static WorkbookModel FromWorkbook(Workbook workbook)
    {
        var workbookModel = new WorkbookModel();

        foreach (var sheet in workbook.Sheets)
        {
            workbookModel.Sheets.Add(SheetMapper.FromSheet(sheet, workbookModel.Formats));
        }

        return workbookModel;
    }

    public static Workbook FromModel(WorkbookModel workbookModel)
    {
        var workbook = new Workbook();
        foreach (var sheetModel in workbookModel.Sheets)
        {
            workbook.AddSheet(SheetMapper.FromModel(sheetModel, workbookModel.Formats));
        }

        return workbook;
    }
}