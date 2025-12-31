using System;
using Sketcher.Application;
using Sketcher.Infrastructure.File;
using Sketcher.Solver.Relaxation;

namespace Sketch.Cli;

class Program
{
    static void Main()
    {
        var svc = new SketchService(new JsonFileSketchRepository(), new RelaxationSolver());

        Console.WriteLine("Sketch CLI. Try: point 0 0 | point 10 5 | line <a> <b> | horizontal <line> | solve | list | save a.json");
        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var cmd = p[0].ToLowerInvariant();

            try
            {
                switch (cmd)
                {
                    case "point":
                        Console.WriteLine(svc.AddPoint(double.Parse(p[1]), double.Parse(p[2])));
                        break;
                    case "line":
                        Console.WriteLine(svc.AddLine(Guid.Parse(p[1]), Guid.Parse(p[2])));
                        break;
                    case "horizontal":
                        Console.WriteLine(svc.AddHorizontal(Guid.Parse(p[1])));
                        break;
                    case "vertical":
                        Console.WriteLine(svc.AddVertical(Guid.Parse(p[1])));
                        break;
                    case "coincident":
                        Console.WriteLine(svc.AddCoincident(Guid.Parse(p[1]), Guid.Parse(p[2])));
                        break;
                    case "distance":
                        Console.WriteLine(svc.AddDistance(Guid.Parse(p[1]), Guid.Parse(p[2]), double.Parse(p[3])));
                        break;
                    case "solve":
                    {
                        var r = svc.Solve();
                        Console.WriteLine($"Success={r.Success} Iter={r.Iterations} Err={r.FinalError}");
                        foreach (var e in r.Errors) Console.WriteLine($"  [{e.Code}] {e.Message}");
                        break;
                    }
                    case "save":
                        svc.Save(p[1]); Console.WriteLine("Saved."); break;
                    case "load":
                        svc.Load(p[1]); Console.WriteLine("Loaded."); break;
                    case "list":
                        Console.WriteLine($"Entities={svc.Model.Entities.Count} Constraints={svc.Model.Constraints.Count}");
                        foreach (var e in svc.Model.Entities.Values) Console.WriteLine($"  {e.GetType().Name} {e.Id}");
                        foreach (var c in svc.Model.Constraints.Values) Console.WriteLine($"  {c.GetType().Name} {c.Id}");
                        break;
                    case "dump":
                        {
                            Console.WriteLine($"Entities={svc.Model.Entities.Count} Constraints={svc.Model.Constraints.Count}");

                            foreach (var e in svc.Model.Entities.Values)
                            {
                                switch (e)
                                {
                                    case Sketcher.Domain.Geometry.Point2 pt:
                                        Console.WriteLine($"  Point2 {pt.Id}  ({pt.X}, {pt.Y})");
                                        break;

                                    case Sketcher.Domain.Geometry.Line2 l:
                                        Console.WriteLine($"  Line2  {l.Id}  start={l.StartPointId} end={l.EndPointId}");
                                        break;

                                    case Sketcher.Domain.Geometry.Circle2 c:
                                        Console.WriteLine($"  Circle2 {c.Id}  center={c.CenterPointId} r={c.Radius}");
                                        break;

                                    case Sketcher.Domain.Geometry.Rectangle2 r:
                                        Console.WriteLine($"  Rectangle2 {r.Id} origin={r.OriginPointId} w={r.Width} h={r.Height}");
                                        break;

                                    default:
                                        Console.WriteLine($"  {e.GetType().Name} {e.Id}");
                                        break;
                                }
                            }

                            foreach (var c in svc.Model.Constraints.Values)
                                Console.WriteLine($"  {c.GetType().Name} {c.Id}");

                            break;
                        }
                    case "exit":
                        return;
                    default:
                        Console.WriteLine("Unknown"); break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
