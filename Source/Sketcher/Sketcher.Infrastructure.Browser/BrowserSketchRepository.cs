using System.Text.Json;
using Microsoft.JSInterop;
using Sketcher.Application.Ports;
using Sketcher.Domain;

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

    public void Save(string key, SketchModel sketch)
    {
        var json = JsonSerializer.Serialize(sketch, Options);
        _js.InvokeVoid("localStorage.setItem", key, json);
    }

    public SketchModel Load(string key)
    {
        var json = _js.Invoke<string?>("localStorage.getItem", key);
        if (string.IsNullOrWhiteSpace(json))
            return new SketchModel();

        return JsonSerializer.Deserialize<SketchModel>(json, Options) ?? new SketchModel();
    }
}
