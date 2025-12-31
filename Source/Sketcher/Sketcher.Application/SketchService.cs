using System;
using Sketcher.Domain;
using Sketcher.Domain.Constraints;
using Sketcher.Domain.Geometry;
using Sketcher.Solver.Abstractions;
using Sketcher.Application.Ports;

namespace Sketcher.Application;

public class SketchService
{
    private readonly ISketchRepository _repo;
    private readonly IConstraintSolver _solver;

    public SketchModel Model { get; private set; } = new();

    public SketchService(ISketchRepository repo, IConstraintSolver solver)
    {
        _repo = repo;
        _solver = solver;
    }

    public Guid AddPoint(double x, double y)
    {
        var p = new Point2(Guid.NewGuid(), x, y);
        Model.AddEntity(p);
        return p.Id;
    }

    public Guid AddLine(Guid startPointId, Guid endPointId)
    {
        var l = new Line2(Guid.NewGuid(), startPointId, endPointId);
        Model.AddEntity(l);
        return l.Id;
    }

    public Guid AddCircle(Guid centerPointId, double radius)
    {
        var c = new Circle2(Guid.NewGuid(), centerPointId, radius);
        Model.AddEntity(c);
        return c.Id;
    }

    public Guid AddRectangle(Guid originPointId, double width, double height)
    {
        var r = new Rectangle2(Guid.NewGuid(), originPointId, width, height);
        Model.AddEntity(r);
        return r.Id;
    }

    public void RemoveEntity(Guid id) => Model.RemoveEntity(id);

    public Guid AddCoincident(Guid pointAId, Guid pointBId)
    {
        var c = new Coincident(Guid.NewGuid(), pointAId, pointBId);
        Model.AddConstraint(c);
        return c.Id;
    }

    public Guid AddDistance(Guid pointAId, Guid pointBId, double value)
    {
        var c = new Distance(Guid.NewGuid(), pointAId, pointBId, value);
        Model.AddConstraint(c);
        return c.Id;
    }

    public Guid AddHorizontal(Guid lineId)
    {
        var c = new Horizontal(Guid.NewGuid(), lineId);
        Model.AddConstraint(c);
        return c.Id;
    }

    public Guid AddVertical(Guid lineId)
    {
        var c = new Vertical(Guid.NewGuid(), lineId);
        Model.AddConstraint(c);
        return c.Id;
    }

    public void RemoveConstraint(Guid id) => Model.RemoveConstraint(id);

    public SolveResult Solve() => _solver.Solve(Model);

    public void Save(string keyOrPath) => _repo.Save(keyOrPath, Model);
    public void Load(string keyOrPath) => Model = _repo.Load(keyOrPath);
}
