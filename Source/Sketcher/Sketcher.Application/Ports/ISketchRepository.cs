using Sketcher.Domain;

namespace Sketcher.Application.Ports;

public interface ISketchRepository
{
    void Save(string keyOrPath, SketchModel sketch);
    SketchModel Load(string keyOrPath);
}
