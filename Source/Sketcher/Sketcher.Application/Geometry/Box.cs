using System;

namespace Sketcher.Application.Geometry;

/// <summary>
/// Axis-aligned box used for Phase 2 mesh-based feature replay.
/// Units match sketch coordinates; Z is extrusion axis.
/// </summary>
public readonly struct Box
{
    public double MinX { get; }
    public double MinY { get; }
    public double MinZ { get; }
    public double MaxX { get; }
    public double MaxY { get; }
    public double MaxZ { get; }

    public Box(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
    {
        if (maxX < minX) (minX, maxX) = (maxX, minX);
        if (maxY < minY) (minY, maxY) = (maxY, minY);
        if (maxZ < minZ) (minZ, maxZ) = (maxZ, minZ);

        MinX = minX; MinY = minY; MinZ = minZ;
        MaxX = maxX; MaxY = maxY; MaxZ = maxZ;
    }

    public double SizeX => MaxX - MinX;
    public double SizeY => MaxY - MinY;
    public double SizeZ => MaxZ - MinZ;

    public bool IsValid(double eps = 1e-9) => SizeX > eps && SizeY > eps && SizeZ > eps;

    public bool Intersects(in Box other)
        => !(other.MaxX <= MinX || other.MinX >= MaxX ||
             other.MaxY <= MinY || other.MinY >= MaxY ||
             other.MaxZ <= MinZ || other.MinZ >= MaxZ);

    public Box? Intersection(in Box other)
    {
        var minX = Math.Max(MinX, other.MinX);
        var minY = Math.Max(MinY, other.MinY);
        var minZ = Math.Max(MinZ, other.MinZ);
        var maxX = Math.Min(MaxX, other.MaxX);
        var maxY = Math.Min(MaxY, other.MaxY);
        var maxZ = Math.Min(MaxZ, other.MaxZ);

        var b = new Box(minX, minY, minZ, maxX, maxY, maxZ);
        return b.IsValid() ? b : null;
    }
}
