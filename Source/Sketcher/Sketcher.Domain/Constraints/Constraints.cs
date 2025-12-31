using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sketcher.Domain.Constraints;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Coincident), "coincident")]
[JsonDerivedType(typeof(Distance), "distance")]
[JsonDerivedType(typeof(Horizontal), "horizontal")]
[JsonDerivedType(typeof(Vertical), "vertical")]

public abstract record Constraint(Guid Id, IReadOnlyList<Guid> EntityIds);

public record Coincident(Guid Id, Guid PointAId, Guid PointBId)
    : Constraint(Id, new[] { PointAId, PointBId });

public record Distance(Guid Id, Guid PointAId, Guid PointBId, double Value)
    : Constraint(Id, new[] { PointAId, PointBId });

public record Horizontal(Guid Id, Guid LineId)
    : Constraint(Id, new[] { LineId });

public record Vertical(Guid Id, Guid LineId)
    : Constraint(Id, new[] { LineId });
