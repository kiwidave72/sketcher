using System;
using System.Collections.Generic;

namespace Sketcher.Domain.Model;

public sealed class Body
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ComponentId { get; set; }
    public string Name { get; set; } = "Body";

    public List<Feature> Features { get; set; } = new();

    // Optional renderable result (can be generated client-side for now).
    public MeshData? Mesh { get; set; }
}
