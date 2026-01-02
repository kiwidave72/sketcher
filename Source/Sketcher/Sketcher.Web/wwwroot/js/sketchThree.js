import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";

let scene, camera, renderer, controls;
let containerEl;

let pointsGroup = new THREE.Group();
let linesGroup = new THREE.Group();
let solidsGroup = new THREE.Group();

function clearGroup(group) {
    // Properly detach children from the group to avoid lingering references
    while (group.children.length) {
        const c = group.children[0];
        group.remove(c);
        if (c.geometry) c.geometry.dispose();
        if (c.material) {
            if (Array.isArray(c.material)) c.material.forEach(m => m.dispose());
            else c.material.dispose();
        }
    }
}

function ensureInit(containerId) {
    if (renderer) return;

    containerEl = document.getElementById(containerId);
    if (!containerEl) throw new Error(`Container not found: ${containerId}`);

    scene = new THREE.Scene();
    scene.background = new THREE.Color(0xf8fafc);

    const w = containerEl.clientWidth || 900;
    const h = containerEl.clientHeight || 600;

    camera = new THREE.PerspectiveCamera(45, w / h, 0.1, 10000);
    camera.position.set(0, -60, 60);
    camera.lookAt(0, 0, 0);

    renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(w, h);
    renderer.setPixelRatio(window.devicePixelRatio ?? 1);
    containerEl.appendChild(renderer.domElement);

    controls = new OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.target.set(0, 0, 0);

    const grid = new THREE.GridHelper(200, 20);
    grid.rotation.x = Math.PI / 2;
    scene.add(grid);

    const axes = new THREE.AxesHelper(20);
    scene.add(axes);

    scene.add(pointsGroup);
    scene.add(linesGroup);
    scene.add(solidsGroup);

    window.addEventListener("resize", () => resize());
    resize();

    animate();
}

function resize() {
    if (!renderer || !camera || !containerEl) return;
    const w = containerEl.clientWidth || 900;
    const h = containerEl.clientHeight || 600;
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
    renderer.setSize(w, h);
}

function animate() {
    requestAnimationFrame(animate);
    controls.update();
    renderer.render(scene, camera);
}

function ensureLight() {
    if (scene.getObjectByName("sketchLight")) return;
    const light = new THREE.DirectionalLight(0xffffff, 1.0);
    light.name = "sketchLight";
    light.position.set(50, -50, 80);
    scene.add(light);
    const amb = new THREE.AmbientLight(0xffffff, 0.35);
    amb.name = "sketchAmbient";
    scene.add(amb);
}

function renderPoints(points) {
    clearGroup(pointsGroup);
    ensureLight();

    const geom = new THREE.SphereGeometry(0.6, 16, 16);
    const mat = new THREE.MeshStandardMaterial();

    for (const p of points) {
        const m = new THREE.Mesh(geom, mat.clone());
        m.position.set(p.x, p.y, 0);
        m.userData = { kind: "point", id: p.id };
        pointsGroup.add(m);
    }
}

function renderLines(lines, pointsById) {
    clearGroup(linesGroup);

    const mat = new THREE.LineBasicMaterial();
    for (const l of lines) {
        const a = pointsById[l.startPointId];
        const b = pointsById[l.endPointId];
        if (!a || !b) continue;

        const g = new THREE.BufferGeometry().setFromPoints([
            new THREE.Vector3(a.x, a.y, 0),
            new THREE.Vector3(b.x, b.y, 0),
        ]);
        const line = new THREE.Line(g, mat.clone());
        line.userData = { kind: "line", id: l.id };
        linesGroup.add(line);
    }
}

export function init(containerId) {
    ensureInit(containerId);
}



