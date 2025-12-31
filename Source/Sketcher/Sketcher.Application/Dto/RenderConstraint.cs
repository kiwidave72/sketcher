using System;

namespace Sketcher.Application.Dto;

public record RenderConstraint(
    Guid Id,
    string Type,
    Guid[] EntityIds,
    string Label
);
