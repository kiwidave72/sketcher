# Sketcher

Sketcher is an experimental **parametric sketching and solid modelling kernel** designed to explore how a modern CAD-style system can be built with:

- clean architecture
- multiple front-ends
- live synchronization
- strong separation between **intent**, **derived geometry**, and **UI**

Sketcher began as a **2D parametric sketch kernel**, but has now grown to include:

- closed-profile detection
- feature-based solid modelling (extrude)
- a document / component / sketch / body hierarchy
- real-time 3D rendering via Three.js

This is **not a UI-first project**.  
The **kernel and object model are the product**.

---

## Key Ideas

- **Hexagonal architecture (Ports & Adapters)**
- **Domain-first design**
- **Offline-first, online-when-available**
- **CLI-driven solver and feature development**
- **Web-based visual debugging**
- **Feature-based modelling (history, not meshes)**
- **Future-proof for real CAD kernels and slicing**

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
│  - Document orchestration                  │
│  - Feature creation                        │
│  - Ports (Repository, Solver, Sync)        │
└──────┬────────────────────────────────────┘
       │
┌──────▼────────────────────────────────────┐
│            Sketcher.Domain                 │
│  - CadDocument                             │
│  - Component                               │
│  - Sketch / SketchModel                   │
│  - Body / Feature                          │
│  - Geometry entities                       │
│  - Constraints                             │
└────────────────────────────────────────────┘
```

---

## Core Object Model

### CadDocument

The root of the model. A document contains:

- Components (assembly hierarchy)
- Sketches
- Bodies
- Active sketch reference

The entire modelling state is serialised as a single document.

---

### Component

Represents an assembly node.

A component may own:

- child components
- sketches
- bodies

This enables future support for assemblies and instancing.

---

### Sketch

A sketch represents **2D parametric intent**.

- Belongs to a component
- Owns a `SketchModel`
- Currently lives in the XY plane (extensible later)

---

### SketchModel

Pure sketch data:

- Geometry entities:
  - `Point2`
  - `Line2`
  - `Circle2`
  - `Rectangle2`
- Constraints:
  - Horizontal
  - Vertical
  - Distance
  - Coincident

The sketch model has **no rendering or solver dependencies**.

---

### Body

A body represents a **solid**.

Important:
- A body does **not** store geometry
- A body stores **features**
- Geometry is **derived** by replaying features

This mirrors real parametric CAD systems.

---

### Feature System

Features capture **modelling intent**.

Currently implemented:

#### ExtrudeFeature

- References a sketch
- References selected sketch edges
- Stores extrusion height
- Rebuilds geometry on demand

Future features (planned):
- Cut extrude
- Revolve
- Fillet / chamfer
- Boolean operations

---

## Feature-Based Modelling Workflow

1. Create sketch geometry (points / lines)
2. Form a closed loop
3. Create an `ExtrudeFeature`
4. Renderer reconstructs loops and generates a solid

Meshes are **never authoritative** — they are a view.

---

## Web Renderer (Three.js)

The web frontend uses **Three.js** as a temporary geometry kernel.

### Rendering behaviour

- Sketch geometry:
  - points rendered as small spheres
  - lines rendered as line segments
- Bodies:
  - purple solid mesh
  - black wireframe outline (edge overlay)

### Important properties

- Geometry is regenerated every update
- Rendering is stateless
- No meshes are persisted in the document

This allows a future swap to a real CAD kernel (OpenCascade / CGAL / WASM).

---

## CLI

The CLI is a REPL-style interface that drives the same model as the Web UI.

### Geometry Commands

```text
point <x> <y>
line <pointId1> <pointId2>
rectangle <width> <height>
```

`rectangle`:
- creates 4 points
- creates 4 lines
- forms a closed outer path

### Extrude

```text
extrude <height> <lineId1> <lineId2> <lineId3> <lineId4>
```

Creates:
- a new body
- an `ExtrudeFeature` referencing the sketch

### Document Commands

```text
dump
save <file.json>
load <file.json>
reset
```

- `reset` clears the document to a blank state
- `dump` prints the full document hierarchy

---

## Offline-first + Live Sync

Sketcher works **fully offline**.

When online:
- CLI and Web connect via SignalR
- All changes are broadcast as full document updates

When offline:
- CLI uses local JSON files
- Web uses browser localStorage

No functionality is lost.

---

## Why both CLI and Web?

- **CLI**
  - fastest way to develop constraints and features
  - scriptable and deterministic
- **Web**
  - immediate visual feedback
  - 3D inspection of results
  - interactive debugging

Together they form a **modelling development loop**.

---

## Roadmap

Short-term:
- Sketch selection and dragging
- Sketch region highlighting
- Cut extrude
- Feature editing / regeneration

Mid-term:
- Revolve feature
- Face selection
- Visibility toggles
- Export (STL)

Long-term:
- Real CAD kernel backend
- Mesh generation
- Slicing
- Toolpath generation

---

## Design Philosophy

> **Meshes are views, not truth.**

The document stores **intent**.  
Geometry is **derived**, replaceable, and never authoritative.

Sketcher favors **clarity, correctness, and architecture** over shortcuts.
