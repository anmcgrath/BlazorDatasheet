﻿using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Formula.Core.Interpreter.References;

public class CellReference : Reference
{
    public int RowIndex { get; private set; }
    public int ColIndex { get; private set; }
    public bool IsColFixed { get; }
    public bool IsRowFixed { get; }

    public CellReference(int rowIndex, int colIndex, bool isColFixed, bool isRowFixed)
    {
        RowIndex = rowIndex;
        ColIndex = colIndex;
        IsColFixed = isColFixed;
        IsRowFixed = isRowFixed;
        Region = new Region(rowIndex, colIndex);
    }

    public override ReferenceKind Kind { get; }

    public override string ToAddressText()
    {
        return RangeText.ToRegionText(Region, IsColFixed, IsColFixed, IsRowFixed, IsRowFixed);
    }

    public override bool SameAs(Reference reference)
    {
        if (reference.Kind == ReferenceKind.Cell)
        {
            var cellRef = (CellReference)reference;
            return ColIndex == cellRef.ColIndex &&
                   RowIndex == cellRef.RowIndex;
        }

        if (reference.Kind == ReferenceKind.Range)
        {
            var rangeRef = (RangeReference)reference;
            return rangeRef.Region.Equals(this.Region);
        }

        return false;
    }

    public override void Shift(int offsetRow, int offsetCol)
    {
        var dRow = IsRowFixed ? 0 : offsetRow;
        var dCol = IsColFixed ? 0 : offsetCol;
        ColIndex += dCol;
        RowIndex += dRow;
        Region.Shift(dRow, dCol);
    }

    public override bool IsInvalid { get; protected set; }
    public sealed override IRegion Region { get; protected set; }

    internal override void SetRegion(IRegion region)
    {
        ColIndex = Region.Left;
        RowIndex = Region.Top;
        Region = region;
    }

    public override string ToString() => ToAddressText();
}