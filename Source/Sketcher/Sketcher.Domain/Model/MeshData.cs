using System;

namespace Sketcher.Domain.Model;

public sealed class MeshData
{
    public float[] Positions { get; set; } = Array.Empty<float>(); // x,y,z...
    public int[] Indices { get; set; } = Array.Empty<int>();
    public float[]? Normals { get; set; }
}
