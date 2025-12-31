# Sketcher

Sketcher is an experimental **parametric sketching kernel** designed to explore how a modern CAD-style sketch system can be built with:
- clean architecture,
- multiple front-ends,
- live synchronization,
- and a strong separation between geometry, constraints, and UI.

Sketcher currently focuses on **2D parametric sketching**, but is intentionally structured to grow into:
- solid modeling,
- mesh generation,
- and 3D-print slicing pipelines.

This is **not a UI-first project**.  
The sketch kernel is the product.

---

## Key Ideas

- **Hexagonal architecture (Ports & Adapters)**
- **Domain-first design**
- **Offline-first, online-when-available**
- **CLI-driven solver development**
- **Web-based visual debugging**
- **Future-proof for solids and slicing**

---

## High-level Architecture

```
┌──────────────┐
│ Sketcher.Cli │
└──────┬───────┘
       │
       │  SignalR (WebSockets)
       │
┌──────▼────────┐        ┌──────────────────┐
│ Sketcher.Server│◄──────► Sketcher.Web     │
│ (Sync + Hub)   │        │ (Blazor WASM +   │
└──────┬────────┘        │  Three.js)       │
       │                 └──────────────────┘
       │
┌──────▼────────────────────────────────────┐
│          Sketcher.Application              │
│  - SketchService                           │
│  - Ports (Repository, Solver, Sync)        │
└──────┬────────────────────────────────────┘
       │
┌──────▼────────────────────────────────────┐
│            Sketcher.Domain                 │
│  - Geometry entities                       │
│  - Constraints                             │
│  - SketchModel                             │
└────────────────────────────────────────────┘
```

---

## Projects in the Solution

### Sketcher.Domain
Pure domain model:
- `Point2`, `Line2`, `Circle2`, `Rectangle2`
- Constraints: `Horizontal`, `Vertical`, `Distance`, `Coincident`
- No UI, no storage, no solver assumptions

### Sketcher.Application
Application layer:
- `SketchService`
- Command-style methods (`AddPoint`, `AddLine`, etc.)
- Query DTOs for rendering
- Ports:
  - `ISketchRepository`
  - `IConstraintSolver`
  - `ISketchSyncClient`

### Sketcher.Solver.Relaxation
Reference constraint solver:
- Iterative relaxation approach
- Deterministic behavior
- Used by CLI, Web, and Server

### Sketcher.Infrastructure.*
Adapters:
- **File**: JSON persistence for CLI
- **Browser**: `localStorage` persistence for WASM
- **SignalR**: WebSocket sync adapter

### Sketcher.Server
ASP.NET Core server:
- SignalR hub (`/sketchHub`)
- In-memory sketch store
- Broadcasts full sketch state
- Optional REST endpoints

### Sketcher.Web
Blazor WebAssembly frontend:
- Three.js renderer
- Offline-first
- Connects to server when available
- Visual debugging of solver behavior
- Toolbar-based interaction (in progress)

### Sketcher.Cli
Command-line interface:
- Fast iteration on geometry and constraints
- Scriptable
- Optional live sync with server

---

## Offline-first + Live Sync

Sketcher is designed so that **everything works offline**.

When the server is available:
- CLI and Web connect via SignalR
- All changes are published as `SketchUpdate`
- Server broadcasts updates to all clients

When offline:
- CLI uses local JSON files
- Web uses browser `localStorage`
- No functionality is lost

---

## Typical Development Workflow

### Start the server
```bash
dotnet run --project Sketcher.Server
```

### Start the web frontend
```bash
dotnet run --project Sketcher.Web
```

### Start the CLI (connected)
```bash
dotnet run --project Sketcher.Cli -- --hub http://localhost:<port>/sketchHub
```

Changes made in the CLI appear instantly in the Web view and vice versa.

---

## Why both CLI and Web?

- **CLI** is the fastest way to:
  - add constraints
  - test solver behavior
  - reproduce edge cases
- **Web** provides:
  - immediate visual feedback
  - intuitive understanding of constraint interactions
  - a future interactive editor

Together they form a **solver development loop**.

---

## UI Direction (Web)

The Web UI is evolving toward a toolbar-driven sketch workflow:
- Select
- Add Point
- Add Line
- Solve
- Save / Load
- Connect / Disconnect

The canvas is intentionally dumb:
- it renders geometry,
- emits click/selection events,
- and never owns domain logic.

---

## Roadmap

Short-term:
- Point selection and dragging
- Constraint glyphs (H/V/D)
- Fix/anchor constraints
- Server-side solving

Mid-term:
- Closed profiles
- Sketch regions
- Extrusion to solids

Long-term:
- Mesh generation
- Slicing
- Toolpath generation

---

## Status

**Active development**

Architecture is stable.  
Features are being layered carefully to preserve correctness and flexibility.

This repository favors **clarity and correctness over shortcuts**.