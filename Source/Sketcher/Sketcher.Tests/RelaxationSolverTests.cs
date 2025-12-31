using Sketcher.Application;
using Sketcher.Application.Ports;
using Sketcher.Infrastructure.File;
using Sketcher.Solver.Relaxation;
using Xunit;

public class RelaxationSolverTests
{
    [Fact]
    public void HorizontalConstraint_MakesEndpointsSameY()
    {
        ISketchRepository repo = new JsonFileSketchRepository();
        var solver = new RelaxationSolver { MaxIterations = 200 };
        var svc = new SketchService(repo, solver);

        var a = svc.AddPoint(0, 0);
        var b = svc.AddPoint(10, 5);
        var l = svc.AddLine(a, b);
        svc.AddHorizontal(l);

        var res = svc.Solve();
        Assert.True(res.Success);

        var pa = (Sketcher.Domain.Geometry.Point2)svc.Model.Entities[a];
        var pb = (Sketcher.Domain.Geometry.Point2)svc.Model.Entities[b];
        Assert.True(System.Math.Abs(pa.Y - pb.Y) < 1e-3);
    }
}
