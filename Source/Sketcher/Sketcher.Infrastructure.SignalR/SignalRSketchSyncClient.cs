using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Sketcher.Application.Ports;
using Sketcher.Application.Sync;

namespace Sketcher.Infrastructure.SignalR;

public sealed class SignalRSketchSyncClient : ISketchSyncClient
{
    private readonly string _hubUrl;
    private readonly string _clientId;
    private HubConnection? _conn;

    public bool IsConnected => _conn?.State == HubConnectionState.Connected;

    public event Action<SketchUpdate>? OnSketchUpdate;

    public SignalRSketchSyncClient(string hubUrl, string? clientId = null)
    {
        _hubUrl = hubUrl.TrimEnd('/');
        _clientId = string.IsNullOrWhiteSpace(clientId) ? Guid.NewGuid().ToString("N") : clientId;
    }

    public async Task<bool> TryConnectAsync(CancellationToken ct = default)
    {
        if (_conn is { State: HubConnectionState.Connected })
            return true;

        _conn = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _conn.On<SketchUpdate>("SketchUpdated", update =>
        {
            if (update.SourceClientId == _clientId) return;
            OnSketchUpdate?.Invoke(update);
        });

        try
        {
            await _conn.StartAsync(ct);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalR] Connect failed to {_hubUrl}: {ex}");
            return false;
        }

    }

    public async Task PublishAsync(SketchUpdate update, CancellationToken ct = default)
    {
        if (_conn is null || _conn.State != HubConnectionState.Connected)
            return;

        var normalized = update with { SourceClientId = _clientId };
        await _conn.InvokeAsync("PublishSketch", normalized, ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_conn is null) return;
        try { await _conn.StopAsync(); } catch { }
        try { await _conn.DisposeAsync(); } catch { }
    }
}
