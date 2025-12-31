# Sketch – Parametric Sketch Kernel (C# / WASM / CLI)

This repository contains a **parametric 2D sketch kernel** written in **C#**, designed using **hexagonal (ports & adapters) architecture** so the same core logic can be reused across:

- a **CLI tool** (headless, scriptable)
- a **Blazor WebAssembly application** (future GUI with web-based 3D rendering)
- potential future desktop applications

The goal is to build a **CAD-style sketch engine** where *design intent* is preserved via constraints, not procedural command history.

---

## Core Capabilities

### Geometry
- Points
- Lines
- Circles
- Rectangles

### Constraints
- Horizontal
- Vertical
- Coincident
- Distance

Constraints are **declarative**: they describe what must be true, not how to achieve it.

---

## Architectural Principles

### 1. Hexagonal Architecture
The system is split into:

- **Domain** – pure geometry and constraints
- **Application** – use cases and orchestration
- **Solver** – pluggable constraint solving engines
- **Adapters** – CLI, Web (WASM), persistence, rendering

The domain has **no dependency** on UI, storage, or execution environment.

---

### 2. Single Source of Truth
- The **domain model** is authoritative
- Solvers compute *derived* geometry
- UIs only visualize results

The same model and solver run in:
- CLI
- WebAssembly
- Tests

---

### 3. Declarative Persistence
Sketches persist:
- entities
- constraints
- IDs and relationships

They **do not** persist:
- CLI commands
- solver iterations
- UI state

This ensures long-term compatibility and safe solver evolution.

---

## Project Structure
Sketch.Domain
Sketch.Application
Sketch.Solver.Abstractions
Sketch.Solver.Relaxation
Sketch.Infrastructure.File
Sketch.Infrastructure.Browser
Sketch.Cli
Sketch.Web
Sketch.Tests



### Project Responsibilities

| Project | Responsibility |
|------|----------------|
| `Sketch.Domain` | Entities, constraints, sketch model |
| `Sketch.Application` | Use cases, ports, DTO queries |
| `Sketch.Solver.Abstractions` | Solver result types |
| `Sketch.Solver.Relaxation` | Iterative relaxation solver |
| `Sketch.Infrastructure.File` | JSON file persistence |
| `Sketch.Infrastructure.Browser` | Browser `localStorage` persistence |
| `Sketch.Cli` | CLI adapter |
| `Sketch.Web` | Blazor WASM host |
| `Sketch.Tests` | Solver and model tests |

---

## Solver (Current)

- Iterative **relaxation-based solver**
- Applies constraints incrementally until convergence
- Designed to be replaceable (CPU, GPU, WebGPU, native, etc.)
- Floating-point tolerance based (`epsilon`)

Current solver enforces:
- equal Y for horizontal lines
- equal X for vertical lines
- distance between points
- coincident points

⚠️ No fixed/anchored geometry yet — sketches can translate freely (expected at this stage).

---

## Persistence Details

### JSON Serialization
- Uses `System.Text.Json`
- Polymorphic domain types via:
  - `[JsonPolymorphic]`
  - `[JsonDerivedType]`
- Abstract base types (`SketchEntity`, `Constraint`) deserialize correctly
- Computed properties (e.g. `EntityIds`) are **not serialized**
- `SketchModel.Entities` and `Constraints` are settable to ensure reliable loading

Saved files represent **design intent**, not execution history.

---

## CLI Usage Example

```text
> point 0 0
> point 10 5
> line <pointA> <pointB>
> horizontal <lineId>
> solve
> dump
> save example.json
> load example.json

The CLI is intended as:
- a development harness
- a batch solver
- a regression test tool
- a headless CAD kernel shell

WebAssembly Support

Core libraries are WASM-safe

Blazor WebAssembly hosts the same domain and solver

Browser persistence via localStorage

Rendering is delegated to JavaScript (future Three.js / Babylon.js)

C# owns:

geometry

constraints

solving

JavaScript owns:

rendering

user interaction

Testing

xUnit tests validate solver behavior

Same solver code is tested as used by CLI and WASM

Saved sketches can be reused as regression fixtures

Known Limitations (Planned Work)

No Fix / anchored constraint yet

No move-point command

No degrees-of-freedom or over-constraint detection

No SVG/DXF export yet

Roadmap (Recommended Order)

Add FixPoint constraint

Respect fixed geometry in solver

Add move-point command

Auto-solve on edits

SVG export adapter

Web-based interactive UI

Philosophy

This project treats the sketch engine as a CAD kernel, not a drawing tool.

Constraints express intent

Solvers derive geometry

Adapters remain thin

The model remains portable across environments

This allows the same sketch to be:

created in a CLI

edited in a browser

solved headlessly in CI

rendered in a future GUI