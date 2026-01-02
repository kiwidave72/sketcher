using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sketcher.Application;
using Sketcher.Application.Sync;
using Sketcher.Domain;
using Sketcher.Domain.Geometry;
using Sketcher.Domain.Model;
using Sketcher.Infrastructure.File;
using Sketcher.Infrastructure.SignalR;
using Sketcher.Solver.Relaxation;

namespace Sketcher.Cli;

class Program
{
    private sealed record CliState(CadDocument DocSnapshot, Guid[]? LastRectangleLineIds);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    static async Task Main(string[] args)
    {
        // Helpful when you start Server+Web+CLI together.
        Thread.Sleep(5000);

        var svc = new SketchService(new JsonFileSketchRepository(), new RelaxationSolver());
        var docId = "default";
        long rev = 0;

        // CLI-local state
        var history = new Stack<CliState>();
        Guid[]? lastRectangleLineIds = null;

        SignalRSketchSyncClient? sync = null;
        var hubArgIndex = Array.FindIndex(args, a => a.Equals("--hub", StringComparison.OrdinalIgnoreCase));
        if (hubArgIndex >= 0 && hubArgIndex + 1 < args.Length)
        {
            var hubUrl = args[hubArgIndex + 1];
            sync = new SignalRSketchSyncClient(hubUrl);

            // (1) Retry connect until success (1s backoff)
            while (true)
            {
                var ok = await sync.TryConnectAsync();
                if (ok)
                {
                    Console.WriteLine($"Connected hub: {hubUrl}");
                    break;
                }

                Console.WriteLine("Hub connect failed. Retrying in 1s...");
                await Task.Delay(1000);
            }

            // If connected, subscribe to remote updates
            sync.OnSketchUpdate += update =>
            {
                svc.LoadFromDocument(update.Document);
                rev = Math.Max(rev, update.Revision);
                Console.WriteLine($"\n[hub] update rev={update.Revision} (source={update.SourceClientId})");
                Console.Write("> ");
            };
        }

        Console.WriteLine("Sketcher CLI.");
        Console.WriteLine("Commands:");
        Console.WriteLine("  point x y");
        Console.WriteLine("  line <startPointId> <endPointId>");
        Console.WriteLine("  rectangle [width height]");
        Console.WriteLine("  horizontal <lineId> | vertical <lineId>");
        Console.WriteLine("  coincident <entityAId> <entityBId>");
        Console.WriteLine("  distance <entityAId> <entityBId> <value>");
        Console.WriteLine("  solve");
        Console.WriteLine("  extrude <height> <lineId1> <lineId2> ...");
        Console.WriteLine("  extrude           (prompts to extrude last rectangle)");
        Console.WriteLine("  move <dx> <dy> <pointId1> [pointId2 ...]");
        Console.WriteLine("  undo");
        Console.WriteLine("  reset | dump | save <file.json> | load <file.json> | exit");

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
                    {
                        PushUndo(history, svc, lastRectangleLineIds);
                        Console.WriteLine(svc.AddPoint(ParseD(p[1]), ParseD(p[2])));
                        rev = await Publish(sync, svc, docId, rev);
                        break;
                    }

                    case "line":
                    {
                        PushUndo(history, svc, lastRectangleLineIds);
                        Console.WriteLine(svc.AddLine(Guid.Parse(p[1]), Guid.Parse(p[2])));
                        rev = await Publish(sync, svc, docId, rev);
                        break;
                    }

                    case "horizontal":
                    {
                        PushUndo(history, svc, lastRectangleLineIds);
                        Console.WriteLine(svc.AddHorizontal(Guid.Parse(p[1])));
                        rev = await Publish(sync, svc, docId, rev);
                        break;
                    }

                    case "vertical":
                    {
                        PushUndo(history, svc, lastRectangleLineIds);
                        Console.WriteLine(svc.AddVertical(Guid.Parse(p[1])));
                        rev = await Publish(sync, svc, docId, rev);
                        break;
                    }

