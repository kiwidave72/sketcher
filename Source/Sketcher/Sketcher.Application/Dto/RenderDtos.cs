using System;

namespace Sketcher.Application.Dto;

public record RenderPoint(Guid Id, double X, double Y);
public record RenderLine(Guid Id, Guid StartPointId, Guid EndPointId);
public record RenderCircle(Guid Id, Guid CenterPointId, double Radius);
public record RenderRectangle(Guid Id, Guid OriginPointId, double Width, double Height);
