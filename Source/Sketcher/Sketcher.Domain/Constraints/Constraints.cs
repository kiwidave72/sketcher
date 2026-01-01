using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sketcher.Domain.Constraints;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Coincident), "coincident")]
[JsonDerivedType(typeof(Distance), "distance")]
[JsonDerivedType(typeof(Horizontal), "horizontal")]
[JsonDerivedType(typeof(Vertical), "vertical")]
public abstract record Constraint
{
    public Guid Id { get; init; }

    [JsonIgnore]
    public abstract IReadOnlyList<Guid> EntityIds { get; }

    [JsonIgnore]
    public virtual string Type => GetType().Name;

    [JsonIgnore]
    public virtual string Label => Type;

    protected Constraint(Guid id) => Id = id;
}

public record Coincident(Guid Id, Guid PointAId, Guid PointBId) : Constraint(Id)
{
    public override IReadOnlyList<Guid> EntityIds => new[] { PointAId, PointBId };
}

public record Distance(Guid Id, Guid PointAId, Guid PointBId, double Value) : Constraint(Id)
{
    public override IReadOnlyList<Guid> EntityIds => new[] { PointAId, PointBId };
}

public record Horizontal(Guid Id, Guid LineId) : Constraint(Id)
{
    public override IReadOnlyList<Guid> EntityIds => new[] { LineId };
}

public record Vertical(Guid Id, Guid LineId) : Constraint(Id)
{
    public override IReadOnlyList<Guid> EntityIds => new[] { LineId };
}