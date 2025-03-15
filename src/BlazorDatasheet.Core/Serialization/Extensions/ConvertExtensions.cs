using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Core.Serialization.Json.Models;

namespace BlazorDatasheet.Core.Serialization.Json.Extensions;

internal static class ConvertExtensions
{
    public static List<Models.DataRegionModel<T>> ToDataRegionCollection<T>(this RegionDataStore<T> store)
        where T : IEquatable<T>
    {
        return store.GetAllDataRegions()
            .Select(x => new Models.DataRegionModel<T>(RangeText.RegionToText(x.Region), x.Data))
            .ToList();
    }

    public static List<DataRegionModel<T>> ToDataRegionModelList<T, S>(this IEnumerable<DataRegion<S>> dataRegions,
        Func<S, T> func)
    {
        return dataRegions.Select(x => new DataRegionModel<T>(RangeText.RegionToText(x.Region), func(x.Data)))
            .ToList();
    }
}