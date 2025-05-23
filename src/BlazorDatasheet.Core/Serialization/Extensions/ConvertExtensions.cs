using BlazorDatasheet.Core.Serialization.Models;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Serialization.Extensions;

internal static class ConvertExtensions
{
    public static List<DataRegionModel<T>> ToDataRegionCollection<T>(this RegionDataStore<T> store)
        where T : IEquatable<T>
    {
        return store.GetAllDataRegions()
            .Select(x => new DataRegionModel<T>(RangeText.RegionToText(x.Region), x.Data))
            .ToList();
    }

    public static List<DataRegionModel<T>> ToDataRegionModelList<T, TS>(this IEnumerable<DataRegion<TS>> dataRegions,
        Func<TS, T> func)
    {
        return dataRegions.Select(x => new DataRegionModel<T>(RangeText.RegionToText(x.Region), func(x.Data)))
            .ToList();
    }
}