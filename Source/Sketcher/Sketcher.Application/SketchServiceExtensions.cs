using System.Reflection;
using Sketcher.Domain;

namespace Sketcher.Application;

public static class SketchServiceExtensions
{
    // Used by sync adapters to replace the current model atomically.
    public static void LoadFromModel(this SketchService svc, SketchModel model)
    {
        var prop = typeof(SketchService).GetProperty("Model", BindingFlags.Instance | BindingFlags.Public);
        prop!.SetValue(svc, model);
    }
}
