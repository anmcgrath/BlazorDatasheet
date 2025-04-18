namespace BlazorDatasheet.DataStructures.RTree;

public partial class RTree<T>
{
    /// <summary>
    /// A node in an R-tree data structure containing other nodes
    /// or elements of type <typeparamref name="T"/>.
    /// </summary>
    public class Node : ISpatialData
    {
        private Envelope _envelope;

        internal Node(List<ISpatialData> items, int height)
        {
            Height = height;
            Items = items;
            ResetEnvelope();
        }

        internal void Add(ISpatialData node)
        {
            Items.Add(node);
            _envelope = Envelope.Extend(node.Envelope);
        }

        internal void Remove(ISpatialData node)
        {
            _ = Items.Remove(node);
            ResetEnvelope();
        }

        internal void RemoveRange(int index, int count)
        {
            Items.RemoveRange(index, count);
            ResetEnvelope();
        }

        internal void ResetEnvelope()
        {
            _envelope = GetEnclosingEnvelope(Items);
        }

        internal readonly List<ISpatialData> Items;

        /// <summary>
        /// The descendent nodes or elements of a <see cref="Node"/>
        /// </summary>
        public IReadOnlyList<ISpatialData> Children => Items;

        /// <summary>
        /// The current height of a <see cref="Node"/>. 
        /// </summary>
        /// <remarks>
        /// A node containing individual elements has a <see cref="Height"/> of 1.
        /// </remarks>
        public int Height { get; }

        /// <summary>
        /// Determines whether the current <see cref="Node"/> is a leaf node.
        /// </summary>
        public bool IsLeaf => Height == 1;

        /// <summary>
        /// Gets the bounding box of all of the descendents of the 
        /// current <see cref="Node"/>.
        /// </summary>
        public ref readonly Envelope Envelope => ref _envelope;
    }
}