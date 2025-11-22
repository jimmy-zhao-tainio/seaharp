# CURRENTSTATUS-gpt5.1-codex.md

This file is a quick on-ramp for future Codex / GPT sessions. It summarizes the **current intersection pipeline** and a **priority-ordered roadmap** so an agent can get productive in one pass.

---

## 0. One-paragraph TL;DR

The repo implements a layered, robust triangle–triangle intersection core. For two meshes A and B, it can:

- Classify intersecting triangle pairs (`IntersectionSet`),
- Build local per-pair geometry (`PairFeatures`),
- Build a global intersection graph with deduplicated vertices/edges (`IntersectionGraph`),
- Build per-triangle indices (`TriangleIntersectionIndex`),
- Build mesh-local topology for mesh A (`MeshATopology`),
- Regularize mesh-A intersection curves into closed, 2‑regular cycles with small noise trimmed and small gaps optionally closed (`IntersectionCurveRegularizer` + `IntersectionCurve`),
- Exercise the entire stack on realistic shapes (e.g., sphere–sphere) via tests and a diagnostics console app.

What it does **not** do yet: cut triangles along these curves, classify surface patches as in/out for booleans, assemble result meshes, or integrate integer-grid (Z³) snapping.

---

## 1. Current intersection pipeline (code-oriented)

### 1.1 Pair classification: `IntersectionSet`

- File: `Kernel/IntersectionSet.cs`
- Responsibilities:
  - Builds a bounding-box tree over mesh B.
  - For each triangle in A, queries candidate triangles in B.
  - Classifies each intersecting pair as `None | Point | Segment | Area`.
  - Stores `Intersection { TriangleIndexA, TriangleIndexB, Type }`.
- Important: No coordinates here, just **which triangles intersect and how**.

### 1.2 Per-pair geometry: `PairFeatures`

- File: `Kernel/PairFeatures.cs`
- For one `(triangleA, triangleB)` pair:
  - `PairFeatures` stores:
    - `Intersection` (from `IntersectionSet`),
    - `Vertices: IReadOnlyList<PairVertex>` (with barycentric on A/B + pair-local `VertexId`),
    - `Segments: IReadOnlyList<PairSegment>` between those pair-local vertices.
- Factory:
  - Non-coplanar path:
    - Samples 3D intersection points via `PairIntersectionMath`.
    - Deduplicates in world space.
    - Converts to barycentric on A and B.
    - Degrades geometry to stay consistent with `IntersectionType`.
  - Coplanar path:
    - Works in 2D projection,
    - Can build ordered polygon loops for area overlaps.

### 1.3 Global graph: `IntersectionGraph`

- File: `Kernel/IntersectionGraph.cs`
- `IntersectionGraph.FromIntersectionSet(set)`:
  - Builds `PairFeatures` per intersecting pair.
  - Constructs **global vertices**:
    - For every `PairVertex`, reconstructs world position from barycentric on triangle A.
    - Quantizes using `Tolerances.TrianglePredicateEpsilon`.
    - Deduplicates to `IntersectionVertexId`.
  - Constructs **global edges**:
    - For every `PairSegment`, maps pair-local endpoints to global ids.
    - Drops degenerate segments.
    - Normalizes as undirected `(minId, maxId)` and dedups to `IntersectionEdgeId`.
- Exposes:
  - `IntersectionSet IntersectionSet`
  - `IReadOnlyList<(IntersectionVertexId Id, RealPoint Position)> Vertices`
  - `IReadOnlyList<(IntersectionEdgeId Id, IntersectionVertexId Start, IntersectionVertexId End)> Edges`
  - `IReadOnlyList<PairFeatures> Pairs`

### 1.4 Per-triangle vertex index: `TriangleIntersectionIndex`

- File: `Kernel/TriangleIntersectionIndex.cs`
- `TriangleIntersectionIndex.Build(graph)`:
  - For each triangle in A and B, builds `TriangleIntersectionVertex[]`:
    - `VertexId` (global),
    - `Barycentric` on that triangle.
  - Uses the same world-space quantization scheme as `IntersectionGraph` to ensure consistency.
- Guarantees checked by tests:
  - Every triangle marked as intersecting has per-triangle vertices.
  - Every per-triangle vertex maps back to a global vertex with matching world position within tolerance.

### 1.5 Mesh-local topology (currently mesh A only): `MeshATopology`

- File: `Kernel/MeshATopology.cs`
- `MeshATopology.Build(graph, index)`:
  - For each triangle in A, builds a set of global vertex ids that lie on it.
  - For each global edge, marks it as lying on a triangle if both endpoints are in that triangle’s vertex set.
  - Produces:
    - `TriangleEdges: IReadOnlyList<IntersectionEdgeId[]>` per triangle in A.
    - `Edges: IReadOnlyList<IntersectionEdgeId>` = all global edges that touch at least one triangle in A.
    - `VertexEdges: IReadOnlyDictionary<IntersectionVertexId, IReadOnlyList<IntersectionEdgeId>>` adjacency restricted to mesh A.
    - `Loops: IReadOnlyList<IntersectionVertexId[]>` raw per-component vertex chains (not yet cleaned).

