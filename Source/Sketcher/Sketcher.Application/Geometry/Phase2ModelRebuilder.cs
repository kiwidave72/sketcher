using System;
using System.Collections.Generic;
using System.Linq;
using Sketcher.Domain;
using Sketcher.Domain.Geometry;
using Sketcher.Domain.Model;

namespace Sketcher.Application.Geometry;

/// <summary>
/// Phase 2: Feature-aware rebuild that regenerates body meshes deterministically from the feature list.
/// Current implementation focuses on rectangular extrudes so Join/Cut can be tested and used in UI.
/// </summary>
public static class Phase2ModelRebuilder
{
    public static void Rebuild(CadDocument doc)
    {
        if (!doc.Sketches.TryGetValue(doc.ActiveSketchId, out var sketch))
            return;

        var model = sketch.Model;

        // For now: each body is rebuilt from its own features into a set of AABB boxes.
        foreach (var body in doc.Bodies.Values)
        {
            var solids = new List<Box>();

            foreach (var feat in body.Features.OfType<ExtrudeFeature>())
            {
                if (!TryBuildToolBox(model, feat, out var tool))
                    continue;

                if (feat.Operation == ExtrudeOperation.Cut)
                    solids = BoxBoolean.Subtract(solids, tool);
                else
                    solids = BoxBoolean.Union(solids, tool);
            }

            body.Mesh = solids.Count == 0 ? null : BoxMeshBuilder.Build(solids);
        }
    }

    private static bool TryBuildToolBox(SketchModel model, ExtrudeFeature feat, out Box box)
    {
        box = default;

        // Collect points from selected edges (lines) in the sketch model
        var pts = new List<Point2>();
        foreach (var eid in feat.SelectedEdgeIds)
        {
            if (!model.Entities.TryGetValue(eid, out var ent)) continue;
            if (ent is not Line2 ln) continue;

            if (model.Entities.TryGetValue(ln.StartPointId, out var aEnt) && aEnt is Point2 a) pts.Add(a);
            if (model.Entities.TryGetValue(ln.EndPointId, out var bEnt) && bEnt is Point2 b) pts.Add(b);
        }

        var uniq = pts
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();

        // Rectangular extrude support: 4 unique points, axis-aligned edges
        if (uniq.Count < 3) return false;

        // We treat ANY selection that forms a rectangle aligned to axes as a box.
        // Minimum requirement: at least 4 points and at least 4 edges.
        if (uniq.Count < 4 || feat.SelectedEdgeIds.Count < 4) return false;

        var minX = uniq.Min(p => p.X);
        var maxX = uniq.Max(p => p.X);
        var minY = uniq.Min(p => p.Y);
        var maxY = uniq.Max(p => p.Y);

        // Validate axis-aligned rectangle by ensuring all points lie on min/max x/y.
        const double eps = 1e-9;
        bool onRect(Point2 p) =>
            Math.Abs(p.X - minX) < eps || Math.Abs(p.X - maxX) < eps ||
            Math.Abs(p.Y - minY) < eps || Math.Abs(p.Y - maxY) < eps;

        if (!uniq.All(onRect)) return false;

        var height = feat.Height;
        if (height < eps) return false;

        box = new Box(minX, minY, 0, maxX, maxY, height);
        return box.IsValid();
    }
}
