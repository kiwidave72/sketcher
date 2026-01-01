using System;
using Sketcher.Domain.Model;

namespace Sketcher.Application.Sync;

public record SketchUpdate(
    string DocumentId,
    long Revision,
    string SourceClientId,
    DateTimeOffset TimestampUtc,
    CadDocument Document
);
