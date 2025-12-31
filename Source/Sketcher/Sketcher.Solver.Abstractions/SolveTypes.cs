using System;
using System.Collections.Generic;

namespace Sketcher.Solver.Abstractions;

public record SolverError(string Code, string Message, Guid? RelatedConstraintId = null);

public record SolveResult(
    bool Success,
    int Iterations,
    double FinalError,
    IReadOnlyList<SolverError> Errors
);