function renderSolids(bodies, lines, byPointId, activeSketchId) {
    clearGroup(solidsGroup);

    // Build quick lookup for lines by id
    const lineById = {};
    for (const l of lines) lineById[l.id] = l;

    for (const body of bodies) {
        // Phase 2 (server-built) meshes take priority when present.
        if (body.mesh && body.mesh.positions && body.mesh.indices) {
            const pos = new Float32Array(body.mesh.positions);
            const idx = new Uint32Array(body.mesh.indices);

            const geom = new THREE.BufferGeometry();
            geom.setAttribute('position', new THREE.BufferAttribute(pos, 3));
            geom.setIndex(new THREE.BufferAttribute(idx, 1));
            geom.computeVertexNormals();

            const bodyMat = new THREE.MeshStandardMaterial({
                color: 0x8000ff,
                metalness: 0.05,
                roughness: 0.6,
                polygonOffset: true,
                polygonOffsetFactor: 1,
                polygonOffsetUnits: 1
            });

            const bodyMesh = new THREE.Mesh(geom, bodyMat);
            bodyMesh.userData = { kind: "body", id: body.id };

            const edges = new THREE.EdgesGeometry(geom, 1);
            const wire = new THREE.LineSegments(edges, new THREE.LineBasicMaterial({ color: 0x000000 }));
            wire.renderOrder = 999;
            wire.material.depthTest = true;
            bodyMesh.add(wire);

            solidsGroup.add(bodyMesh);
            continue;
        }

        if (!body.features) continue;
        for (const feat of body.features) {
            if (feat.type !== "extrude") continue;

            // For now, we only have the active sketch's entities in the payload.
            if (activeSketchId && feat.sketchId && feat.sketchId !== activeSketchId) continue;

            const loops = buildLoopsFromSelectedEdges(feat.edgeIds ?? [], lineById, byPointId);
            if (loops.length === 0) continue;

            // Largest abs area = outer; rest = holes
            loops.sort((a, b) => Math.abs(b.area) - Math.abs(a.area));
            const outer = loops[0];
            const holes = loops.slice(1);

            const shape = new THREE.Shape(outer.points.map(p => new THREE.Vector2(p.x, p.y)));
            for (const h of holes) {
                const path = new THREE.Path(h.points.map(p => new THREE.Vector2(p.x, p.y)));
                shape.holes.push(path);
            }

            const depth = feat.height ?? 10;
const geom = new THREE.ExtrudeGeometry(shape, { depth, bevelEnabled: false });

// Purple solid body
const bodyMat = new THREE.MeshStandardMaterial({
    color: 0x8000ff,
    metalness: 0.05,
    roughness: 0.6,
    polygonOffset: true,
    polygonOffsetFactor: 1,
    polygonOffsetUnits: 1
});
const bodyMesh = new THREE.Mesh(geom, bodyMat);
bodyMesh.userData = { kind: "body", id: body.id, featureId: feat.id };

// Black wireframe outline
const edges = new THREE.EdgesGeometry(geom, 1);
const wire = new THREE.LineSegments(edges, new THREE.LineBasicMaterial({ color: 0x000000 }));
wire.renderOrder = 999;
wire.material.depthTest = true;

bodyMesh.add(wire);
solidsGroup.add(bodyMesh);

        }
    }
}

// Graph-based loop builder using point-id topology
function buildLoopsFromSelectedEdges(edgeIds, lineById, byPointId) {
    const adj = new Map(); // pointId -> array of edgeId
    const selected = [];

    for (const eid of edgeIds) {
        const ln = lineById[eid];
        if (!ln) continue;
        selected.push(ln);

        if (!adj.has(ln.startPointId)) adj.set(ln.startPointId, []);
        if (!adj.has(ln.endPointId)) adj.set(ln.endPointId, []);

        adj.get(ln.startPointId).push(eid);
        adj.get(ln.endPointId).push(eid);
    }

    // Must have degree 2 for vertices involved in each loop
    // We'll still attempt to walk, but non-degree2 will likely yield no loop.
    const unused = new Set(edgeIds.filter(eid => lineById[eid]));

    const loops = [];
    while (unused.size > 0) {
        const startEdge = unused.values().next().value;
        const ln0 = lineById[startEdge];
        if (!ln0) { unused.delete(startEdge); continue; }

        const startPt = ln0.startPointId;
        let currentPt = startPt;
        let currentEdge = startEdge;
        let prevEdge = null;

        const orderedPts = [];
        const orderedEdges = [];

        for (let guard = 0; guard < 10000; guard++) {
            unused.delete(currentEdge);
            orderedEdges.push(currentEdge);
            orderedPts.push(currentPt);

            const ln = lineById[currentEdge];
            const nextPt = (ln.startPointId === currentPt) ? ln.endPointId : ln.startPointId;

            const incident = adj.get(nextPt) ?? [];
            const nextEdges = incident.filter(eid => eid !== currentEdge && eid !== prevEdge && lineById[eid] && unused.has(eid));
            const allNext = incident.filter(eid => eid !== currentEdge && lineById[eid]);

            prevEdge = currentEdge;
            currentPt = nextPt;

            if (currentPt === startPt) break;
            if (nextEdges.length > 0) currentEdge = nextEdges[0];
            else if (allNext.length > 0) currentEdge = allNext[0]; // fallback
            else break;
        }

        // We closed when currentPt returned to startPt.
        // orderedPts already contains the polygon vertices; don't require the last entry == startPt.
        if (orderedPts.length >= 3 && currentPt === startPt) {
            const pts = orderedPts.map(pid => byPointId[pid]).filter(Boolean);
            if (pts.length >= 3) {
                loops.push({ points: pts, area: signedArea(pts) });
            }
        }

    }
    return loops;
}

function signedArea(pts) {
    let a = 0;
    for (let i = 0; i < pts.length; i++) {
        const p0 = pts[i];
        const p1 = pts[(i + 1) % pts.length];
        a += p0.x * p1.y - p1.x * p0.y;
    }
    return a * 0.5;
}
export function setSketch(renderPayload) {
    const pts = renderPayload.points ?? [];
    const lns = renderPayload.lines ?? [];
    const bodies = renderPayload.bodies ?? [];
    const activeSketchId = renderPayload.activeSketchId ?? null;

    const byId = {};
    for (const p of pts) byId[p.id] = p;

    renderPoints(pts);
    renderLines(lns, byId);
    renderSolids(bodies, lns, byId, activeSketchId);
}
