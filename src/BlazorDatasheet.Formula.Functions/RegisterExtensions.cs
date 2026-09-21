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
        builder.Add(AbsFunction.Descriptor);
        builder.Add(AverageFunction.Descriptor);
        builder.Add(ColumnFunction.Descriptor);
        builder.Add(DegreesFunction.Descriptor);
        builder.Add(InterceptFunction.Descriptor);
        builder.Add(LogFunction.Descriptor);
        builder.Add(Log10Function.Descriptor);
        builder.Add(MaxFunction.Descriptor);
        builder.Add(MinFunction.Descriptor);
        builder.Add(ModFunction.Descriptor);
        builder.Add(PiFunction.Descriptor);
        builder.Add(SumFunction.Descriptor);
        builder.Add(SumSqFunction.Descriptor);
        builder.Add(SinFunction.Descriptor);
        builder.Add(SignFunction.Descriptor);
        builder.Add(SlopeFunction.Descriptor);
        builder.Add(SqrtFunction.Descriptor);
        builder.Add(PowerFunction.Descriptor);
        builder.Add(RadiansFunction.Descriptor);
        builder.Add(RandFunction.Descriptor);
        builder.Add(RoundFunction.Descriptor);
        builder.Add(RowFunction.Descriptor);
    }

    public static void RegisterLookupFunctions(this FunctionRegistryBuilder builder)
    {
        builder.Add(VLookupFunction.Descriptor);
    }
}
