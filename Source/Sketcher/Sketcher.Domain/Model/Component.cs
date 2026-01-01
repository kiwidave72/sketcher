using System;
using System.Collections.Generic;

namespace Sketcher.Domain.Model;

public sealed class Component
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentId { get; set; }

    public string Name { get; set; } = "Component";

    public List<Guid> ChildComponentIds { get; set; } = new();
    public List<Guid> SketchIds { get; set; } = new();
    public List<Guid> BodyIds { get; set; } = new();
}