### 1.6 Regularized intersection curves: `IntersectionCurve` + `IntersectionCurveRegularizer`

- Files:
  - `Kernel/IntersectionCurve.cs`
  - `Kernel/IntersectionCurveRegularizer.cs`

#### `IntersectionCurve`

- Represents one closed intersection curve on a mesh:
  - `ImmutableArray<IntersectionVertexId> Vertices`  
    - First must equal last (closed cycle).
  - `ImmutableArray<IntersectionEdgeId> Edges`  
    - Length must be `Vertices.Length - 1`.
    - Ordered correspondingly to vertex segments.
  - `ImmutableArray<bool> IsClosureEdge`  
    - Flags edges that are **synthetic closure edges** added by the regularizer.
  - `double TotalLength`  
    - Sum of world-space lengths of edges in this curve.
- Constructor enforces invariants above.

#### `IntersectionCurveRegularizer`

- API:
  - `public static IntersectionCurveRegularizer.Result RegularizeMeshA(IntersectionGraph graph, MeshATopology topology)`

- `Result`:
  - `IReadOnlyList<IntersectionCurve> Curves`
  - `IReadOnlyList<ComponentStats> Components` (per connected component on mesh A)

- `ComponentStats`:
  - `Index`
  - `VertexCount`, `EdgeCount`
  - `Degree1Count`, `Degree2Count`, `DegreeMoreCount`
  - `TotalLength`, `MedianEdgeLength`
  - `Classification: StrongLoopCandidate | TinyNoise | Ambiguous`

- Logic (per connected component on mesh A):
  1. Build component from `topology.VertexEdges` restricted to `topology.Edges`.
  2. Compute vertex degrees (within component edges) and edge length statistics.
  3. Classify:
     - **StrongLoopCandidate** if:
       - `degreeMoreCount == 0`
       - `degree1Count <= 2`
       - `edgeCount >= 4`
       - `MedianEdgeLength > 0`
       - `TotalLength >= 4 * MedianEdgeLength`
     - **TinyNoise** if:
       - `edgeCount <= 3`
       - `MedianEdgeLength > 0`
       - `TotalLength <= 2 * MedianEdgeLength`
     - **Ambiguous** otherwise.
  4. For **StrongLoopCandidate** components:
     - If `degree1Count == 0`:
       - Treat as 2-regular graph; walk edges to extract a single closed cycle that uses all component edges.
       - Emit it as an `IntersectionCurve` (no synthetic edges).
     - If `degree1Count == 2`:
       - Identify the two degree-1 endpoints.
       - Measure their world-space distance `d`.
       - Threshold:
         - `median = MedianEdgeLength`, `total = TotalLength`.
         - If `median > 0 && total > 0`, use `threshold = max(3 * median, 0.25 * total)`.
       - If `d <= threshold`:
         - Add a synthetic closure edge between the endpoints (negative `IntersectionEdgeId.Value`).
         - Run the same cycle extraction over the augmented component.
         - Emit the resulting closed cycle as an `IntersectionCurve` with the synthetic edge flagged in `IsClosureEdge`.
     - Otherwise ignore (but keep `ComponentStats` for diagnostics).
  5. TinyNoise and Ambiguous components:
     - Never emit curves, but keep stats.

The old `IntersectionCurveCleaner` has been removed; `IntersectionCurveRegularizer` is the canonical higher-level layer now.

---

## 2. Tests and diagnostics

### 2.1 Core tests

- File: `Kernel.Tests/IntersectionGraphTests.cs`
  - Exercise:
    - `IntersectionGraph.FromIntersectionSet` on synthetic examples.
    - `TriangleIntersectionIndex.Build`.
    - Sphere–sphere intersection:
      - Ensures per-triangle vertices are consistent with global positions.
      - Builds `MeshATopology`.
      - Uses `IntersectionCurveRegularizer.RegularizeMeshA` and asserts existence of at least one closed, 2‑regular `IntersectionCurve`.

### 2.2 Regularizer-specific tests

- File: `Kernel.Tests/IntersectionCurveRegularizerTests.cs`
  - Builds synthetic graphs via reflection (no dependency on real geometry):
    - Pure loop (square) → exactly one curve covering all edges, no synthetic closures.
    - Loop plus tiny hanging chain → chain is classified as `TinyNoise`, loop is still returned as a curve.
    - Pure chain → classified as `TinyNoise`, no curves returned.

### 2.3 Diagnostics console

