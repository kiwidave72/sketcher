using System;
using System.Threading.Tasks;
using Sketcher.Application;
using Sketcher.Application.Sync;
using Sketcher.Infrastructure.File;
using Sketcher.Infrastructure.SignalR;
using Sketcher.Solver.Relaxation;

namespace Sketcher.Cli;

class Program
{
    static async Task Main(string[] args)
    {
        Thread.Sleep(5000);
        var svc = new SketchService(new JsonFileSketchRepository(), new RelaxationSolver());
        var docId = "default";
        long rev = 0;

        SignalRSketchSyncClient? sync = null;
        var hubArgIndex = Array.FindIndex(args, a => a.Equals("--hub", StringComparison.OrdinalIgnoreCase));
        if (hubArgIndex >= 0 && hubArgIndex + 1 < args.Length)
        {
            sync = new SignalRSketchSyncClient(args[hubArgIndex + 1]);
            var ok = await sync.TryConnectAsync();
            Console.WriteLine(ok ? $"Connected hub: {args[hubArgIndex + 1]}" : "Hub connect failed (offline mode).");

            if (ok)
            {
                sync.OnSketchUpdate += update =>
                {
                    svc.LoadFromDocument(update.Document);
                    rev = Math.Max(rev, update.Revision);
                    Console.WriteLine($"[hub] updated to rev {rev}");
                };
            }
        }

        Console.WriteLine("Sketcher CLI. Commands: point x y | line <startPointId> <endPointId> | rectangle [width height] | horizontal <lineId> | vertical <lineId> | coincident <pointIdA> <pointIdB> | distance <pointIdA> <pointIdB> <d> | solve | extrude <height> <lineId...> | reset | dump | save <file.json> | load <file.json> | exit");
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
                        rev = await Publish(sync, svc, docId, rev);

                        break;
                    case "line":
                        Console.WriteLine(svc.AddLine(Guid.Parse(p[1]), Guid.Parse(p[2])));
                        rev = await Publish(sync, svc, docId, rev);

                        break;
                    case "horizontal":
                        Console.WriteLine(svc.AddHorizontal(Guid.Parse(p[1])));
                        rev = await Publish(sync, svc, docId, rev);

                        break;
                    case "vertical":
                        Console.WriteLine(svc.AddVertical(Guid.Parse(p[1])));
                        rev = await Publish(sync, svc, docId, rev);

                        break;
                    //case "coincident":
                    //    Console.WriteLine(svc.AddCoincident(Guid.Parse(p[1]), Guid.Parse(p[2])));
                    //    rev = await Publish(sync, svc, docId, rev);

                    //    break;
                    //case "distance":
                    //    Console.WriteLine(svc.AddDistance(Guid.Parse(p[1]), Guid.Parse(p[2]), double.Parse(p[3])));
                    //    rev = await Publish(sync, svc, docId, rev);

                    //    break;
                    case "solve":
                        {
                            var r = svc.Solve();
                            Console.WriteLine($"Success={r.Success} Iter={r.Iterations} Err={r.FinalError}");
                            foreach (var e in r.Errors) Console.WriteLine($"  [{e.Code}] {e.Message}");
                            rev = await Publish(sync, svc, docId, rev);

                            break;
                        }
                    
                    

case "rectangle":
    // rectangle [width height]
    double w, h;
    if (p.Length >= 3)
    {
        w = double.Parse(p[1]);
        h = double.Parse(p[2]);
    }
    else
    {
        Console.Write("Width: ");
        w = double.Parse(Console.ReadLine() ?? "0");
        Console.Write("Height: ");
        h = double.Parse(Console.ReadLine() ?? "0");
    }

    // anchored at origin
    var rp1 = svc.AddPoint(0, 0);
    var rp2 = svc.AddPoint(w, 0);
    var rp3 = svc.AddPoint(w, h);
    var rp4 = svc.AddPoint(0, h);

    var rl1 = svc.AddLine(rp1, rp2);
    var rl2 = svc.AddLine(rp2, rp3);
    var rl3 = svc.AddLine(rp3, rp4);
    var rl4 = svc.AddLine(rp4, rp1);

    Console.WriteLine("Rectangle created:");
    Console.WriteLine($"  Points: {rp1}, {rp2}, {rp3}, {rp4}");
    Console.WriteLine($"  Lines:  {rl1}, {rl2}, {rl3}, {rl4}");

    rev = await Publish(sync, svc, docId, rev);
    break;

case "reset":
    // reset document to a blank default document
    svc.ResetDocument();
    Console.WriteLine("Document reset to blank.");
    rev = await Publish(sync, svc, docId, rev);
    break;

case "extrude":
                        // extrude <height> <lineId1> <lineId2> ...
                        if (p.Length < 3) { Console.WriteLine("Usage: extrude <height> <lineId1> <lineId2> ..."); break; }
                        var height = double.Parse(p[1]);
                        var lineIds = p.Skip(2).Select(Guid.Parse).ToArray();
                        var bodyId = svc.ExtrudeFromActiveSketch(lineIds, height);
                        Console.WriteLine($"Extruded Body {bodyId} (height={height})");
                        rev = await Publish(sync, svc, docId, rev);
                        break;
case "save":
                        svc.Save(p[1]); Console.WriteLine($"Saved to {p[1]}."); break;
                    case "load":
                        svc.Load(p[1]); Console.WriteLine($"Loaded. Entities={svc.Model.Entities.Count} Constraints={svc.Model.Constraints.Count}");
                        rev = await Publish(sync, svc, docId, rev);

                        break;
                    case "dump":
                        Dump(svc);
                        break;
                    case "exit":
                        if (sync is not null) await sync.DisposeAsync();
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

    static void Dump(SketchService svc)
    {
        var doc = svc.Document;
        Console.WriteLine($"Document: {doc.Name} Id={doc.Id}");
        Console.WriteLine($"Components={doc.Components.Count} Sketches={doc.Sketches.Count} Bodies={doc.Bodies.Count}");
        Console.WriteLine($"RootComponentId={doc.RootComponentId} ActiveSketchId={doc.ActiveSketchId}");

        if (doc.Components.TryGetValue(doc.RootComponentId, out var root))
        {
            Console.WriteLine($"Root Component: {root.Name} Id={root.Id}");
            Console.WriteLine($"  ChildComponents={root.ChildComponentIds.Count} Sketches={root.SketchIds.Count} Bodies={root.BodyIds.Count}");
        }

        // Active sketch details
        Console.WriteLine("=== Active Sketch ===");
        Console.WriteLine($"Entities={svc.Model.Entities.Count} Constraints={svc.Model.Constraints.Count}");

        foreach (var e in svc.Model.Entities.Values)
        {
            switch (e)
            {
                case Sketcher.Domain.Geometry.Point2 pt:
                    Console.WriteLine($"  Point2 {pt.Id} ({pt.X}, {pt.Y})");
                    break;
                case Sketcher.Domain.Geometry.Line2 ln:
                    Console.WriteLine($"  Line2 {ln.Id} start={ln.StartPointId} end={ln.EndPointId}");
                    break;
                default:
                    Console.WriteLine($"  {e.GetType().Name} {e.Id}");
                    break;
            }
        }

        foreach (var c in svc.Model.Constraints.Values)
            Console.WriteLine($"  Constraint {c.Type} {c.Id} ({c.Label}) entities=[{string.Join(", ", c.EntityIds)}]");

        Console.WriteLine("=== Bodies ===");
        foreach (var body in doc.Bodies.Values)
        {
            Console.WriteLine($"  Body {body.Id} name='{body.Name}' component={body.ComponentId} features={body.Features.Count}");
            foreach (var f in body.Features)
            {
                switch (f)
                {
                    case Sketcher.Domain.Model.ExtrudeFeature ex:
                        Console.WriteLine($"    Extrude {ex.Id} sketch={ex.SketchId} height={ex.Height} edges={ex.SelectedEdgeIds.Count}");
                        break;
                    default:
                        Console.WriteLine($"    {f.GetType().Name} {f.Id}");
                        break;
                }
            }
        }
    }

static async Task<long> Publish(
      SignalRSketchSyncClient? sync,
      SketchService svc,
      string docId,
      long rev)
    {
        if (sync is null || !sync.IsConnected)
            return rev;

        var nextRev = rev + 1;

        var update = new SketchUpdate(
            docId,
            nextRev,
            "cli",
            DateTimeOffset.UtcNow,
            svc.Document);

        await sync.PublishAsync(update);
        return nextRev;
    }

 }
