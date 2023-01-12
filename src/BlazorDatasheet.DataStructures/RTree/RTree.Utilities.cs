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
	#region Sort Functions
	private static readonly IComparer<ISpatialData> s_compareMinX =
		Comparer<ISpatialData>.Create((x, y) => Comparer<double>.Default.Compare(x.Envelope.MinX, y.Envelope.MinX));
	private static readonly IComparer<ISpatialData> s_compareMinY =
		Comparer<ISpatialData>.Create((x, y) => Comparer<double>.Default.Compare(x.Envelope.MinY, y.Envelope.MinY));
	private static readonly IComparer<T> s_tcompareMinX =
		Comparer<T>.Create((x, y) => Comparer<double>.Default.Compare(x.Envelope.MinX, y.Envelope.MinX));
	private static readonly IComparer<T> s_tcompareMinY =
		Comparer<T>.Create((x, y) => Comparer<double>.Default.Compare(x.Envelope.MinY, y.Envelope.MinY));
	#endregion

	#region Search
	private List<T> DoSearch(in Envelope boundingBox)
	{
		if (!Root.Envelope.Intersects(boundingBox))
			return new List<T>();

		var intersections = new List<T>();
		var queue = new Queue<Node>();
		queue.Enqueue(Root);

		while (queue.Count != 0)
		{
			var item = queue.Dequeue();

			if (item.IsLeaf)
			{
				for (var index = 0; index < item.Items.Count; index++)
				{
					var leafChildItem = item.Items[index];
					if (leafChildItem.Envelope.Intersects(boundingBox))
						intersections.Add((T)leafChildItem);
				}
			}
			else
			{
				for (var index = 0; index < item.Items.Count; index++)
				{
					var childNode = item.Items[index];
					if (childNode.Envelope.Intersects(boundingBox))
						queue.Enqueue((Node)childNode);
				}
			}
		}

		return intersections;
	}
	#endregion

	#region Insert
	private List<Node> FindCoveringArea(in Envelope area, int depth)
	{
		var path = new List<Node>();
		var node = this.Root;

		while (true)
		{
			path.Add(node);
			if (node.IsLeaf || path.Count == depth) return path;

			var next = node.Items[0];
			var nextArea = next.Envelope.Extend(area).Area;

			for (var i = 1; i < node.Items.Count; i++)
			{
				var newArea = node.Items[1].Envelope.Extend(area).Area;
				if (newArea > nextArea)
					continue;

				if (newArea == nextArea
					&& node.Items[i].Envelope.Area >= next.Envelope.Area)
					continue;

				next = node.Items[i];
				nextArea = newArea;
			}

			node = (next as Node)!;
		}
	}

	private void Insert(ISpatialData data, int depth)
	{
		var path = FindCoveringArea(data.Envelope, depth);

		var insertNode = path[path.Count - 1];
		insertNode.Add(data);

		while (--depth >= 0)
		{
			if (path[depth].Items.Count > _maxEntries)
			{
				var newNode = SplitNode(path[depth]);
				if (depth == 0)
					SplitRoot(newNode);
				else
					path[depth - 1].Add(newNode);
			}
			else
				path[depth].ResetEnvelope();
		}
	}

	#region SplitNode
	private void SplitRoot(Node newNode) =>
		this.Root = new Node(new List<ISpatialData> { this.Root, newNode }, this.Root.Height + 1);

	private Node SplitNode(Node node)
	{
		SortChildren(node);

		var splitPoint = GetBestSplitIndex(node.Items);
		var newChildren = node.Items.Skip(splitPoint).ToList();
		node.RemoveRange(splitPoint, node.Items.Count - splitPoint);
		return new Node(newChildren, node.Height);
	}

	#region SortChildren
	private void SortChildren(Node node)
	{
		node.Items.Sort(s_compareMinX);
		var splitsByX = GetPotentialSplitMargins(node.Items);
		node.Items.Sort(s_compareMinY);
		var splitsByY = GetPotentialSplitMargins(node.Items);

		if (splitsByX < splitsByY)
			node.Items.Sort(s_compareMinX);
	}

	private double GetPotentialSplitMargins(List<ISpatialData> children) =>
		GetPotentialEnclosingMargins(children) +
		GetPotentialEnclosingMargins(children.AsEnumerable().Reverse().ToList());

	private double GetPotentialEnclosingMargins(List<ISpatialData> children)
	{
		var envelope = Envelope.EmptyBounds;
		var i = 0;
		for (; i < _minEntries; i++)
		{
			envelope = envelope.Extend(children[i].Envelope);
		}

		var totalMargin = envelope.Margin;
		for (; i < children.Count - _minEntries; i++)
		{
			envelope = envelope.Extend(children[i].Envelope);
			totalMargin += envelope.Margin;
		}

		return totalMargin;
	}
	#endregion

	private int GetBestSplitIndex(List<ISpatialData> children)
	{
		return Enumerable.Range(_minEntries, children.Count - _minEntries)
			.Select(i =>
			{
				var leftEnvelope = GetEnclosingEnvelope(children.Take(i));
				var rightEnvelope = GetEnclosingEnvelope(children.Skip(i));

				var overlap = leftEnvelope.Intersection(rightEnvelope).Area;
				var totalArea = leftEnvelope.Area + rightEnvelope.Area;
				return new { i, overlap, totalArea };
			})
			.OrderBy(x => x.overlap)
			.ThenBy(x => x.totalArea)
			.Select(x => x.i)
			.First();
	}
	#endregion
	#endregion

	#region BuildTree
	private Node BuildTree(T[] data)
	{
		var treeHeight = GetDepth(data.Length);
		var rootMaxEntries = (int)Math.Ceiling(data.Length / Math.Pow(_maxEntries, treeHeight - 1));
		return BuildNodes(new ArraySegment<T>(data), treeHeight, rootMaxEntries);
	}

	private int GetDepth(int numNodes) =>
		(int)Math.Ceiling(Math.Log(numNodes) / Math.Log(_maxEntries));

	private Node BuildNodes(ArraySegment<T> data, int height, int maxEntries)
	{
		if (data.Count <= maxEntries)
		{
			return height == 1
				? new Node(data.Cast<ISpatialData>().ToList(), height)
				: new Node(
					new List<ISpatialData>
					{
						BuildNodes(data, height - 1, _maxEntries),
					},
					height);
		}

		// after much testing, this is faster than using Array.Sort() on the provided array
		// in spite of the additional memory cost and copying. go figure!
		var byX = new ArraySegment<T>(data.OrderBy(i => i.Envelope.MinX).ToArray());

		var nodeSize = (data.Count + (maxEntries - 1)) / maxEntries;
		var subSortLength = nodeSize * (int)Math.Ceiling(Math.Sqrt(maxEntries));

		var children = new List<ISpatialData>(maxEntries);
		foreach (var subData in Chunk(byX, subSortLength))
		{
			var byY = new ArraySegment<T>(subData.OrderBy(d => d.Envelope.MinY).ToArray());

			foreach (var nodeData in Chunk(byY, nodeSize))
			{
				children.Add(BuildNodes(nodeData, height - 1, _maxEntries));
			}
		}

		return new Node(children, height);
	}

	private static IEnumerable<ArraySegment<T>> Chunk(ArraySegment<T> values, int chunkSize)
	{
		var start = 0;
		while (start < values.Count)
		{
			var len = Math.Min(values.Count - start, chunkSize);
			yield return new ArraySegment<T>(values.Array!, values.Offset + start, len);
			start += chunkSize;
		}
	}
	#endregion

	private static Envelope GetEnclosingEnvelope(IEnumerable<ISpatialData> items)
	{
		var envelope = Envelope.EmptyBounds;
		foreach (var data in items)
		{
			envelope = envelope.Extend(data.Envelope);
		}
		return envelope;
	}

	private List<T> GetAllChildren(List<T> list, Node n)
	{
		if (n.IsLeaf)
		{
			list.AddRange(
				n.Items.Cast<T>());
		}
		else
		{
			foreach (var node in n.Items.Cast<Node>())
				GetAllChildren(list, node);
		}

		return list;
	}

}
