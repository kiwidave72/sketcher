using System;
using System.Collections.Generic;
using Sketcher.Application.Ports;
using Sketcher.Domain;
using Sketcher.Domain.Constraints;
using Sketcher.Domain.Geometry;
using Sketcher.Solver.Abstractions;

namespace Sketcher.Solver.Relaxation;

public class RelaxationSolver : IConstraintSolver
{
    public int MaxIterations { get; init; } = 100;
    public double Epsilon { get; init; } = 1e-6;
    public double Step { get; init; } = 0.5;

    public SolveResult Solve(SketchModel model)
    {
        var errors = new List<SolverError>();

        foreach (var c in model.Constraints.Values)
            foreach (var eid in c.EntityIds)
                if (!model.Entities.ContainsKey(eid))
                    errors.Add(new SolverError("MissingEntity", $"Constraint references missing entity {eid}", c.Id));

        if (errors.Count > 0)
            return new SolveResult(false, 0, double.PositiveInfinity, errors);

        double totalErr = double.PositiveInfinity;
        int iter;
        for (iter = 0; iter < MaxIterations; iter++)
        {
            totalErr = 0.0;
            foreach (var c in model.Constraints.Values)
                totalErr += Apply(model, c);

            if (totalErr < Epsilon)
                return new SolveResult(true, iter + 1, totalErr, Array.Empty<SolverError>());
        }

        errors.Add(new SolverError("NoConverge", $"Did not converge within {MaxIterations} iterations."));
        return new SolveResult(false, iter, totalErr, errors);
    }

    private double Apply(SketchModel model, Constraint c) => c switch
    {
        Coincident co => Coincident(model, co),
        Distance d => Distance(model, d),
        Horizontal h => Horizontal(model, h),
        Vertical v => Vertical(model, v),
        _ => 0.0
    };

    private double Coincident(SketchModel model, Coincident c)
    {
        var a = (Point2)model.Entities[c.PointAId];
        var b = (Point2)model.Entities[c.PointBId];
        var dx = b.X - a.X; var dy = b.Y - a.Y;
        var err = dx * dx + dy * dy;

        var midX = (a.X + b.X) / 2.0;
        var midY = (a.Y + b.Y) / 2.0;

        model.UpsertEntity(a with { X = Lerp(a.X, midX), Y = Lerp(a.Y, midY) });
        model.UpsertEntity(b with { X = Lerp(b.X, midX), Y = Lerp(b.Y, midY) });
        return err;
    }

    private double Distance(SketchModel model, Distance c)
    {
        var a = (Point2)model.Entities[c.PointAId];
        var b = (Point2)model.Entities[c.PointBId];
        var dx = b.X - a.X; var dy = b.Y - a.Y;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 1e-12) dist = 1e-12;

        var diff = dist - c.Value;
        var err = diff * diff;

        var nx = dx / dist; var ny = dy / dist;
        var move = diff * 0.5 * Step;

        model.UpsertEntity(a with { X = a.X + nx * move, Y = a.Y + ny * move });
        model.UpsertEntity(b with { X = b.X - nx * move, Y = b.Y - ny * move });
        return err;
    }

    private double Horizontal(SketchModel model, Horizontal c)
    {
        var line = (Line2)model.Entities[c.LineId];
        var a = (Point2)model.Entities[line.StartPointId];
        var b = (Point2)model.Entities[line.EndPointId];
        var dy = b.Y - a.Y;
        var err = dy * dy;

        var midY = (a.Y + b.Y) / 2.0;
        model.UpsertEntity(a with { Y = Lerp(a.Y, midY) });
        model.UpsertEntity(b with { Y = Lerp(b.Y, midY) });
        return err;
    }

    private double Vertical(SketchModel model, Vertical c)
    {
        var line = (Line2)model.Entities[c.LineId];
        var a = (Point2)model.Entities[line.StartPointId];
        var b = (Point2)model.Entities[line.EndPointId];
        var dx = b.X - a.X;
        var err = dx * dx;

        var midX = (a.X + b.X) / 2.0;
        model.UpsertEntity(a with { X = Lerp(a.X, midX) });
        model.UpsertEntity(b with { X = Lerp(b.X, midX) });
        return err;
    }

    private double Lerp(double a, double b) => a + (b - a) * Step;
}
