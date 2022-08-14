using System.Reflection;

namespace BlazorDatasheet.Model;

public class Cell
{
    public string Type { get; set; } = "text";

    public string? Value
    {
        get
        {
            if (String.IsNullOrEmpty(Key))
                return Data?.ToString();
            return Data?.GetType().GetProperty(Key)?.GetValue(Data, null)?.ToString();
        }
        set
        {
            if (String.IsNullOrEmpty(Key))
            {
                if (Setter == null)
                {
                    Data = value;
                    return;
                }

                Setter.Invoke(Data, value);
                return;
            }

            var prop = Data.GetType().GetProperty(Key);
            if (prop == null)
                return;

            if (Setter == null)
            {
                var propType = prop.PropertyType;
                try
                {
                    // Convert.ChangeType won't convert string to nullable type
                    if (System.Nullable.GetUnderlyingType(propType) != null)
                    {
                        if (String.IsNullOrEmpty(value))
                        {
                            prop.SetValue(Data, null);
                            return;
                        }

                        propType = System.Nullable.GetUnderlyingType(propType);
                    }
                    
                    object convertedValue = Convert.ChangeType(value, propType);
                    prop.SetValue(Data, convertedValue);
                }
                catch (Exception e)
                {
                    return;
                }
            }

            else
                Setter.Invoke(Data, value);
        }
    }

    public Action<object, string>? Setter { get; set; }

    public string? Key { get; set; }

    public object Data { get; set; }
    public string Format { get; set; } = "";
}