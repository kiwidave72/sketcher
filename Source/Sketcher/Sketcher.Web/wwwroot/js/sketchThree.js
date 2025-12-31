import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";


let scene, camera, renderer, controls;
let containerEl;
let animHandle = null;

let pointsGroup = new THREE.Group();
let linesGroup = new THREE.Group();
let labelsGroup = new THREE.Group(); // optional (we’ll leave it empty for now)

function clearGroup(group) {
  while (group.children.length) {
    const c = group.children.pop();
    if (c.geometry) c.geometry.dispose();
    if (c.material) c.material.dispose();
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

  // Grid on XY plane
  const grid = new THREE.GridHelper(200, 20);
  grid.rotation.x = Math.PI / 2; // make it lie on XY (Three’s default grid is XZ)
  grid.position.z = 0;
  scene.add(grid);

  // Axes
  const axes = new THREE.AxesHelper(20);
  scene.add(axes);

  scene.add(pointsGroup);
  scene.add(linesGroup);
  scene.add(labelsGroup);

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
  animHandle = requestAnimationFrame(animate);
  controls.update();
  renderer.render(scene, camera);
}

// points: [{ id, x, y }]
function renderPoints(points) {
  clearGroup(pointsGroup);

  const geom = new THREE.SphereGeometry(0.6, 16, 16);
  const mat = new THREE.MeshStandardMaterial();

  // Simple light
  if (!scene.getObjectByName("sketchLight")) {
    const light = new THREE.DirectionalLight(0xffffff, 1.0);
    light.name = "sketchLight";
    light.position.set(50, -50, 80);
    scene.add(light);

    const amb = new THREE.AmbientLight(0xffffff, 0.35);
    amb.name = "sketchAmbient";
    scene.add(amb);
  }

  for (const p of points) {
    const m = new THREE.Mesh(geom, mat.clone());
    m.position.set(p.x, p.y, 0);
    m.userData = { kind: "point", id: p.id };
    pointsGroup.add(m);
  }
}

// lines: [{ id, startPointId, endPointId }]
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

export function setSketch(renderPayload) {
  // renderPayload: { points: [...], lines: [...], constraints: [...] }
  const pts = renderPayload.points ?? [];
  const lns = renderPayload.lines ?? [];

  // Create lookup map
  const byId = {};
  for (const p of pts) byId[p.id] = p;

  renderPoints(pts);
  renderLines(lns, byId);

  // constraints visualization later (labels / glyphs)
}
