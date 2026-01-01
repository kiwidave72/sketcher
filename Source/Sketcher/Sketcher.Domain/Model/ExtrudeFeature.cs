using System;
using System.Collections.Generic;

namespace Sketcher.Domain.Model;

public sealed class ExtrudeFeature : Feature
{
    public Guid SketchId { get; set; }
    public List<Guid> SelectedEdgeIds { get; set; } = new();
    public double Height { get; set; }
}
