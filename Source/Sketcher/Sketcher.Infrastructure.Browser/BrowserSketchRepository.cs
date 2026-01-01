using System.Text.Json;
using Microsoft.JSInterop;
using Sketcher.Application.Ports;
using Sketcher.Domain.Model;

namespace Sketcher.Infrastructure.Browser;

public class BrowserSketchRepository : ISketchRepository
{
    private readonly IJSInProcessRuntime _js;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BrowserSketchRepository(IJSRuntime js)
    {
        _js = js as IJSInProcessRuntime
              ?? throw new PlatformNotSupportedException(
                  "BrowserSketchRepository requires IJSInProcessRuntime (Blazor WebAssembly).");
    }

    public void Save(string key, CadDocument document)
    {
        var json = JsonSerializer.Serialize(document, Options);
        _js.InvokeVoid("localStorage.setItem", key, json);
    }

    public CadDocument Load(string key)
    {
        var json = _js.Invoke<string?>("localStorage.getItem", key);
        if (string.IsNullOrWhiteSpace(json))
            return CadDocument.CreateDefault();

        return JsonSerializer.Deserialize<CadDocument>(json, Options) ?? CadDocument.CreateDefault();
    }
}
