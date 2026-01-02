using System;
using System.Collections.Generic;
using System.Numerics;
using Sketcher.Domain.Model;

namespace Sketcher.Application.Geometry;

public static class BoxMeshBuilder
{
    /// <summary>
    /// Builds a single MeshData from a set of axis-aligned boxes.
    /// No vertex welding; deterministic and simple for testing.
    /// </summary>
    public static MeshData Build(IReadOnlyList<Box> boxes)
    {
        var positions = new List<float>(boxes.Count * 8 * 3);
        var indices = new List<int>(boxes.Count * 12 * 3);

        int baseVertex = 0;
        foreach (var b in boxes)
        {
            // 8 vertices
            var v = new Vector3[]
            {
                new((float)b.MinX, (float)b.MinY, (float)b.MinZ), // 0
                new((float)b.MaxX, (float)b.MinY, (float)b.MinZ), // 1
                new((float)b.MaxX, (float)b.MaxY, (float)b.MinZ), // 2
                new((float)b.MinX, (float)b.MaxY, (float)b.MinZ), // 3
                new((float)b.MinX, (float)b.MinY, (float)b.MaxZ), // 4
                new((float)b.MaxX, (float)b.MinY, (float)b.MaxZ), // 5
                new((float)b.MaxX, (float)b.MaxY, (float)b.MaxZ), // 6
                new((float)b.MinX, (float)b.MaxY, (float)b.MaxZ), // 7
            };

            foreach (var p in v)
            {
                positions.Add(p.X);
                positions.Add(p.Y);
                positions.Add(p.Z);
            }

            // 12 triangles (two per face)
            void tri(int a, int c, int d) { indices.Add(baseVertex + a); indices.Add(baseVertex + c); indices.Add(baseVertex + d); }

            // bottom (0,1,2,3)
            tri(0, 1, 2); tri(0, 2, 3);
            // top (4,5,6,7)
            tri(4, 6, 5); tri(4, 7, 6);
            // front (0,1,5,4)
            tri(0, 5, 1); tri(0, 4, 5);
            // back (3,2,6,7)
            tri(3, 2, 6); tri(3, 6, 7);
            // left (0,3,7,4)
            tri(0, 3, 7); tri(0, 7, 4);
            // right (1,2,6,5)
            tri(1, 6, 2); tri(1, 5, 6);

            baseVertex += 8;
        }

        return new MeshData
        {
            Positions = positions.ToArray(),
            Indices = indices.ToArray(),
            Normals = null
        };
    }
}
