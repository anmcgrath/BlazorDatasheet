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

/// <summary>
/// Provides the base interface for the abstraction for
/// an updateable data store of elements on a 2-d plane.
/// </summary>
/// <typeparam name="T">The type of elements in the index.</typeparam>
public interface ISpatialDatabase<T> : ISpatialIndex<T>
{
	/// <summary>
	/// Adds an object to the <see cref="ISpatialDatabase{T}"/>
	/// </summary>
	/// <param name="item">
	/// The object to be added to <see cref="ISpatialDatabase{T}"/>.
	/// </param>
	void Insert(T item);

	/// <summary>
	/// Removes an object from the <see cref="ISpatialDatabase{T}"/>.
	/// </summary>
	/// <param name="item">
	/// The object to be removed from the <see cref="ISpatialDatabase{T}"/>.
	/// </param>
	void Delete(T item);

	/// <summary>
	/// Removes all elements from the <see cref="ISpatialDatabase{T}"/>.
	/// </summary>
	void Clear();

	/// <summary>
	/// Adds all of the elements from the collection to the <see cref="ISpatialDatabase{T}"/>.
	/// </summary>
	/// <param name="items">
	/// A collection of items to add to the <see cref="ISpatialDatabase{T}"/>.
	/// </param>
	/// <remarks>
	/// For multiple items, this method is more performant than 
	/// adding items individually via <see cref="Insert(T)"/>.
	/// </remarks>
	void BulkLoad(IEnumerable<T> items);
}
