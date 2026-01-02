using System;
using System.Collections.Generic;
using System.Linq;
using Sketcher.Application.Ports;
using Sketcher.Domain;
using Sketcher.Domain.Constraints;
using Sketcher.Domain.Geometry;
using Sketcher.Domain.Model;
using Sketcher.Solver.Abstractions;

namespace Sketcher.Application;

public class SketchService
{
    private readonly ISketchRepository _repo;
    private readonly IConstraintSolver _solver;

    public CadDocument Document { get; private set; }

    /// <summary>
    /// Back-compat convenience for existing UI/CLI: returns the active sketch's SketchModel.
    /// </summary>
    public SketchModel Model => ActiveSketch.Model;

    public Guid ActiveSketchId => Document.ActiveSketchId;

    private Sketch ActiveSketch
        => Document.Sketches.TryGetValue(Document.ActiveSketchId, out var sk)
            ? sk
            : EnsureDefaultSketch();

    public SketchService(ISketchRepository repo, IConstraintSolver solver)
    {
        _repo = repo;
        _solver = solver;
        Document = CadDocument.CreateDefault();
    }

    

/// <summary>
/// Resets the current document to a blank default document (root component + one empty sketch).
/// </summary>
public void ResetDocument()
{
    Document = CadDocument.CreateDefault();
}
private Sketch EnsureDefaultSketch()
    {
        if (Document.Sketches.Count > 0)
        {
            Document.ActiveSketchId = Document.Sketches.Keys.First();
            return Document.Sketches[Document.ActiveSketchId];
        }

        // Create a minimal default structure
        Document = CadDocument.CreateDefault();
        return Document.Sketches[Document.ActiveSketchId];
    }

    // ---------- Document / hierarchy ----------
    public Guid CreateComponent(Guid parentComponentId, string name)
    {
        if (!Document.Components.ContainsKey(parentComponentId))
            throw new InvalidOperationException("Parent component not found.");

        var c = new Component
        {
            Id = Guid.NewGuid(),
            ParentId = parentComponentId,
            Name = name
        };

        Document.Components[c.Id] = c;
        Document.Components[parentComponentId].ChildComponentIds.Add(c.Id);
        return c.Id;
    }

    public Guid CreateSketch(Guid componentId, string name)
    {
        if (!Document.Components.ContainsKey(componentId))
            throw new InvalidOperationException("Component not found.");

        var sk = new Sketch
        {
            Id = Guid.NewGuid(),
            ComponentId = componentId,
            Name = name,
            Model = new SketchModel()
        };

        Document.Sketches[sk.Id] = sk;
        Document.Components[componentId].SketchIds.Add(sk.Id);
        Document.ActiveSketchId = sk.Id;
        return sk.Id;
    }

    public void SetActiveSketch(Guid sketchId)
    {
        if (!Document.Sketches.ContainsKey(sketchId))
            throw new InvalidOperationException("Sketch not found.");

        Document.ActiveSketchId = sketchId;
    }

    // ---------- Sketch creation/editing ----------
    public Guid AddPoint(double x, double y)
    {
        var pt = new Point2(Guid.NewGuid(), x, y);
        ActiveSketch.Model.AddEntity(pt);
        return pt.Id;
    }

    public Guid AddLine(Guid startPointId, Guid endPointId)
    {
        var ln = new Line2(Guid.NewGuid(), startPointId, endPointId);
        ActiveSketch.Model.AddEntity(ln);
        return ln.Id;
    }

    public Guid AddCircle(Guid centerPointId, double radius)
    {
        var c = new Circle2(Guid.NewGuid(), centerPointId, radius);
        ActiveSketch.Model.AddEntity(c);
        return c.Id;
    }

    public Guid AddRectangle(Guid originPointId, double width, double height)
    {
        var r = new Rectangle2(Guid.NewGuid(), originPointId, width, height);
        ActiveSketch.Model.AddEntity(r);
        return r.Id;
    }

    

    /// <summary>
    /// Moves a set of points in the active sketch by an (dx, dy) offset.
    /// This is a direct edit; constraints can be re-solved afterwards.
    /// </summary>
    public void MovePoints(IEnumerable<Guid> pointIds, double dx, double dy)
    {
        var ids = pointIds?.ToList() ?? new List<Guid>();
        if (ids.Count == 0) throw new ArgumentException("No point ids supplied.", nameof(pointIds));

        foreach (var id in ids)
        {
            if (!ActiveSketch.Model.Entities.TryGetValue(id, out var e) || e is not Point2 p)
                throw new InvalidOperationException($"Point not found: {id}");

            // Point2 uses init-only properties, so treat it as immutable and replace the instance.
            var moved = new Point2(p.Id, p.X + dx, p.Y + dy);
            ActiveSketch.Model.UpsertEntity(moved);
        }
    }
public void RemoveEntity(Guid id) => ActiveSketch.Model.RemoveEntity(id);

