namespace BlazorDatasheet.Render.Layers.Preview;

/// <summary>
/// Centralised store for <see cref="PreviewItem"/> overlays. Components such as autofill
/// and column resize post items here; each pane's <see cref="PreviewLayer"/> subscribes
/// to <see cref="Changed"/> and renders the items clipped to its own view region.
/// </summary>
public class PreviewService
{
    private readonly List<PreviewItem> _items = new();

    /// <summary>
    /// Raised whenever the item list is modified (add, remove, or clear).
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// The current set of preview items to render.
    /// </summary>
    public IReadOnlyList<PreviewItem> Items => _items;

    /// <summary>
    /// Adds a preview item and raises <see cref="Changed"/>.
    /// </summary>
    public void Add(PreviewItem item)
    {
        _items.Add(item);
        Changed?.Invoke();
    }

    /// <summary>
    /// Removes a preview item by reference. Raises <see cref="Changed"/> only if the item was found.
    /// </summary>
    public void Remove(PreviewItem item)
    {
        if (_items.Remove(item))
            Changed?.Invoke();
    }

    /// <summary>
    /// Atomically replaces an existing preview item with a new one, raising <see cref="Changed"/> once.
    /// If the old item is not found, the new item is simply added.
    /// </summary>
    public void Update(PreviewItem? old, PreviewItem replacement)
    {
        if (old != null)
            _items.Remove(old);
        _items.Add(replacement);
        Changed?.Invoke();
    }

    /// <summary>
    /// Removes all preview items. Raises <see cref="Changed"/> only if the list was non-empty.
    /// </summary>
    public void Clear()
    {
        if (_items.Count == 0)
            return;

        _items.Clear();
        Changed?.Invoke();
    }
}