                    case "coincident":
                    {
                        PushUndo(history, svc, lastRectangleLineIds);
                        Console.WriteLine(svc.AddCoincident(Guid.Parse(p[1]), Guid.Parse(p[2])));
                        rev = await Publish(sync, svc, docId, rev);
                        break;
                    }

                    case "distance":
                    {
                        PushUndo(history, svc, lastRectangleLineIds);
                        Console.WriteLine(svc.AddDistance(Guid.Parse(p[1]), Guid.Parse(p[2]), ParseD(p[3])));
                        rev = await Publish(sync, svc, docId, rev);
                        break;
                    }

                    case "solve":
                    {
                        PushUndo(history, svc, lastRectangleLineIds);
                        var r = svc.Solve();
                        Console.WriteLine($"Success={r.Success} Iter={r.Iterations} Err={r.FinalError}");
                        foreach (var e in r.Errors) Console.WriteLine($"  [{e.Code}] {e.Message}");
                        rev = await Publish(sync, svc, docId, rev);
                        break;
                    }

                    case "rectangle":
                    {
                        PushUndo(history, svc, lastRectangleLineIds);

                        // rectangle [width height]
                        double w, h;
                        if (p.Length >= 3)
                        {
                            w = ParseD(p[1]);
                            h = ParseD(p[2]);
                        }
                        else
                        {
                            Console.Write("Width: ");
                            w = ParseD(Console.ReadLine() ?? "0");
                            Console.Write("Height: ");
                            h = ParseD(Console.ReadLine() ?? "0");
                        }

                        // anchored at origin
                        var rp1 = svc.AddPoint(0, 0);
                        var rp2 = svc.AddPoint(w, 0);
                        var rp3 = svc.AddPoint(w, h);
                        var rp4 = svc.AddPoint(0, h);

                        var l1 = svc.AddLine(rp1, rp2);
                        var l2 = svc.AddLine(rp2, rp3);
                        var l3 = svc.AddLine(rp3, rp4);
                        var l4 = svc.AddLine(rp4, rp1);

                        lastRectangleLineIds = new[] { l1, l2, l3, l4 };

                        Console.WriteLine($"Rectangle points: {rp1} {rp2} {rp3} {rp4}");
                        Console.WriteLine($"Rectangle lines:  {l1} {l2} {l3} {l4}");

                        rev = await Publish(sync, svc, docId, rev);
                        break;
                    }

                    case "move":
                    {
                        // (3) move dx dy pointIds...
                        if (p.Length < 4)
                        {
                            Console.WriteLine("Usage: move <dx> <dy> <pointId1> [pointId2 ...]");
                            break;
                        }

                        PushUndo(history, svc, lastRectangleLineIds);

                        var dx = ParseD(p[1]);
                        var dy = ParseD(p[2]);
                        var pointIds = p.Skip(3).Select(Guid.Parse).ToArray();

                        svc.MovePoints(pointIds, dx, dy);

                        // Re-solve to keep constraints consistent and allow the UI to re-render everything.
                        svc.Solve();

                        Console.WriteLine($"Moved {pointIds.Length} point(s) by ({dx}, {dy}).");
                        rev = await Publish(sync, svc, docId, rev);
                        break;
                    }

