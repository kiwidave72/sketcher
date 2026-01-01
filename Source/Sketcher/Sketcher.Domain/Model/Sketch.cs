using System;

namespace Sketcher.Domain.Model;

public sealed class Sketch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ComponentId { get; set; }

    public string Name { get; set; } = "Sketch";

    // For now, the sketch lies on the XY plane.
    public Sketcher.Domain.SketchModel Model { get; set; } = new();
}
