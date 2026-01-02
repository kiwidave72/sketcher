# Sketcher

Sketcher is an experimental **parametric sketching and solid modelling kernel** designed to explore how a modern CAD-style system can be built with:

- clean architecture  
- multiple front-ends  
- live synchronization  
- a strong separation between **intent**, **derived geometry**, and **UI**

This is **not a UI-first project**.  
The **kernel, document model, and rebuild pipeline are the product**.

---

## What Sketcher Is Today (Phase 2)

Sketcher is currently in **Phase 2: Feature-aware solid modelling with deterministic rebuilds**.

In Phase 2:

- Modelling intent is captured as **features**
- Solids are rebuilt by **replaying the feature history**
- **Join and Cut extrusions are real operations**
- Geometry is derived, not edited
- Meshes are still used — but **only as a derived representation**

This allows Sketcher to behave like a real CAD system *without yet requiring a full CAD kernel*.

---

## Phase Roadmap Overview

### Phase 1 — Sketch + Visual Extrude (completed)
- 2D parametric sketching
- Closed profile detection
- Visual extrude in the renderer
- No real solid operations

### **Phase 2 — Feature-aware solids (current)**
- Feature history (extrude join / cut)
- Deterministic rebuild on every change
- Server-side geometry evaluation
- Cut operations remove material
- Undo via feature replay
- Meshes are derived, never authoritative

### Phase 3 — True CAD kernel (future)
- B-Rep / topology-aware solids
- Stable face and edge identities
- Fillets, chamfers, shells
- Sketch-on-face
- Robust booleans

Phase 3 will be introduced **incrementally** and **behind a feature toggle**, allowing Phase 2 to remain a stable, working system while Phase 3 is developed over time.

---

## Key Ideas

- **Hexagonal architecture (Ports & Adapters)**
- **Domain-first design**
- **Offline-first, online-when-available**
- **CLI-driven solver and feature development**
- **Web-based visual debugging**
- **Feature-based modelling (history, not meshes)**
- **Rebuild, don’t mutate**
- **Future-proof for real CAD kernels and slicing**

---

## Design Philosophy

> **Meshes are views, not truth.**

The document stores **intent**.  
Features describe **what** was done, not **how geometry was mutated**.  
Geometry is derived, replayable, and replaceable.

Sketcher favors **clarity, correctness, and architecture** over shortcuts.
