using System.IO;
using System.Text.Json;
using Sketcher.Application.Ports;
using Sketcher.Domain;

namespace Sketcher.Infrastructure.File;

public class JsonFileSketchRepository : ISketchRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public void Save(string path, SketchModel sketch)
    {
        var json = JsonSerializer.Serialize(sketch, Options);
        System.IO.File.WriteAllText(path, json);
    }

    public SketchModel Load(string path)
    {
        var json = System.IO.File.ReadAllText(path);
        return JsonSerializer.Deserialize<SketchModel>(json, Options) ?? new SketchModel();
    }
}
