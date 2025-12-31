using System.Collections.Generic;
using System.Linq;
using Sketcher.Domain.Geometry;
using Sketcher.Application.Dto;

namespace Sketcher.Application;

public static class SketchQuery
{
    public static IReadOnlyList<RenderPoint> Points(this SketchService svc)
        => svc.Model.Entities.Values.OfType<Point2>()
            .Select(p => new RenderPoint(p.Id, p.X, p.Y))
            .ToList();
}