- File: `Diagnostics.Kernel/Program.cs`
  - Runs sphere–sphere intersection using `World.Shapes.Sphere`.
  - Prints:
    - Counts of triangles, intersecting pairs, global vertices/edges, mesh-A edges and components.
    - Degree histogram on mesh A.
    - Per-component stats from `IntersectionCurveRegularizer`.
    - Summary of each regularized curve (vertex/edge counts, length, closedness, presence of synthetic closure edges).

---

## 3. Roadmap (priority-ordered for future agents)

This is a suggested sequence; each bullet is intentionally concrete.

### 3.1 Mirror topology & curves to mesh B

- Add `MeshBTopology` analogous to `MeshATopology`:
  - Same API shape but restricted to triangles of mesh B.
- Add `IntersectionCurveRegularizer.RegularizeMeshB(...)`:
  - Same logic as `RegularizeMeshA`, but over `MeshBTopology`.
- Extend diagnostics/tests to:
  - Report component stats and curves on both meshes A and B.

### 3.2 Per-triangle cutting along intersection curves

Goal: for each triangle in A (and B), cut it into smaller patches along the intersection segments that lie on it.

- For each triangle in mesh A:
  - Collect:
    - Original triangle vertices.
    - All intersection vertices on it (`TriangleIntersectionIndex`).
    - All intersection edges lying on it (`MeshATopology.TriangleEdges`).
  - In the triangle’s local 2D parameterization:
    - Build a planar graph: nodes = triangle + intersection vertices, edges = intersection segments + triangle edges.
    - Compute a planar subdivision into polygonal faces.
  - Triangulate each face (simple ear clipping or fan is fine for now).
- Output per-triangle patches that exactly cover the original triangle and meet along shared edges.

### 3.3 Patch classification (boolean labeling)

Goal: label each patch as belonging to:

- `A ∩ B`, `A \ B`, `B \ A`, `A ∪ B` (depending on the operation).

High-level approach:

- For each patch of A:
  - Pick an interior sample point in world space (e.g., centroid of its vertices).
  - Test whether that point is inside mesh B:
    - Use a robust point-in-closed-mesh test (ray casting or winding number) built on top of the existing BVH infrastructure.
  - Based on inside/outside and whether the patch borders intersection curves, classify it for intersection/union/difference.
- Do the symmetric process for patches of B where needed.

### 3.4 Output mesh assembly

Goal: assemble a watertight result mesh from classified patches.

- Decide on an output mesh representation (likely in `Topology` or `World`):
  - Vertex list (world positions),
  - Triangle list (indices into the vertex list).
- Implementation notes:
  - Reuse global intersection vertices and original mesh vertices as much as possible.
  - Deduplicate new vertices via a quantized key (similar to `IntersectionGraph`).
  - Ensure:
    - Every interior edge is shared by exactly two triangles.
    - No cracks exist along intersection curves.

### 3.5 Integer grid (Z³) snapping

After continuous-geometry booleans work:

- Introduce an integer grid representation (`Point`-like but with higher precision if needed) and a snapping function:
  - Map world-space doubles → Z³ grid.
  - Ensure consistent snapping for coincident vertices and edges.
- Rebuild:
  - Intersection vertices/edges in Z³.
  - Cut patches with snapped coordinates.
- Add tests to verify that snapping:
  - Preserves topology (no new self-intersections/cracks),
  - Keeps distances within acceptable error bounds.

### 3.6 High-level boolean APIs

Finally, wrap the whole pipeline:

- Candidate APIs:
  - `Mesh BooleanUnion(Mesh a, Mesh b)`
  - `Mesh BooleanIntersection(Mesh a, Mesh b)`
  - `Mesh BooleanDifference(Mesh a, Mesh b)`
  - Or shape-oriented wrappers in `World` (e.g., `Shape BooleanUnion(Shape a, Shape b)`).
- Implementation:
  - Convert shapes to meshes if needed.
  - Run:
    - IntersectionSet → PairFeatures → IntersectionGraph → TriangleIntersectionIndex,
    - Mesh[A/B]Topology → IntersectionCurveRegularizer,
    - Cutting → Patch classification → Mesh assembly.
  - Return a new mesh/shape.

---

## 4. Tips for future agents

- **Don’t touch**: public APIs of `IntersectionGraph`, `TriangleIntersectionIndex`, `MeshATopology` unless explicitly requested.
- **Prefer**:
  - Adding new layers / helpers (like `IntersectionCurveRegularizer`) over mutating core math types.
  - Synthetic tests (small graphs) where possible; use `World.Shapes.*` for realistic integrations.
- **When in doubt**:
  - Start by reading:
    - `Kernel/IntersectionGraph.cs`
    - `Kernel/MeshATopology.cs`
    - `Kernel/IntersectionCurveRegularizer.cs`
    - `Kernel.Tests/IntersectionGraphTests.cs`
    - `Kernel.Tests/IntersectionCurveRegularizerTests.cs`
  - Then align your changes with the roadmap in section 3.

