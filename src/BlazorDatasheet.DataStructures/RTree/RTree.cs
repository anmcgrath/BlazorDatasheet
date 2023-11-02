// MIT License
//
// Copyright (c) 2017 viceroypenguin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// 	of this software and associated documentation files (the "Software"), to deal
// 	in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
// 	furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// 	copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// 	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// 	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// 	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// 	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

// This file contains code from https://github.com/viceroypenguin/RBush and has been slightly
// modified to suit the blazor datasheet project.

using System.Diagnostics.CodeAnalysis;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Util;

namespace BlazorDatasheet.DataStructures.RTree;

/// <summary>
/// An implementation of the R-tree data structure for 2-d spatial indexing.
/// </summary>
/// <typeparam name="T">The type of elements in the index.</typeparam>
public partial class RTree<T> : ISpatialDatabase<T>, ISpatialIndex<T> where T : ISpatialData
{
    private const int DefaultMaxEntries = 9;
    private const int MinimumMaxEntries = 4;
    private const int MinimumMinEntries = 2;
    private const double DefaultFillFactor = 0.4;

    private readonly IEqualityComparer<T> _comparer;
    private readonly int _maxEntries;
    private readonly int _minEntries;

    /// <summary>
    /// The root of the R-tree.
    /// </summary>
    public Node Root { get; private set; }

    /// <summary>
    /// The bounding box of all elements currently in the data structure.
    /// </summary>
    public ref readonly Envelope Envelope => ref Root.Envelope;

    /// <summary>
    /// Initializes a new instance of the <see cref="RTree{T}"/> that is
    /// empty and has the default tree width and default <see cref="IEqualityComparer{T}"/>.
    /// </summary>
    public RTree()
        : this(DefaultMaxEntries, EqualityComparer<T>.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RTree{T}"/> that is
    /// empty and has a custom max number of elements per tree node
    /// and default <see cref="IEqualityComparer{T}"/>.
    /// </summary>
    /// <param name="maxEntries"></param>
    public RTree(int maxEntries)
        : this(maxEntries, EqualityComparer<T>.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RTree{T}"/> that is
    /// empty and has a custom max number of elements per tree node
    /// and a custom <see cref="IEqualityComparer{T}"/>.
    /// </summary>
    /// <param name="maxEntries"></param>
    /// <param name="comparer"></param>
    public RTree(int maxEntries, IEqualityComparer<T> comparer)
    {
        this._comparer = comparer;
        this._maxEntries = Math.Max(MinimumMaxEntries, maxEntries);
        this._minEntries = Math.Max(MinimumMinEntries, (int)Math.Ceiling(this._maxEntries * DefaultFillFactor));

        this.Clear();
    }

    /// <summary>
    /// Gets the number of items currently stored in the <see cref="RTree{T}"/>
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Removes all elements from the <see cref="RTree{T}"/>.
    /// </summary>
    [MemberNotNull(nameof(Root))]
    public void Clear()
    {
        this.Root = new Node(new List<ISpatialData>(), 1);
        this.Count = 0;
    }

    /// <summary>
    /// Get all of the elements within the current <see cref="RTree{T}"/>.
    /// </summary>
    /// <returns>
    /// A list of every element contained in the <see cref="RTree{T}"/>.
    /// </returns>
    public IReadOnlyList<T> Search() =>
        GetAllChildren(new List<T>(), this.Root);

    public IReadOnlyList<T> Search(params Envelope[] boundingBoxes)
    {
        var children = new List<T>();
        foreach (var box in boundingBoxes)
            children.AddRange(Search(box));
        return children;
    }

    /// <summary>
    /// Get all of the elements from this <see cref="RTree{T}"/>
    /// within the <paramref name="boundingBox"/> bounding box.
    /// </summary>
    /// <param name="boundingBox">The area for which to find elements.</param>
    /// <returns>
    /// A list of the points that are within the bounding box
    /// from this <see cref="RTree{T}"/>.
    /// </returns>
    public IReadOnlyList<T> Search(in Envelope boundingBox) =>
        DoSearch(boundingBox);

    /// <summary>
    /// Adds an object to the <see cref="RTree{T}"/>
    /// </summary>
    /// <param name="item">
    /// The object to be added to <see cref="RTree{T}"/>.
    /// </param>
    public void Insert(T item)
    {
        Insert(item, this.Root.Height);
        this.Count++;
    }

    /// <summary>
    /// Adds all of the elements from the collection to the <see cref="RTree{T}"/>.
    /// </summary>
    /// <param name="items">
    /// A collection of items to add to the <see cref="RTree{T}"/>.
    /// </param>
    /// <remarks>
    /// For multiple items, this method is more performant than 
    /// adding items individually via <see cref="Insert(T)"/>.
    /// </remarks>
    public void BulkLoad(IEnumerable<T> items)
    {
        var data = items.ToArray();
        if (data.Length == 0) return;

        if (this.Root.IsLeaf &&
            this.Root.Items.Count + data.Length < _maxEntries)
        {
            foreach (var i in data)
                Insert(i);
            return;
        }

        if (data.Length < this._minEntries)
        {
            foreach (var i in data)
                Insert(i);
            return;
        }

        var dataRoot = BuildTree(data);
        this.Count += data.Length;

        if (this.Root.Items.Count == 0)
            this.Root = dataRoot;
        else if (this.Root.Height == dataRoot.Height)
        {
            if (this.Root.Items.Count + dataRoot.Items.Count <= this._maxEntries)
            {
                foreach (var isd in dataRoot.Items)
                    this.Root.Add(isd);
            }
            else
                SplitRoot(dataRoot);
        }
        else
        {
            if (this.Root.Height < dataRoot.Height)
            {
#pragma warning disable IDE0180 // netstandard 1.2 doesn't support tuple
                var tmp = this.Root;
                this.Root = dataRoot;
                dataRoot = tmp;
#pragma warning restore IDE0180
            }

            this.Insert(dataRoot, this.Root.Height - dataRoot.Height);
        }
    }

    /// <summary>
    /// Removes an object from the <see cref="RTree{T}"/>.
    /// </summary>
    /// <param name="item">
    /// The object to be removed from the <see cref="RTree{T}"/>.
    /// </param>
    public void Delete(T item) =>
        DoDelete(Root, item);

    private bool DoDelete(Node node, T item)
    {
        if (!node.Envelope.Contains(item.Envelope))
            return false;

        if (node.IsLeaf)
        {
            var cnt = node.Items.RemoveAll(i => _comparer.Equals((T)i, item));
            if (cnt != 0)
            {
                Count -= cnt;
                node.ResetEnvelope();
                return true;
            }
            else
                return false;
        }

        var flag = false;
        foreach (Node n in node.Items)
        {
            flag |= DoDelete(n, item);
        }

        if (flag)
            node.ResetEnvelope();
        return flag;
    }
}