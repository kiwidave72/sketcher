using System;
using System.Collections.Generic;

namespace Sketcher.Domain.Model;

public sealed class CadDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Untitled";

    public Guid RootComponentId { get; set; }
    public Guid ActiveSketchId { get; set; }

    public Dictionary<Guid, Component> Components { get; set; } = new();
    public Dictionary<Guid, Sketch> Sketches { get; set; } = new();
    public Dictionary<Guid, Body> Bodies { get; set; } = new();

    public static CadDocument CreateDefault()
    {
        var doc = new CadDocument();

        var root = new Component
        {
            Id = Guid.NewGuid(),
            ParentId = null,
            Name = "Root"
        };

        doc.RootComponentId = root.Id;
        doc.Components[root.Id] = root;

        var sketch = new Sketch
        {
            Id = Guid.NewGuid(),
            ComponentId = root.Id,
            Name = "Sketch 1",
            Model = new Sketcher.Domain.SketchModel()
        };
        doc.Sketches[sketch.Id] = sketch;
        root.SketchIds.Add(sketch.Id);

        doc.ActiveSketchId = sketch.Id;

        return doc;
    }
}
