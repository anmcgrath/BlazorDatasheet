using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Functions.Logical;
using BlazorDatashet.Formula.Functions.Lookup;
using BlazorDatashet.Formula.Functions.Math;

namespace BlazorDatashet.Formula.Functions;

public static class RegisterExtensions
{
    public static void RegisterLogicalFunctions(this FunctionRegistryBuilder builder)
    {
        builder.Add(AndFunction.Descriptor);
        builder.Add(IfFunction.Descriptor);
        builder.Add(OrFunction.Descriptor);
        builder.Add(NotFunction.Descriptor);
    }

    public static void RegisterMathFunctions(this FunctionRegistryBuilder builder)
    {
        builder.Add(AverageFunction.Descriptor);
        builder.Add(ColumnFunction.Descriptor);
        builder.Add(InterceptFunction.Descriptor);
        builder.Add(SumFunction.Descriptor);
        builder.Add(SinFunction.Descriptor);
        builder.Add(SlopeFunction.Descriptor);
        builder.Add(PowerFunction.Descriptor);
        builder.Add(RandFunction.Descriptor);
        builder.Add(RowFunction.Descriptor);
    }

    public static void RegisterLookupFunctions(this FunctionRegistryBuilder builder)
    {
        builder.Add(VLookupFunction.Descriptor);
    }
}
