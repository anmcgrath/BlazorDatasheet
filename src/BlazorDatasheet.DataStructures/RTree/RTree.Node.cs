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

// This file contains code from https://github.com/viceroypenguin/RBush

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
			this.Height = height;
			this.Items = items;
			ResetEnvelope();
		}

		internal void Add(ISpatialData node)
		{
			Items.Add(node);
			_envelope = Envelope.Extend(node.Envelope);
		}

		internal void Remove(ISpatialData node)
		{
			Items.Remove(node);
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
