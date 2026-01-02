using Microsoft.AspNetCore.SignalR;
using Sketcher.Application.Geometry;
using Sketcher.Application.Sync;

namespace Sketcher.Server;

public class SketchHub : Hub
{
    private readonly SketchStore _store;

    public SketchHub(SketchStore store) => _store = store;

    public async Task PublishSketch(SketchUpdate update)
    {
        // Phase 2 rebuild: generate body meshes server-side so all clients (CLI/Web)
        // see consistent Join/Cut results without relying on JS-side reconstruction.
        Phase2ModelRebuilder.Rebuild(update.Document);
        var stored = _store.Put(update);
        await Clients.All.SendAsync("SketchUpdated", stored);
    }

    public Task<SketchUpdate?> GetCurrent(string documentId)
        => Task.FromResult(_store.Get(documentId));
}
