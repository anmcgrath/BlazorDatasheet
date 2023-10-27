using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core;

public class RangeText
{
    public static Reference ParseRangePartAsReference(string rangeText)
    {
        if (IsValidCellReference(rangeText))
        {
            var cellReference = CellReference.FromString(rangeText);
            return cellReference;
        }

        if (IsValidColReference(rangeText))
            return new ColReference(CellReference.ColStrToNumber(rangeText),
                rangeText.StartsWith('$'));

        if (IsValidRowReference(rangeText))
            return new RowReference(CellReference.RowStrToNumber(rangeText), rangeText.StartsWith('$'));

        return new NamedReference(rangeText);
    }

    public static bool IsValidColReference(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '$' && i == 0)
                continue;
            if (!char.IsLetter(c))
                return false;
        }

        return true;
    }

    public static bool IsValidRowReference(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '$' && i == 0)
                continue;
            if (!char.IsDigit(c))
                return false;
        }

        return true;
    }
    
    public static bool IsValidCellReference(string text)
    {
        return CellReference.IsValid(text);
    }
}