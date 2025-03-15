using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Serialization.Json.Extensions;
using BlazorDatasheet.Serialization.Json.Models;

namespace BlazorDatasheet.Serialization.Json.Mappers;

internal class SheetMapper
{
    public static SheetModel FromSheet(Sheet sheet, List<CellFormat> formats)
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
                rowModel.FormatIndex = GetOrAddFormatIndex((CellFormat)row.Format, formats);

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
                columnModel.FormatIndex = GetOrAddFormatIndex(col.Format, formats);

            columnModel.Filters = col.Filters.ToList();

            sheetModel.Columns.Add(columnModel);
        }

        sheetModel.Merges = sheet.Cells.GetMerges(sheet.Region)
            .Select(x => new DataRegionModel<bool>(RangeText.RegionToText(x), true))
            .ToList();

        sheetModel.CellFormats = sheet.Cells.GetFormatData(sheet.Region)
            .ToDataRegionModelList(cf => GetOrAddFormatIndex(cf, formats));

        sheetModel.ConditionalFormats = sheet.ConditionalFormats.GetAllFormats()
            .Select(x => new ConditionalFormatModel()
            {
                RegionString = RangeText.RegionToText(x.Region),
                RuleType = x.Data.GetType().Name,
                Rule = x.Data
            })
            .ToList();

        sheetModel.Types = sheet.Cells.GetTypeData().ToDataRegionModelList(x => x);
        sheetModel.Validators = sheet.Validators.GetAll().ToDataRegionModelList(x => x);

        return sheetModel;
    }

    public static Sheet FromModel(SheetModel sheetModel, List<CellFormat> formats)
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

            if (rowModel.FormatIndex != null && rowModel.FormatIndex < formats.Count &&
                !formats[rowModel.FormatIndex.Value].IsDefaultFormat())
                sheet.SetFormat(new RowRegion(rowModel.RowIndex), formats[rowModel.FormatIndex.Value]);

            if (rowModel.Hidden)
                sheet.Rows.Hide(rowModel.RowIndex, 1);

            foreach (var cellModel in rowModel.Cells)
            {
                if (!cellModel.CellValue.IsEmpty)
                    sheet.Cells.SetValue(rowModel.RowIndex, cellModel.ColIndex, cellModel.CellValue);
                if (cellModel.Formula != null)
                    sheet.Cells.SetFormula(rowModel.RowIndex, cellModel.ColIndex, cellModel.Formula);
                foreach (var kp in cellModel.MetaData)
                {
                    sheet.Cells.SetCellMetaData(rowModel.RowIndex, cellModel.ColIndex, kp.Key, kp.Value);
                }
            }
        }

        foreach (var merge in sheetModel.Merges)
        {
            sheet.Range(merge.RegionString)!.Merge();
        }

        foreach (var cellFormat in sheetModel.CellFormats)
        {
            if (cellFormat.Value >= formats.Count)
                continue;
            sheet.Range(cellFormat.RegionString)!.Format = formats[cellFormat.Value];
        }

        foreach (var cf in sheetModel.ConditionalFormats)
        {
            sheet.ConditionalFormats.Apply(sheet.Range(cf.RegionString), cf.Rule);
        }

        foreach (var type in sheetModel.Types)
        {
            sheet.Range(type.RegionString)!.Type = type.Value;
        }

        foreach (var validator in sheetModel.Validators)
            sheet.Range(validator.RegionString)!.AddValidator(validator.Value);

        foreach (var colModel in sheetModel.Columns)
        {
            if (colModel.Heading != null)
                sheet.Columns.SetHeadings(colModel.ColIndex, colModel.ColIndex, colModel.Heading);
            if (colModel.Width != null)
                sheet.Columns.SetSize(colModel.ColIndex, colModel.Width.Value);
            if (colModel.FormatIndex != null && colModel.FormatIndex < formats.Count &&
                !formats[colModel.FormatIndex.Value].IsDefaultFormat())
                sheet.SetFormat(new ColumnRegion(colModel.ColIndex, colModel.ColIndex),
                    formats[colModel.FormatIndex.Value]);
            if (colModel.Hidden)
                sheet.Columns.Hide(colModel.ColIndex, 1);

            sheet.Columns.Filters.Set(colModel.ColIndex, colModel.Filters);
        }

        sheet.EndBatchUpdates();
        sheet.ScreenUpdating = true;

        return sheet;
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
}