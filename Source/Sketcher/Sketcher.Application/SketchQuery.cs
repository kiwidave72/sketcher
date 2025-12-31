using Sketcher.Application.Dto;
using Sketcher.Domain.Geometry;
using Sketcher.Application;
using System.Collections.Generic;
using System.Linq;

namespace Sketcher.Application;

public static class SketchQuery
{
    public static IReadOnlyList<RenderPoint> Points(this SketchService svc)
        => svc.Model.Entities.Values.OfType<Point2>()
            .Select(p => new RenderPoint(p.Id, p.X, p.Y))
            .ToList();

    public static IReadOnlyList<RenderLine> Lines(this SketchService svc)
        => svc.Model.Entities.Values.OfType<Line2>()
            .Select(l => new RenderLine(l.Id, l.StartPointId, l.EndPointId))
            .ToList();

    public static IReadOnlyList<RenderCircle> Circles(this SketchService svc)
        => svc.Model.Entities.Values.OfType<Circle2>()
            .Select(c => new RenderCircle(c.Id, c.CenterPointId, c.Radius))
            .ToList();

    public static IReadOnlyList<RenderRectangle> Rectangles(this SketchService svc)
        => svc.Model.Entities.Values.OfType<Rectangle2>()
            .Select(r => new RenderRectangle(r.Id, r.OriginPointId, r.Width, r.Height))
            .ToList();
}
