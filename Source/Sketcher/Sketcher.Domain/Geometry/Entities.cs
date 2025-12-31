using System;
using System.Text.Json.Serialization;

namespace Sketcher.Domain.Geometry;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Point2), "point2")]
[JsonDerivedType(typeof(Line2), "line2")]
[JsonDerivedType(typeof(Circle2), "circle2")]
[JsonDerivedType(typeof(Rectangle2), "rectangle2")]

public abstract record SketchEntity(Guid Id);

public record Point2(Guid Id, double X, double Y) : SketchEntity(Id);

public record Line2(Guid Id, Guid StartPointId, Guid EndPointId) : SketchEntity(Id);

public record Circle2(Guid Id, Guid CenterPointId, double Radius) : SketchEntity(Id);

public record Rectangle2(Guid Id, Guid OriginPointId, double Width, double Height) : SketchEntity(Id);
