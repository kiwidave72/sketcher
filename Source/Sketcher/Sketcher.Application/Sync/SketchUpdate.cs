using System;
using Sketcher.Domain;

namespace Sketcher.Application.Sync;

public record SketchUpdate(
    string DocumentId,
    long Revision,
    string SourceClientId,
    DateTimeOffset TimestampUtc,
    SketchModel Model
);
