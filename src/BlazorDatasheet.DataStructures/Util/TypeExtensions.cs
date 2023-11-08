namespace BlazorDatasheet.DataStructures.Util;

public static class TypeExtensions
{
    public static bool IsNullable(this Type propType)
    {
        return System.Nullable.GetUnderlyingType(propType) != null;
    }

    public static bool IsNumeric(this Type propType)
    {
        var type = propType;
        if (propType.IsNullable())
            type = Nullable.GetUnderlyingType(propType);

        return type == typeof(int) || type == typeof(double) || type == typeof(decimal) || type == typeof(short) ||
               type == typeof(float) || type == typeof(long);
    }

    public static object? GetDefault(this Type t)
    {
        if (t.IsNullable())
            return null;
        Func<object> f = GetDefault<object>;
        return f.Method.GetGenericMethodDefinition().MakeGenericMethod(t).Invoke(null, null);
    }

    private static T GetDefault<T>()
    {
        return default(T);
    }
}