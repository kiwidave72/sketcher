using System.Collections.Concurrent;
using Sketcher.Application.Sync;

namespace Sketcher.Server;

public sealed class SketchStore
{
    private readonly ConcurrentDictionary<string, SketchUpdate> _docs = new();

    public SketchUpdate? Get(string docId) => _docs.TryGetValue(docId, out var u) ? u : null;

    public SketchUpdate Put(SketchUpdate update)
    {
        _docs[update.DocumentId] = update;
        return update;
    }
}
