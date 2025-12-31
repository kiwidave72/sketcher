using System;
using System.Threading;
using System.Threading.Tasks;
using Sketcher.Application.Sync;

namespace Sketcher.Application.Ports;

public interface ISketchSyncClient : IAsyncDisposable
{
    bool IsConnected { get; }
    Task<bool> TryConnectAsync(CancellationToken ct = default);
    Task PublishAsync(SketchUpdate update, CancellationToken ct = default);

    event Action<SketchUpdate>? OnSketchUpdate;
}
