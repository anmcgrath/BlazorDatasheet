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
			MinX: Math.Min(MinX, other.MinX),
			MinY: Math.Min(MinY, other.MinY),
			MaxX: Math.Max(MaxX, other.MaxX),
			MaxY: Math.Max(MaxY, other.MaxY));

	/// <summary>
	/// Intersects a bounding box to only include the common area
	/// of both bounding boxes
	/// </summary>
	/// <param name="other">The other bounding box</param>
	/// <returns>A new bounding box that is the intersection of both bounding boxes.</returns>
	/// <remarks>Does not affect the current bounding box.</remarks>
	public Envelope Intersection(in Envelope other) =>
		new(
			MinX: Math.Max(MinX, other.MinX),
			MinY: Math.Max(MinY, other.MinY),
			MaxX: Math.Min(MaxX, other.MaxX),
			MaxY: Math.Min(MaxY, other.MaxY));

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
}