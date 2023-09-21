using BlazorDatasheet.Commands;
using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class MergeCellsAndInsertColRowTests
{
    /*
     We need to test correct behaviour on inserting columns and rows into sheet with merged cells.
     After insert merged region should shift if inserted row is above it or inserted column is before 
     or inserted into merged region.
     
     Initial data

           0  1  2  3  4
       0 |  |  |  |  |  |
       1 |  |U |  |  |  |
       2 |  |  |M    |  |
       3 |  |  |     |  |
       4 |  |  |  |  |  |
     
     */
    [Test]
    public void Insert_Row_Above_Then_Undo_Correct()
    {
        var sheet = new Sheet(5, 5);
        sheet.TrySetCellValue(2, 2, "M");
        sheet.TrySetCellValue(1, 1, "U");

        sheet.MergeCells(sheet.Range(2, 3, 2, 3));

        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(3, 3));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(4, 4));

        Assert.AreEqual("M", sheet.GetValue(2, 2));
        Assert.AreEqual(null, sheet.GetValue(3, 3));
        Assert.AreEqual("U", sheet.GetValue(1, 1));

        sheet.InsertRowAfter(0);
        /*
               0  1  2  3  4
           0 |  |  |  |  |  |
           1 |  |  |  |  |  |
           2 |  |U |  |  |  |
           3 |  |  |M    |  |
           4 |  |  |     |  |
           5 |  |  |  |  |  |
         
         */

        Assert.True(sheet.IsPositionMerged(3, 2));
        Assert.True(sheet.IsPositionMerged(3, 3));
        Assert.True(sheet.IsPositionMerged(4, 2));
        Assert.True(sheet.IsPositionMerged(4, 3));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(2, 2));
        Assert.False(sheet.IsPositionMerged(5, 2));

        Assert.AreEqual("M", sheet.GetValue(3, 2));
        Assert.AreEqual(null, sheet.GetValue(4, 3));
        Assert.AreEqual("U", sheet.GetValue(2, 1));

        sheet.Commands.Undo();

        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(3, 3));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(4, 4));
    }

    [Test]
    public void Insert_Row_Into_Then_Undo_Correct()
    {
        var sheet = new Sheet(5, 5);
        sheet.TrySetCellValue(2, 2, "M");
        sheet.TrySetCellValue(1, 1, "U");

        sheet.MergeCells(sheet.Range(2, 3, 2, 3));

        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(3, 3));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(4, 4));

        Assert.AreEqual("M", sheet.GetValue(2, 2));
        Assert.AreEqual(null, sheet.GetValue(3, 3));
        Assert.AreEqual("U", sheet.GetValue(1, 1));

        sheet.InsertRowAfter(2);
        /*
               0  1  2  3  4
           0 |  |  |  |  |  |
           1 |  |U |  |  |  |
           2 |  |  |M    |  |
           3 |  |  |     |  |
           4 |  |  |     |  |
           5 |  |  |  |  |  |
         
         */

        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(3, 3));
        Assert.True(sheet.IsPositionMerged(4, 2));
        Assert.True(sheet.IsPositionMerged(4, 3));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(1, 2));
        Assert.False(sheet.IsPositionMerged(5, 2));

        Assert.AreEqual("M", sheet.GetValue(2, 2));
        Assert.AreEqual(null, sheet.GetValue(4, 3));
        Assert.AreEqual("U", sheet.GetValue(1, 1));
    }

    [Test]
    public void Insert_Column_Before_Then_Undo_Correct()
    {
        var sheet = new Sheet(5, 5);
        sheet.TrySetCellValue(2, 2, "M");
        sheet.TrySetCellValue(1, 1, "U");

        sheet.MergeCells(sheet.Range(2, 3, 2, 3));

        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(3, 3));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(4, 4));

        Assert.AreEqual("M", sheet.GetValue(2, 2));
        Assert.AreEqual(null, sheet.GetValue(3, 3));
        Assert.AreEqual("U", sheet.GetValue(1, 1));

        sheet.InsertColAfter(0);

        /*
               0  1  2  3  4  5
           0 |  |  |  |  |  |  |
           1 |  |U |  |  |  |  |
           2 |  |  |  |M    |  |
           3 |  |  |  |     |  |
           4 |  |  |  |  |  |  |
     
         */

        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(2, 4));
        Assert.True(sheet.IsPositionMerged(3, 3));
        Assert.True(sheet.IsPositionMerged(3, 4));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(2, 2));
        Assert.False(sheet.IsPositionMerged(1, 2));
        Assert.False(sheet.IsPositionMerged(4, 5));

        Assert.AreEqual("M", sheet.GetValue(2, 3));
        Assert.AreEqual(null, sheet.GetValue(2, 4));
        Assert.AreEqual("U", sheet.GetValue(1, 2));

        sheet.Commands.Undo();

        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(3, 3));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(4, 4));
    }

    [Test]
    public void Insert_Column_Into_Then_Undo_Correct()
    {
        var sheet = new Sheet(5, 5);
        sheet.TrySetCellValue(2, 2, "M");
        sheet.TrySetCellValue(1, 1, "U");

        sheet.MergeCells(sheet.Range(2, 3, 2, 3));

        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(3, 3));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(4, 4));

        Assert.AreEqual("M", sheet.GetValue(2, 2));
        Assert.AreEqual(null, sheet.GetValue(3, 3));
        Assert.AreEqual("U", sheet.GetValue(1, 1));

        sheet.InsertColAfter(2);

        /*
               0  1  2  3  4  5
           0 |  |  |  |  |  |  |
           1 |  |U |  |  |  |  |
           2 |  |  |M       |  |
           3 |  |  |        |  |
           4 |  |  |  |  |  |  |
     
         */

        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(2, 4));
        Assert.True(sheet.IsPositionMerged(3, 2));
        Assert.True(sheet.IsPositionMerged(3, 3));
        Assert.True(sheet.IsPositionMerged(3, 4));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(1, 1));
        Assert.False(sheet.IsPositionMerged(2, 1));
        Assert.False(sheet.IsPositionMerged(4, 5));

        Assert.AreEqual("M", sheet.GetValue(2, 2));
        Assert.AreEqual(null, sheet.GetValue(2, 4));
        Assert.AreEqual("U", sheet.GetValue(1, 1));
    }

    [Test]
    public void Remove_Column()
    {
        /*
               0  1  2  3  4  5
           0 |  |  |  |  |  |  |
           1 |  |U |  |  |  |  |
           2 |  |  |M       |  |
           3 |  |  |        |  |
           4 |  |  |  |  |  |  |
     
         */

        var sheet = new Sheet(5, 5);
        sheet.TrySetCellValue(2, 2, "M");
        sheet.TrySetCellValue(1, 1, "U");

        sheet.MergeCells(sheet.Range(2, 3, 2, 4));

        sheet.RemoveCol(3);

        /*
               0  1  2  3  4 
           0 |  |  |  |  |  |
           1 |  |U |  |  |  |
           2 |  |  |M    |  |
           3 |  |  |     |  |
           4 |  |  |  |  |  |
    
         */

        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(3, 2));
        Assert.True(sheet.IsPositionMerged(3, 3));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(1, 1));
        Assert.False(sheet.IsPositionMerged(2, 4));
        Assert.False(sheet.IsPositionMerged(4, 4));

        Assert.AreEqual("M", sheet.GetValue(2, 2));
        Assert.AreEqual(null, sheet.GetValue(2, 4));
        Assert.AreEqual("U", sheet.GetValue(1, 1));

        sheet.Commands.Undo();

        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(2, 4));
        Assert.True(sheet.IsPositionMerged(3, 2));
        Assert.True(sheet.IsPositionMerged(3, 3));
        Assert.True(sheet.IsPositionMerged(3, 4));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(1, 1));
        Assert.False(sheet.IsPositionMerged(2, 1));
        Assert.False(sheet.IsPositionMerged(4, 5));

        Assert.AreEqual("M", sheet.GetValue(2, 2));
        Assert.AreEqual(null, sheet.GetValue(2, 4));
        Assert.AreEqual("U", sheet.GetValue(1, 1));

        sheet.RemoveCol(0);

        /*
               0  1  2  3  4 
           0 |  |  |  |  |  |
           1 |U |  |  |  |  |
           2 |  |M       |  |
           3 |  |        |  |
           4 |  |  |  |  |  |
     
         */

        Assert.True(sheet.IsPositionMerged(2, 1));
        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(3, 1));
        Assert.True(sheet.IsPositionMerged(3, 2));
        Assert.True(sheet.IsPositionMerged(3, 3));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(1, 1));
        Assert.False(sheet.IsPositionMerged(2, 4));
        Assert.False(sheet.IsPositionMerged(4, 4));

        Assert.AreEqual("M", sheet.GetValue(2, 1));
        Assert.AreEqual(null, sheet.GetValue(2, 3));
        Assert.AreEqual("U", sheet.GetValue(1, 0));
    }

    [Test]
    public void Remove_Row()
    {
        /*
               0  1  2  3  4  5
           0 |  |  |  |  |  |  |
           1 |  |U |  |  |  |  |
           2 |  |  |M       |  |
           3 |  |  |        |  |
           4 |  |  |        |  |
           5 |  |  |  |  |  |  |
     
         */

        var sheet = new Sheet(5, 5);
        sheet.TrySetCellValue(2, 2, "M");
        sheet.TrySetCellValue(1, 1, "U");

        sheet.MergeCells(sheet.Range(2, 4, 2, 4));

        sheet.RemoveRow(3);

        /*
               0  1  2  3  4  5
           0 |  |  |  |  |  |  |
           1 |  |U |  |  |  |  |
           2 |  |  |M       |  |
           3 |  |  |        |  |
           4 |  |  |  |  |  |  |
     
         */

        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(2, 4));
        Assert.True(sheet.IsPositionMerged(3, 2));
        Assert.True(sheet.IsPositionMerged(3, 3));
        Assert.True(sheet.IsPositionMerged(3, 4));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(1, 1));
        Assert.False(sheet.IsPositionMerged(2, 5));
        Assert.False(sheet.IsPositionMerged(4, 4));

        Assert.AreEqual("M", sheet.GetValue(2, 2));
        Assert.AreEqual(null, sheet.GetValue(2, 3));
        Assert.AreEqual("U", sheet.GetValue(1, 1));

        sheet.Commands.Undo();

        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(2, 3));
        Assert.True(sheet.IsPositionMerged(2, 4));
        Assert.True(sheet.IsPositionMerged(4, 2));
        Assert.True(sheet.IsPositionMerged(4, 3));
        Assert.True(sheet.IsPositionMerged(4, 4));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(1, 1));
        Assert.False(sheet.IsPositionMerged(2, 5));
        Assert.False(sheet.IsPositionMerged(5, 5));

        sheet.RemoveRow(0);


        /*
               0  1  2  3  4  5
           0 |  |U |  |  |  |  |
           1 |  |  |M       |  |
           2 |  |  |        |  |
           3 |  |  |        |  |
           4 |  |  |  |  |  |  |
     
         */

        Assert.True(sheet.IsPositionMerged(1, 2));
        Assert.True(sheet.IsPositionMerged(1, 3));
        Assert.True(sheet.IsPositionMerged(1, 4));
        Assert.True(sheet.IsPositionMerged(3, 2));
        Assert.True(sheet.IsPositionMerged(3, 3));
        Assert.True(sheet.IsPositionMerged(3, 4));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(0, 3));
        Assert.False(sheet.IsPositionMerged(1, 5));
        Assert.False(sheet.IsPositionMerged(3, 5));
        Assert.False(sheet.IsPositionMerged(5, 5));

        Assert.AreEqual("M", sheet.GetValue(1, 2));
        Assert.AreEqual(null, sheet.GetValue(2, 3));
        Assert.AreEqual("U", sheet.GetValue(0, 1));
    }

    [Test]
    public void Unmerge_Column()
    {
        var sheet = new Sheet(5, 5);
        sheet.TrySetCellValue(2, 2, "M");
        sheet.TrySetCellValue(1, 1, "U");

        sheet.MergeCells(sheet.Range(2, 2, 2, 3));

        /*
                0  1  2  3  4
            0 |  |  |  |  |  |
            1 |  |U |  |  |  |
            2 |  |  |M    |  |
            3 |  |  |  |  |  |
            4 |  |  |  |  |  |
         */

        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(2, 3));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(4, 4));

        Assert.AreEqual("M", sheet.GetValue(2, 2));
        Assert.AreEqual(null, sheet.GetValue(3, 3));
        Assert.AreEqual("U", sheet.GetValue(1, 1));

        sheet.RemoveCol(3);
        /*
                0  1  2  3  4
            0 |  |  |  |  |  |
            1 |  |U |  |  |  |
            2 |  |  |M |  |  |
            3 |  |  |  |  |  |
            4 |  |  |  |  |  |
         */

        Assert.False(sheet.IsPositionMerged(2, 2));
        Assert.False(sheet.IsPositionMerged(2, 3));
    }

    [Test]
    public void Unmerge_Row()
    {
        var sheet = new Sheet(5, 5);
        sheet.TrySetCellValue(2, 2, "M");
        sheet.TrySetCellValue(1, 1, "U");

        sheet.MergeCells(sheet.Range(2, 3, 2, 2));

        /*
                0  1  2  3  4
            0 |  |  |  |  |  |
            1 |  |U |  |  |  |
            2 |  |  |M |  |  |
            3 |  |  |M*|  |  |
            4 |  |  |  |  |  |
         */

        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.True(sheet.IsPositionMerged(3, 2));

        Assert.False(sheet.IsPositionMerged(0, 0));
        Assert.False(sheet.IsPositionMerged(4, 4));

        Assert.AreEqual("M", sheet.GetValue(2, 2));
        Assert.AreEqual(null, sheet.GetValue(3, 3));
        Assert.AreEqual("U", sheet.GetValue(1, 1));

        sheet.RemoveRow(3);
        /*
                0  1  2  3  4
            0 |  |  |  |  |  |
            1 |  |U |  |  |  |
            2 |  |  |M |  |  |
            3 |  |  |  |  |  |
         */

        Assert.False(sheet.IsPositionMerged(2, 2));
        Assert.False(sheet.IsPositionMerged(3, 2));
    }

    [Test]
    public void Insert_Row_Inside_Merged_Column_Expands_Merge()
    {
        // This case tests when an entire column is merged
        // and a row is inserted inside the merge. The behaviour
        // should be the same as when inserting inside a smaller range
        var sheet = new Sheet(3, 3);
        sheet.TrySetCellValue(0, 1, "M");
        sheet.MergeCells(new ColumnRegion(1));
        Assert.AreEqual(sheet.GetValue(0, 1), "M");

        sheet.InsertRowAfter(0);
        var mergeRegion = sheet.GetMergedRegionAtPosition(0, 1);
        Assert.NotNull(mergeRegion);
        Assert.AreEqual(mergeRegion.GetType(), typeof(ColumnRegion));
    }

    [Test]
    public void Insert_Col_Inside_Merged_Row_Expands_Merge()
    {
        // This case tests when an entire row is merged
        // and a col is inserted inside the merge. The behaviour
        // should be the same as when inserting inside a smaller range
        var sheet = new Sheet(3, 3);
        sheet.TrySetCellValue(0, 1, "M");
        
        sheet.MergeCells(new RowRegion(1));

        Assert.AreEqual(sheet.GetValue(0, 1), "M");

        sheet.InsertColAfter(0);

        var mergeRowRegion = sheet.GetMergedRegionAtPosition(1, 0);
        Assert.NotNull(mergeRowRegion);
        Assert.AreEqual(mergeRowRegion.GetType(), typeof(RowRegion));

    }

    [Test]
    public void Insert_Col_Inside_Merged_Column_Shift_Merge()
    {
        // This case tests when an entire row is merged
        // and a col is inserted inside the merge. The behaviour
        // should be the same as when inserting inside a smaller range
        var sheet = new Sheet(3, 3);
        sheet.TrySetCellValue(0, 1, "M");

        sheet.MergeCells(new ColumnRegion(2));


        Assert.AreEqual(sheet.GetValue(0, 1), "M");

        sheet.InsertColAfter(0);


        var mergeColumnRegion = sheet.GetMergedRegionAtPosition(0, 3);
        Assert.NotNull(mergeColumnRegion);
        Assert.AreEqual(mergeColumnRegion.GetType(), typeof(ColumnRegion));
    }

    [Test]
    public void Insert_Col_Inside_Merged_Row_Shift_Merge()
    {
        // This case tests when an entire row is merged
        // and a col is inserted inside the merge. The behaviour
        // should be the same as when inserting inside a smaller range
        var sheet = new Sheet(3, 3);
        sheet.TrySetCellValue(0, 1, "M");

        sheet.MergeCells(new RowRegion(1));

        Assert.AreEqual(sheet.GetValue(0, 1), "M");

        sheet.InsertRowAfter(0);

        var mergeRowRegion = sheet.GetMergedRegionAtPosition(2, 0);
        Assert.NotNull(mergeRowRegion);
        Assert.AreEqual(mergeRowRegion.GetType(), typeof(RowRegion));
    }
}