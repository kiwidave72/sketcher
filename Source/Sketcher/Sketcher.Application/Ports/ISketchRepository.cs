using Sketcher.Domain.Model;

namespace Sketcher.Application.Ports;

public interface ISketchRepository
{
    void Save(string keyOrPath, CadDocument document);
    CadDocument Load(string keyOrPath);
}
