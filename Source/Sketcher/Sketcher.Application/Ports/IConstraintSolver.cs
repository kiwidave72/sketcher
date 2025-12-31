using Sketcher.Domain;
using Sketcher.Solver.Abstractions;

namespace Sketcher.Application.Ports;

public interface IConstraintSolver
{
    SolveResult Solve(SketchModel model);
}
