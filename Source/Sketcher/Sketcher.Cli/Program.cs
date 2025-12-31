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
                    svc.LoadFromModel(update.Model);
                    rev = Math.Max(rev, update.Revision);
                    Console.WriteLine($"[hub] updated to rev {rev}");
                };
            }
        }

        Console.WriteLine("Sketcher CLI. Commands: point x y | line <pA> <pB> | horizontal <line> | solve | dump | save f.json | load f.json | exit");
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
                    case "coincident":
                        Console.WriteLine(svc.AddCoincident(Guid.Parse(p[1]), Guid.Parse(p[2])));
                        rev = await Publish(sync, svc, docId, rev);

                        break;
                    case "distance":
                        Console.WriteLine(svc.AddDistance(Guid.Parse(p[1]), Guid.Parse(p[2]), double.Parse(p[3])));
                        rev = await Publish(sync, svc, docId, rev);

                        break;
                    case "solve":
                        {
                            var r = svc.Solve();
                            Console.WriteLine($"Success={r.Success} Iter={r.Iterations} Err={r.FinalError}");
                            foreach (var e in r.Errors) Console.WriteLine($"  [{e.Code}] {e.Message}");
                            rev = await Publish(sync, svc, docId, rev);

                            break;
                        }
                    case "save":
                        svc.Save(p[1]); Console.WriteLine("Saved."); break;
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
            Console.WriteLine($"  {c.GetType().Name} {c.Id}");
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
            svc.Model);

        await sync.PublishAsync(update);
        return nextRev;
    }

 }
