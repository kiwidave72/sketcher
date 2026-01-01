using System;
using System.Text.Json.Serialization;

namespace Sketcher.Domain.Model;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ExtrudeFeature), "extrude")]
public abstract class Feature
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Feature";
}
