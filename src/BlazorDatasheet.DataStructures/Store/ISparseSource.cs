namespace BlazorDatasheet.DataStructures.Store;

public interface ISparseSource
{
    /// <summary>
    /// Returns the next non-empty index after the index specified, for the main axis invovled.. Returns -1 if no non-empty indices exist after the given index.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public int GetNextNonEmptyIndex(int index);
}