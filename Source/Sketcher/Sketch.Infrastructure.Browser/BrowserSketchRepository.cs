using System.Text.Json;
using Microsoft.JSInterop;
using Sketcher.Application.Ports;
using Sketcher.Domain;

namespace Sketcher.Infrastructure.Browser;

public class BrowserSketchRepository : ISketchRepository
{
    private readonly IJSRuntime _js;
    public BrowserSketchRepository(IJSRuntime js) => _js = js;

    public void Save(string keyOrPath, SketchModel sketch) => _ = SaveAsync(keyOrPath, sketch);
    public SketchModel Load(string keyOrPath) => LoadAsync(keyOrPath).GetAwaiter().GetResult();

    private async Task SaveAsync(string key, SketchModel sketch)
    {
        var json = JsonSerializer.Serialize(sketch);
        await _js.InvokeVoidAsync("localStorage.setItem", key, json);
    }

    private async Task<SketchModel> LoadAsync(string key)
    {
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", key);
        if (string.IsNullOrWhiteSpace(json))
            return new SketchModel();
        return JsonSerializer.Deserialize<SketchModel>(json)!;
    }
}
