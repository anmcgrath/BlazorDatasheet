using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Serialization.Json.Models;

internal class WorkbookModel
{
    public List<CellFormat> Formats { get; set; } = new();
    public List<SheetModel> Sheets { get; set; } = new();

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

                if (!row.IsVisible)
                    rowModel.Hidden = !row.IsVisible;

                if (!row.Format.IsDefaultFormat())
                    rowModel.FormatIndex = GetOrAddFormatIndex((CellFormat)row.Format, model.Formats);

                rowModel.Heading = row.Heading;
                foreach (var cell in row.NonEmptyCells)
                {
                    var cellModel = new CellModel
                    {
                        CellValue = cell.CellValue,
                        Formula = cell.Formula,
                        ColIndex = cell.Col,
                        MetaData = cell.MetaData.ToDictionary(x => x.Key, x => x.Value)
                    };

                    rowModel.Cells.Add(cellModel);
                }

                sheetModel.Rows.Add(rowModel);
            }

            foreach (var col in sheet.Columns.NonEmpty)
            {
                var columnModel = new ColumnModel
                {
                    ColIndex = col.ColIndex
                };

                if (Math.Abs(col.Width - sheet.Columns.DefaultSize) > 0.001)
                    columnModel.Width = col.Width;

                if (!string.IsNullOrEmpty(col.Heading))
                    columnModel.Heading = col.Heading;

                if (!col.Visible)
                    columnModel.Hidden = !col.Visible;

                if (col.Format?.IsDefaultFormat() == false)
                    columnModel.FormatIndex = GetOrAddFormatIndex(col.Format, model.Formats);

                sheetModel.Columns.Add(columnModel);
            }

            sheetModel.Merges = sheet.Cells.GetMerges(sheet.Region)
                .Select(x => new DataRegion<bool>(RangeText.RegionToText(x), true))
                .ToList();

            sheetModel.CellFormats = sheet.Cells.GetFormatData(sheet.Region)
                .Select(x =>
                    new DataRegion<int>(RangeText.RegionToText(x.Region), GetOrAddFormatIndex(x.Data, model.Formats)))
                .ToList();

            sheetModel.ConditionalFormats = sheet.ConditionalFormats.GetAllFormats()
                .Select(x => new ConditionalFormatModel()
                {
                    RegionString = RangeText.RegionToText(x.Region),
                    Rule = x.Data
                })
                .ToList();

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

                if (rowModel.FormatIndex != null && rowModel.FormatIndex < Formats.Count &&
                    !Formats[rowModel.FormatIndex.Value].IsDefaultFormat())
                    sheet.SetFormat(new RowRegion(rowModel.RowIndex), Formats[rowModel.FormatIndex.Value]);

                if (rowModel.Hidden)
                    sheet.Rows.Hide(rowModel.RowIndex, 1);

                foreach (var cellModel in rowModel.Cells)
                {
                    var cellValue = ToCellValue(cellModel);
                    if (!cellValue.IsEmpty)
                        sheet.Cells.SetValue(rowModel.RowIndex, cellModel.ColIndex, ToCellValue(cellModel));
                    if (cellModel.Formula != null)
                        sheet.Cells.SetFormula(rowModel.RowIndex, cellModel.ColIndex, cellModel.Formula);
                    foreach (var kp in cellModel.MetaData)
                    {
                        sheet.Cells.SetCellMetaData(rowModel.RowIndex, cellModel.ColIndex, kp.Key, kp.Value);
                    }
                }
            }

            foreach (var colModel in sheetModel.Columns)
            {
                if (colModel.Heading != null)
                    sheet.Columns.SetHeadings(colModel.ColIndex, colModel.ColIndex, colModel.Heading);
                if (colModel.Width != null)
                    sheet.Columns.SetSize(colModel.ColIndex, colModel.Width.Value);
                if (colModel.FormatIndex < Formats.Count && !Formats[colModel.FormatIndex].IsDefaultFormat())
                    sheet.SetFormat(new ColumnRegion(colModel.ColIndex, colModel.ColIndex),
                        Formats[colModel.FormatIndex]);
                if (colModel.Hidden)
                    sheet.Columns.Hide(colModel.ColIndex, 1);
            }

            foreach (var merge in sheetModel.Merges)
            {
                sheet.Range(merge.RegionString)!.Merge();
            }

            foreach (var cellFormat in sheetModel.CellFormats)
            {
                if (cellFormat.Value >= Formats.Count)
                    continue;
                sheet.Range(cellFormat.RegionString)!.Format = Formats[cellFormat.Value];
            }

            foreach (var cf in sheetModel.ConditionalFormats)
            {
                sheet.ConditionalFormats.Apply(sheet.Range(cf.RegionString), cf.Rule);
            }

            sheet.EndBatchUpdates();
            sheet.ScreenUpdating = true;
            workbook.AddSheet(sheet);
        }

        return workbook;
    }

    private CellValue ToCellValue(CellModel cellModel)
    {
        return cellModel.CellValue;
    }
}