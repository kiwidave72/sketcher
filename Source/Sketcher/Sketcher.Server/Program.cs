using Sketcher.Server;
using Sketcher.Application.Sync;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SketchStore>();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p =>
        p.AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials()
         .SetIsOriginAllowed(_ => true));
});

var app = builder.Build();
app.UseCors();

app.MapGet("/api/sketch/{docId}", (string docId, SketchStore store) =>
{
    var u = store.Get(docId);
    return u is null ? Results.NotFound() : Results.Ok(u);
});

app.MapPut("/api/sketch/{docId}", (string docId, SketchUpdate update, SketchStore store) =>
{
    if (update.DocumentId != docId) update = update with { DocumentId = docId };
    var u = store.Put(update);
    return Results.Ok(u);
});

app.MapHub<SketchHub>("/sketchHub");

app.Run();
