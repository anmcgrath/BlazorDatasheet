using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.SharedPages.Extensions;

public static class SheetExtensions
{
    public static void FillRandomNumbers(this Sheet sheet, int min = 0, int max = 100)
    {
        var r = new Random();
        var data = new CellValue[sheet.NumRows][];
        for (int row = 0; row < sheet.NumRows; row++)
        {
            data[row] = new CellValue[sheet.NumCols];
            for (int col = 0; col < sheet.NumCols; col++)
            {
                data[row][col] = CellValue.Number(r.Next(min, max));
            }
        }

        sheet.BatchUpdates();
        sheet.Cells.SetValues(0, 0, data);
        sheet.EndBatchUpdates();
    }
}