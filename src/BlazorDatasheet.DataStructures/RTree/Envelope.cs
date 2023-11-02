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
public readonly record struct Envelope(
    double MinX,
    double MinY,
    double MaxX,
    double MaxY)
{
    /// <summary>
    /// The calculated area of the bounding box.
    /// </summary>
    public double Area =>
        Math.Max(MaxX - MinX, 0) * Math.Max(MaxY - MinY, 0);

    /// <summary>
    /// Half of the linear perimeter of the bounding box
    /// </summary>
    public double Margin =>
        Math.Max(MaxX - MinX, 0) + Math.Max(MaxY - MinY, 0);

    /// <summary>
    /// Extends a bounding box to include another bounding box
    /// </summary>
    /// <param name="other">The other bounding box</param>
    /// <returns>A new bounding box that encloses both bounding boxes.</returns>
    /// <remarks>Does not affect the current bounding box.</remarks>
    public Envelope Extend(in Envelope other) =>
        new(
            MinX: Math.Min(this.MinX, other.MinX),
            MinY: Math.Min(this.MinY, other.MinY),
            MaxX: Math.Max(this.MaxX, other.MaxX),
            MaxY: Math.Max(this.MaxY, other.MaxY));

    /// <summary>
    /// Intersects a bounding box to only include the common area
    /// of both bounding boxes
    /// </summary>
    /// <param name="other">The other bounding box</param>
    /// <returns>A new bounding box that is the intersection of both bounding boxes.</returns>
    /// <remarks>Does not affect the current bounding box.</remarks>
    public Envelope Intersection(in Envelope other) =>
        new(
            MinX: Math.Max(this.MinX, other.MinX),
            MinY: Math.Max(this.MinY, other.MinY),
            MaxX: Math.Min(this.MaxX, other.MaxX),
            MaxY: Math.Min(this.MaxY, other.MaxY));

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
        this.MinX <= other.MinX &&
        this.MinY <= other.MinY &&
        this.MaxX >= other.MaxX &&
        this.MaxY >= other.MaxY;

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
        this.MinX < other.MaxX &&
        this.MinY < other.MaxY &&
        this.MaxX > other.MinX &&
        this.MaxY > other.MinY;

    /// <summary>
    /// A bounding box that contains the entire 2-d plane.
    /// </summary>
    public static Envelope InfiniteBounds { get; } =
        new(
            MinX: double.NegativeInfinity,
            MinY: double.NegativeInfinity,
            MaxX: double.PositiveInfinity,
            MaxY: double.PositiveInfinity);

    /// <summary>
    /// An empty bounding box.
    /// </summary>
    public static Envelope EmptyBounds { get; } =
        new(
            MinX: double.PositiveInfinity,
            MinY: double.PositiveInfinity,
            MaxX: double.NegativeInfinity,
            MaxY: double.NegativeInfinity);

    public bool IsSameAs(in Envelope other) =>
        this.MinX == other.MinX &&
        this.MinY == other.MinY &&
        this.MaxX == other.MaxX &&
        this.MaxY == other.MaxY;
}