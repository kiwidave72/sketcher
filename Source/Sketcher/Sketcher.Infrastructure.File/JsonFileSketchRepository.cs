using System.IO;
using System.Text.Json;
using Sketcher.Application.Ports;
using Sketcher.Domain.Model;

namespace Sketcher.Infrastructure.File;

public class JsonFileSketchRepository : ISketchRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public void Save(string path, CadDocument document)
    {
        var json = JsonSerializer.Serialize(document, Options);
        System.IO.File.WriteAllText(path, json);
    }

    public CadDocument Load(string path)
    {
        var json = System.IO.File.ReadAllText(path);
        return JsonSerializer.Deserialize<CadDocument>(json, Options) ?? CadDocument.CreateDefault();
    }
}
