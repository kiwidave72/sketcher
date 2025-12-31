using Microsoft.AspNetCore.SignalR;
using Sketcher.Application.Sync;

namespace Sketcher.Server;

public class SketchHub : Hub
{
    private readonly SketchStore _store;

    public SketchHub(SketchStore store) => _store = store;

    public async Task PublishSketch(SketchUpdate update)
    {
        var stored = _store.Put(update);
        await Clients.All.SendAsync("SketchUpdated", stored);
    }

    public Task<SketchUpdate?> GetCurrent(string documentId)
        => Task.FromResult(_store.Get(documentId));
}
