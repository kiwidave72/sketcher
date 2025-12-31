using System;
using System.Collections.Generic;
using System.Linq;
using Sketcher.Domain.Constraints;
using Sketcher.Domain.Geometry;

namespace Sketcher.Domain;

public class SketchModel
{
    public Dictionary<Guid, SketchEntity> Entities { get; set; } = new();
    public Dictionary<Guid, Constraint> Constraints { get; set; } = new();

    public void AddEntity(SketchEntity entity) => Entities.Add(entity.Id, entity);
    public void UpsertEntity(SketchEntity entity) => Entities[entity.Id] = entity;

    public void RemoveEntity(Guid id)
    {
        Entities.Remove(id);

        var toRemove = Constraints.Values
            .Where(c => c.EntityIds.Contains(id))
            .Select(c => c.Id)
            .ToList();

        foreach (var cid in toRemove)
            Constraints.Remove(cid);
    }

    public void AddConstraint(Constraint constraint) => Constraints.Add(constraint.Id, constraint);
    public void RemoveConstraint(Guid id) => Constraints.Remove(id);
}
