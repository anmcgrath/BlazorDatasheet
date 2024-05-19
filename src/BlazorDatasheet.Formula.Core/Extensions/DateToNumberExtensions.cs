namespace BlazorDatasheet.Formula.Core.Extensions;

public static class DateToNumberExtensions
{
    public static double ToNumber(this DateTime date)
    {
        var epoch = new DateTime(1900, 1, 1);
        return (date - epoch).Days;
    }

    public static DateTime ToDate(this double number)
    {
        var epoch = new DateTime(1900, 1, 1);
        return epoch.AddDays(number);
    }
}