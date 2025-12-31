using System.Linq;
using Sketcher.Application.Dto;
using Sketcher.Domain.Constraints;

namespace Sketcher.Application;

public static class ConstraintQuery
{
    public static RenderConstraint[] Constraints(this SketchService svc)
        => svc.Model.Constraints.Values.Select(c =>
        {
            var label = c switch
            {
                Horizontal => "H",
                Vertical => "V",
                Coincident => "⨉",
                Distance d => $"D={d.Value:g}",
                _ => c.GetType().Name
            };

            return new RenderConstraint(
                c.Id,
                c.GetType().Name,
                c.EntityIds.ToArray(),
                label
            );
        }).ToArray();
}