                    case "extrude":
                    {
                        double height;
                        Guid[] lineIds;

                        // (2) extrude with no params => prompt for last rectangle
                        if (p.Length == 1)
                        {
                            if (lastRectangleLineIds is null || lastRectangleLineIds.Length == 0)
                            {
                                Console.WriteLine("No previous rectangle to extrude. Usage: extrude <height> <lineId1> <lineId2> ...");
                                break;
                            }

                            Console.Write("Extrude last rectangle? (Y/n): ");
                            var ans = (Console.ReadLine() ?? "").Trim();
                            if (ans.Length > 0 && ans.StartsWith("n", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine("Cancelled.");
                                break;
                            }

                            Console.Write("Height: ");
                            height = ParseD(Console.ReadLine() ?? "0");
                            lineIds = lastRectangleLineIds;
                        }
                        else
                        {
                            if (p.Length < 3)
                            {
                                Console.WriteLine("Usage: extrude <height> <lineId1> <lineId2> ...");
                                break;
                            }

                            height = ParseD(p[1]);
                            lineIds = p.Skip(2).Select(Guid.Parse).ToArray();
                        }

                        PushUndo(history, svc, lastRectangleLineIds);

                        // (4) If the resulting solid intersects an existing solid, prompt for Join/Cut (default Join).
                        var activeSketchId = svc.Document.ActiveSketchId;
                        var profileBounds = TryComputeProfileBounds(svc.Document.Sketches[activeSketchId].Model, lineIds);
                        if (profileBounds is null)
                        {
                            Console.WriteLine("Could not compute profile bounds from supplied line ids.");
                            break;
                        }

                        var hitBodyId = FindFirstIntersectingBody(svc, profileBounds.Value.minX, profileBounds.Value.minY, profileBounds.Value.maxX, profileBounds.Value.maxY, height);
                        if (hitBodyId is not null)
                        {
                            Console.WriteLine($"Intersection detected with body {hitBodyId}.");
                            Console.Write("Operation? [Join/cut] (default Join): ");
                            var opStr = (Console.ReadLine() ?? "").Trim();

                            var op = opStr.StartsWith("c", StringComparison.OrdinalIgnoreCase)
                                ? ExtrudeOperation.Cut
                                : ExtrudeOperation.Join;

                            svc.AddExtrudeFeature(hitBodyId.Value, activeSketchId, lineIds, height, op);
                            Console.WriteLine($"Applied {op} extrusion feature to existing body {hitBodyId} (height={height}).");
                        }
                        else
                        {
                            var bodyId = svc.ExtrudeFromActiveSketch(lineIds, height);
                            Console.WriteLine($"Extruded Body {bodyId} (height={height}).");
                        }

                        rev = await Publish(sync, svc, docId, rev);
                        break;
                    }

                    case "undo":
                    {
                        // (5) Undo last operation
                        if (history.Count == 0)
                        {
                            Console.WriteLine("Nothing to undo.");
                            break;
                        }

                        var state = history.Pop();
                        svc.LoadFromDocument(Clone(state.DocSnapshot));
                        lastRectangleLineIds = state.LastRectangleLineIds;

                        Console.WriteLine("Undone last operation.");
                        rev = await Publish(sync, svc, docId, rev);
                        break;
                    }

                    case "reset":
                    {
                        PushUndo(history, svc, lastRectangleLineIds);
                        svc.LoadFromDocument(CadDocument.CreateDefault());
                        lastRectangleLineIds = null;
                        Console.WriteLine("Reset document.");
                        rev = await Publish(sync, svc, docId, rev);
                        break;
                    }

                    case "save":
                    {
                        svc.Save(p[1]);
                        Console.WriteLine($"Saved to {p[1]}.");
                        break;
                    }

                    case "load":
                    {
                        PushUndo(history, svc, lastRectangleLineIds);
                        svc.Load(p[1]);
                        lastRectangleLineIds = null; // unknown after load
                        Console.WriteLine($"Loaded from {p[1]}.");
                        rev = await Publish(sync, svc, docId, rev);
                        break;
                    }

                    case "dump":
                        Dump(svc);
                        break;

                    case "exit":
                        if (sync is not null) await sync.DisposeAsync();
                        return;

                    default:
                        Console.WriteLine("Unknown command.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private static double ParseD(string s) => double.Parse(s, CultureInfo.InvariantCulture);

    private static void PushUndo(Stack<CliState> history, SketchService svc, Guid[]? lastRectangleLineIds)
        => history.Push(new CliState(Clone(svc.Document), lastRectangleLineIds));

    private static CadDocument Clone(CadDocument doc)
    {
        var json = JsonSerializer.Serialize(doc, JsonOptions);
        return JsonSerializer.Deserialize<CadDocument>(json, JsonOptions) ?? CadDocument.CreateDefault();
    }

    private static (double minX, double minY, double maxX, double maxY)? TryComputeProfileBounds(SketchModel model, IEnumerable<Guid> lineIds)
    {
        var ids = lineIds.ToArray();
        if (ids.Length == 0) return null;

        var xs = new List<double>();
        var ys = new List<double>();

        foreach (var id in ids)
        {
            if (!model.Entities.TryGetValue(id, out var e) || e is not Line2 ln)
                continue;

            if (model.Entities.TryGetValue(ln.StartPointId, out var s) && s is Point2 sp)
            {
                xs.Add(sp.X);
                ys.Add(sp.Y);
            }

            if (model.Entities.TryGetValue(ln.EndPointId, out var t) && t is Point2 tp)
            {
                xs.Add(tp.X);
                ys.Add(tp.Y);
            }
        }

        if (xs.Count == 0) return null;

        return (xs.Min(), ys.Min(), xs.Max(), ys.Max());
    }

    private static Guid? FindFirstIntersectingBody(SketchService svc, double minX, double minY, double maxX, double maxY, double newHeight)
    {
        foreach (var body in svc.Document.Bodies.Values)
        {
            var bounds = TryComputeBodyBounds(svc, body);
            if (bounds is null) continue;

            var (bMinX, bMinY, bMaxX, bMaxY, bMaxZ) = bounds.Value;

            var xyOverlap = !(maxX < bMinX || minX > bMaxX || maxY < bMinY || minY > bMaxY);
            var zOverlap = !(newHeight < 0 || 0 > bMaxZ); // bodies assumed to start at z=0

            if (xyOverlap && zOverlap)
                return body.Id;
        }

        return null;
    }

    private static (double minX, double minY, double maxX, double maxY, double maxZ)? TryComputeBodyBounds(SketchService svc, Body body)
    {
        double? minX = null, minY = null, maxX = null, maxY = null, maxZ = null;

        foreach (var f in body.Features.OfType<ExtrudeFeature>())
        {
            if (!svc.Document.Sketches.TryGetValue(f.SketchId, out var sk))
                continue;

            var b = TryComputeProfileBounds(sk.Model, f.SelectedEdgeIds);
            if (b is null) continue;

            minX = minX is null ? b.Value.minX : Math.Min(minX.Value, b.Value.minX);
            minY = minY is null ? b.Value.minY : Math.Min(minY.Value, b.Value.minY);
            maxX = maxX is null ? b.Value.maxX : Math.Max(maxX.Value, b.Value.maxX);
            maxY = maxY is null ? b.Value.maxY : Math.Max(maxY.Value, b.Value.maxY);
            maxZ = maxZ is null ? f.Height : Math.Max(maxZ.Value, f.Height);
        }

        if (minX is null || maxZ is null) return null;
        return (minX.Value, minY!.Value, maxX!.Value, maxY!.Value, maxZ.Value);
    }

    static void Dump(SketchService svc)
    {
        var doc = svc.Document;
        Console.WriteLine($"Document: {doc.Name} Id={doc.Id}");
        Console.WriteLine($"Components={doc.Components.Count} Sketches={doc.Sketches.Count} Bodies={doc.Bodies.Count}");
        Console.WriteLine($"RootComponentId={doc.RootComponentId} ActiveSketchId={doc.ActiveSketchId}");

        // Active sketch details
        Console.WriteLine("=== Active Sketch ===");
        Console.WriteLine($"Entities={svc.Model.Entities.Count} Constraints={svc.Model.Constraints.Count}");

        foreach (var e in svc.Model.Entities.Values)
        {
            switch (e)
            {
                case Point2 pt:
                    Console.WriteLine($"  Point2 {pt.Id} ({pt.X}, {pt.Y})");
                    break;
                case Line2 ln:
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
                if (f is ExtrudeFeature ex)
                {
                    Console.WriteLine($"    ExtrudeFeature {ex.Id} sketch={ex.SketchId} height={ex.Height} op={ex.Operation} edges=[{string.Join(", ", ex.SelectedEdgeIds)}]");
                }
                else
                {
                    Console.WriteLine($"    Feature {f.Id} {f.GetType().Name}");
                }
            }
        }
    }

    static async Task<long> Publish(SignalRSketchSyncClient? sync, SketchService svc, string docId, long rev)
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
