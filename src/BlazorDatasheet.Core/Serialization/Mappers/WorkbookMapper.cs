using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Serialization.Models;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.References;

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

        // Deleted ranges are kept: a reader that finds a key missing may treat it as never tracked.
        foreach (var range in workbook.TrackedRanges.GetAll())
        {
            workbookModel.TrackedRanges.Add(new TrackedRangeModel
            {
                Key = range.Key,
                Sheet = range.SheetName,
                RegionString = RangeText.RegionToText(range.Region),
                Deleted = range.IsDeleted
            });
        }

        // Deleted and detached tables are kept, as tracked ranges are. Column names are not written: they are read
        // again from the header cells or column headings when the workbook is loaded.
        foreach (var table in workbook.Tables.GetAll())
        {
            workbookModel.Tables.Add(new TableModel
            {
                Name = table.Name,
                Sheet = table.SheetName,
                Region = table.IsSheetTable || table.Region == null ? null : RangeText.RegionToText(table.Region),
                IsSheetTable = table.IsSheetTable,
                ShowTotals = table.ShowTotals,
                Deleted = table.IsDeleted
            });
        }

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

            // After the cells, so that column names are read from the header cells, and before the variables, which
            // may refer to tables. Formulas parsed before their tables existed are then resolved again.
            if (workbookModel.Tables is { Count: > 0 } tables)
            {
                foreach (var table in tables)
                {
                    var region = table.IsSheetTable ? null : ParseRegion(workbook, table.Region);
                    if (!table.IsSheetTable && region == null)
                        continue;
                    workbook.Tables.Load(table.Name, table.Sheet, region, table.IsSheetTable, table.ShowTotals,
                        table.Deleted);
                }

                engine.RefreshTableReferences(null);
            }

            foreach (var variable in workbookModel.Variables)
            {
                if (variable.Formula != null)
                    engine.SetVariable(variable.Name, variable.Formula);
                else if (!variable.Value.IsEmpty)
                    engine.SetVariable(variable.Name, variable.Value);
            }

            foreach (var range in workbookModel.TrackedRanges)
            {
                if (ParseRegion(workbook, range.RegionString) is { } region)
                    workbook.TrackedRanges.Track(range.Key, range.Sheet, region, range.Deleted);
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

    private static IRegion? ParseRegion(Workbook workbook, string? regionString)
    {
        if (string.IsNullOrEmpty(regionString))
            return null;

        var engine = workbook.GetFormulaEngine();
        var value = engine.EvaluateFormula(engine.ParseFormula($"={regionString}", string.Empty, true),
            resolveReferences: false);

        return value.ValueType == CellValueType.Reference
            ? value.GetValue<Reference>()?.Region
            : null;
    }
}
