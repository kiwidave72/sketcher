using System;
using System.Collections.Generic;

namespace Sketcher.Application.Geometry;

/// <summary>
/// Very small, test-friendly boolean implementation for axis-aligned boxes.
/// This is intentionally limited (rectangular extrudes only) but gives Phase 2
/// real Join/Cut behavior without bringing in heavy dependencies.
/// </summary>
public static class BoxBoolean
{
    public static List<Box> Union(List<Box> solids, Box tool)
    {
        // For now, keep as a set of boxes (no merging). This preserves determinism and is easy to debug.
        solids.Add(tool);
        return solids;
    }

    public static List<Box> Subtract(List<Box> solids, Box tool, double eps = 1e-9)
    {
        var result = new List<Box>(solids.Count);
        foreach (var s in solids)
        {
            if (!s.Intersects(tool))
            {
                result.Add(s);
                continue;
            }

            var inter = s.Intersection(tool);
            if (inter is null)
            {
                result.Add(s);
                continue;
            }

            foreach (var piece in SubtractOne(s, inter.Value, eps))
                result.Add(piece);
        }
        return result;
    }

    private static IEnumerable<Box> SubtractOne(Box src, Box cut, double eps)
    {
        // cut is guaranteed to be inside src bounds (intersection)
        // We split src into up to 6 boxes around the cut region.
        // X slices
        if (cut.MinX - src.MinX > eps)
            yield return new Box(src.MinX, src.MinY, src.MinZ, cut.MinX, src.MaxY, src.MaxZ);

        if (src.MaxX - cut.MaxX > eps)
            yield return new Box(cut.MaxX, src.MinY, src.MinZ, src.MaxX, src.MaxY, src.MaxZ);

        var x0 = Math.Max(src.MinX, cut.MinX);
        var x1 = Math.Min(src.MaxX, cut.MaxX);

        // Y slices (within X overlap)
        if (cut.MinY - src.MinY > eps)
            yield return new Box(x0, src.MinY, src.MinZ, x1, cut.MinY, src.MaxZ);

        if (src.MaxY - cut.MaxY > eps)
            yield return new Box(x0, cut.MaxY, src.MinZ, x1, src.MaxY, src.MaxZ);

        var y0 = Math.Max(src.MinY, cut.MinY);
        var y1 = Math.Min(src.MaxY, cut.MaxY);

        // Z slices (within X and Y overlap)
        if (cut.MinZ - src.MinZ > eps)
            yield return new Box(x0, y0, src.MinZ, x1, y1, cut.MinZ);

        if (src.MaxZ - cut.MaxZ > eps)
            yield return new Box(x0, y0, cut.MaxZ, x1, y1, src.MaxZ);
    }
}
