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

using System.Runtime.InteropServices;

namespace BlazorDatasheet.DataStructures.RTree;

/// <summary>
/// A bounding envelope, used to identify the bounds of of the points within
/// a particular node.
/// </summary>
/// <param name="MinX">The minimum X value of the bounding box.</param>
/// <param name="MinY">The minimum Y value of the bounding box.</param>
/// <param name="MaxX">The maximum X value of the bounding box.</param>
/// <param name="MaxY">The maximum Y value of the bounding box.</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Envelope
{
    public readonly int MinX;
    public readonly int MaxX;
    public readonly int MinY;
    public readonly int MaxY;

    /// <summary>
    /// The calculated area of the bounding box.
    /// </summary>
    public readonly int Area;

    /// <summary>
    /// Half of the linear perimeter of the bounding box
    /// </summary>
    public readonly int Margin;

    public Envelope(int minX, int minY, int maxX, int maxY)
    {
        MinX = minX;
        MaxX = maxX;
        MinY = minY;
        MaxY = maxY;
        Area = Math.Max(MaxX - MinX, 0) * Math.Max(MaxY - MinY, 0);
        Margin = Math.Max(MaxX - MinX, 0) + Math.Max(MaxY - MinY, 0);
    }

    /// <summary>
    /// Extends a bounding box to include another bounding box
    /// </summary>
    /// <param name="other">The other bounding box</param>
    /// <returns>A new bounding box that encloses both bounding boxes.</returns>
    /// <remarks>Does not affect the current bounding box.</remarks>
    public Envelope Extend(in Envelope other) =>
        new(
            minX: Math.Min(MinX, other.MinX),
            minY: Math.Min(MinY, other.MinY),
            maxX: Math.Max(MaxX, other.MaxX),
            maxY: Math.Max(MaxY, other.MaxY));

    /// <summary>
    /// Intersects a bounding box to only include the common area
    /// of both bounding boxes
    /// </summary>
    /// <param name="other">The other bounding box</param>
    /// <returns>A new bounding box that is the intersection of both bounding boxes.</returns>
    /// <remarks>Does not affect the current bounding box.</remarks>
    public Envelope Intersection(in Envelope other) =>
        new(
            minX: Math.Max(MinX, other.MinX),
            minY: Math.Max(MinY, other.MinY),
            maxX: Math.Min(MaxX, other.MaxX),
            maxY: Math.Min(MaxY, other.MaxY));

    /// <summary>
    /// Determines whether <paramref name="other"/> is contained
    /// within this bounding box.
    /// </summary>
    /// <param name="other">The other bounding box</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="other"/> is
    /// completely contained within this bounding box; 
    /// <see langword="false" /> otherwise.
    /// </returns>
    public bool Contains(in Envelope other) =>
        MinX <= other.MinX &&
        MinY <= other.MinY &&
        MaxX >= other.MaxX &&
        MaxY >= other.MaxY;

    /// <summary>
    /// Determines whether <paramref name="other"/> intersects
    /// this bounding box.
    /// </summary>
    /// <param name="other">The other bounding box</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="other"/> is
    /// intersects this bounding box in any way; 
    /// <see langword="false" /> otherwise.
    /// </returns>
    public bool Intersects(in Envelope other) =>
        MinX <= other.MaxX &&
        MinY <= other.MaxY &&
        MaxX >= other.MinX &&
        MaxY >= other.MinY;

    /// <summary>
    /// A bounding box that contains the entire 2-d plane.
    /// </summary>
    public static Envelope InfiniteBounds { get; } =
        new(
            minX: int.MinValue,
            minY: int.MinValue,
            maxX: int.MaxValue,
            maxY: int.MaxValue);

    /// <summary>
    /// An empty bounding box.
    /// </summary>
    public static Envelope EmptyBounds { get; } =
        new(
            minX: int.MaxValue,
            minY: int.MaxValue,
            maxX: int.MinValue,
            maxY: int.MinValue);
}