using System;
using System.Collections.Generic;

namespace Sketcher.Domain.Model;

public enum ExtrudeOperation
{
    Join = 0,
    Cut = 1
}

public sealed class ExtrudeFeature : Feature
{
    public Guid SketchId { get; set; }
    public List<Guid> SelectedEdgeIds { get; set; } = new();
    public double Height { get; set; }

    /// <summary>
    /// How this extrusion should be applied when it intersects an existing solid.
    /// Default is Join.
    /// </summary>
    public ExtrudeOperation Operation { get; set; } = ExtrudeOperation.Join;
}