    public Guid AddHorizontal(Guid lineId)
    {
        var c = new Horizontal(Guid.NewGuid(), lineId);
        ActiveSketch.Model.AddConstraint(c);
        return c.Id;
    }

    public Guid AddVertical(Guid lineId)
    {
        var c = new Vertical(Guid.NewGuid(), lineId);
        ActiveSketch.Model.AddConstraint(c);
        return c.Id;
    }

    public Guid AddCoincident(Guid pointAId, Guid pointBId)
    {
        var c = new Coincident(Guid.NewGuid(), pointAId, pointBId);
        ActiveSketch.Model.AddConstraint(c);
        return c.Id;
    }

    public Guid AddDistance(Guid pointAId, Guid pointBId, double value)
    {
        var c = new Distance(Guid.NewGuid(), pointAId, pointBId, value);
        ActiveSketch.Model.AddConstraint(c);
        return c.Id;
    }

    public void RemoveConstraint(Guid id) => ActiveSketch.Model.RemoveConstraint(id);

    public SolveResult Solve() => _solver.Solve(ActiveSketch.Model);

    // ---------- Solid modeling (feature-based) ----------
    public Guid CreateBody(Guid componentId, string name = "Body")
    {
        if (!Document.Components.ContainsKey(componentId))
            throw new InvalidOperationException("Component not found.");

        var body = new Body
        {
            Id = Guid.NewGuid(),
            ComponentId = componentId,
            Name = name
        };

        Document.Bodies[body.Id] = body;
        Document.Components[componentId].BodyIds.Add(body.Id);
        return body.Id;
    }

    public Guid AddExtrudeFeature(Guid bodyId, Guid sketchId, IEnumerable<Guid> selectedEdgeIds, double height, ExtrudeOperation operation = ExtrudeOperation.Join)
    {
        if (!Document.Bodies.TryGetValue(bodyId, out var body))
            throw new InvalidOperationException("Body not found.");
        if (!Document.Sketches.ContainsKey(sketchId))
            throw new InvalidOperationException("Sketch not found.");

        var feat = new ExtrudeFeature
        {
            Id = Guid.NewGuid(),
            Name = "Extrude",
            SketchId = sketchId,
            SelectedEdgeIds = selectedEdgeIds.ToList(),
            Height = height,
            Operation = operation
        };

        body.Features.Add(feat);
        return feat.Id;
    }

    /// <summary>
    /// Convenience for CLI/Web: extrude from the active sketch into a new body under the same component.
    /// </summary>
    public Guid ExtrudeFromActiveSketch(IEnumerable<Guid> selectedLineIds, double height)
    {
        var sketch = ActiveSketch;
        var bodyId = CreateBody(sketch.ComponentId, "Extrude Body");
        AddExtrudeFeature(bodyId, sketch.Id, selectedLineIds, height, ExtrudeOperation.Join);
        return bodyId;
    }

    
    /// <summary>
    /// Used by sync adapters to replace the current document atomically.
    /// </summary>
    public void LoadFromDocument(CadDocument document)
    {
        Document = document ?? CadDocument.CreateDefault();
        if (Document.ActiveSketchId == Guid.Empty || !Document.Sketches.ContainsKey(Document.ActiveSketchId))
            EnsureDefaultSketch();
    }

// ---------- Persistence ----------
    public void Save(string keyOrPath) => _repo.Save(keyOrPath, Document);

    public void Load(string keyOrPath)
    {
        Document = _repo.Load(keyOrPath) ?? CadDocument.CreateDefault();

        // Ensure required IDs exist
        if (Document.RootComponentId == Guid.Empty || !Document.Components.ContainsKey(Document.RootComponentId))
        {
            var fresh = CadDocument.CreateDefault();
            Document.RootComponentId = fresh.RootComponentId;
            foreach (var kv in fresh.Components) Document.Components[kv.Key] = kv.Value;
            foreach (var kv in fresh.Sketches) Document.Sketches[kv.Key] = kv.Value;
            Document.ActiveSketchId = fresh.ActiveSketchId;
        }

        if (Document.ActiveSketchId == Guid.Empty || !Document.Sketches.ContainsKey(Document.ActiveSketchId))
            EnsureDefaultSketch();
    }
}
