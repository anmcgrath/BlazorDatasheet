using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Serialization.Json.Models;

internal class WorkbookModel
{
    public List<CellFormat> Formats { get; set; } = new();
    public List<SheetModel> Sheets { get; set; } = new();

    public WorkbookModel()
    {
    }

    private static int GetOrAddFormatIndex(CellFormat format, List<CellFormat> formats)
    {
        var index = formats.FindIndex(f => f.Equals(format));
        if (index == -1)
        {
            formats.Add(format);
            index = formats.Count - 1;
        }

        return index;
    }

    public static WorkbookModel Create(Workbook workbook)
    {
        var model = new WorkbookModel();

        foreach (var sheet in workbook.Sheets)
        {
            var sheetModel = new SheetModel();
            sheetModel.NumRows = sheet.NumRows;
            sheetModel.NumCols = sheet.NumCols;

            foreach (var row in sheet.Rows.NonEmpty)
            {
                var rowModel = new RowModel();
                rowModel.RowIndex = row.RowIndex;
                if (Math.Abs(row.Height - sheet.Rows.DefaultSize) > 0.001)
                    rowModel.Height = row.Height;

                rowModel.FormatIndex = GetOrAddFormatIndex((CellFormat)row.Format, model.Formats);

                rowModel.Heading = row.Heading;
                foreach (var cell in row.NonEmptyCells)
                {
                    var cellModel = new CellModel();
                    cellModel.CellValue = cell.CellValue;
                    cellModel.Formula = cell.Formula;
                    cellModel.ColIndex = cell.Col;
                    rowModel.Cells.Add(cellModel);
                }

                sheetModel.Rows.Add(rowModel);
            }

            model.Sheets.Add(sheetModel);
        }

        return model;
    }

    public Workbook FromModel()
    {
        var workbook = new Workbook();
        foreach (var sheetModel in Sheets)
        {
            var sheet = new Sheet(sheetModel.NumRows, sheetModel.NumCols);
            sheet.ScreenUpdating = false;
            sheet.BatchUpdates();
            foreach (var rowModel in sheetModel.Rows)
            {
                if (rowModel.Heading != null)
                    sheet.Rows.SetHeadings(rowModel.RowIndex, rowModel.RowIndex, rowModel.Heading);
                if (rowModel.Height != null)
                    sheet.Rows.SetSize(rowModel.RowIndex, rowModel.Height.Value);
                foreach (var cellModel in rowModel.Cells)
                {
                    var cellValue = ToCellValue(cellModel);
                    if (!cellValue.IsEmpty)
                        sheet.Cells.SetValue(rowModel.RowIndex, cellModel.ColIndex, ToCellValue(cellModel));
                    if (cellModel.Formula != null)
                        sheet.Cells.SetFormula(rowModel.RowIndex, cellModel.ColIndex, cellModel.Formula);
                }
            }

            sheet.EndBatchUpdates();
            sheet.ScreenUpdating = true;
            workbook.AddSheet(sheet);
        }

        return workbook;
    }

    private CellValue ToCellValue(CellModel cellModel)
    {
        if (cellModel.CellValue.IsEmpty)
            return CellValue.Empty;

        switch (cellModel.CellValue.ValueType)
        {
            case CellValueType.Logical:
                return CellValue.Logical((bool)cellModel.CellValue.Data!);
            case CellValueType.Number:
                return CellValue.Number((double)cellModel.CellValue.Data!);
            case CellValueType.Text:
                return CellValue.Text((string)cellModel.CellValue.Data!);
            case CellValueType.Date:
                return CellValue.Date((DateTime)cellModel.CellValue.Data!);
            default:
                return CellValue.Empty;
        }
    }
}