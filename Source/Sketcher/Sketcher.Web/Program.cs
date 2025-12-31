using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sketcher.Application;
using Sketcher.Application.Ports;
using Sketcher.Infrastructure.Browser;
using Sketcher.Infrastructure.SignalR;
using Sketcher.Solver.Relaxation;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Sketcher.Web.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<ISketchRepository, BrowserSketchRepository>();
builder.Services.AddScoped<IConstraintSolver, RelaxationSolver>();
builder.Services.AddScoped<SketchService>();

// Optional hub client (connect button controls whether it actually connects)
builder.Services.AddScoped(sp => new SignalRSketchSyncClient("http://localhost:57054/sketchHub"));

await builder.Build().RunAsync();
