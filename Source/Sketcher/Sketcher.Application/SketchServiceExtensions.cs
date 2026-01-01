using Sketcher.Domain.Model;

namespace Sketcher.Application;

public static class SketchServiceExtensions
{
    // Used by sync adapters to replace the current document atomically.
    public static void LoadFromDocument(this SketchService svc, CadDocument document)
        => svc.LoadFromDocument(document);
}
