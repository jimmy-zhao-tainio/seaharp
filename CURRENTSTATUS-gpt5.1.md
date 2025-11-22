# CURRENTSTATUS.md

## 0. High-level snapshot

You now have a pretty serious intersection core:

- Robust triangle–triangle classification and sampling.
- A layered pipeline from pairwise intersections → global intersection graph → per-triangle topology.
- A **regularization layer** that turns messy intersection graphs on a mesh into one or more closed, 2-regular intersection curves, with tiny noise trimmed and small gaps repaired in a controlled way.
- An integration path using real `Sphere` meshes (icosahedron-based) that exercises the whole stack.

You do **not** yet:

- Cut triangles into patches along these curves.
- Reconstruct watertight boolean result meshes.
- Integrate the Z³ / integer grid snapping layer.
- Expose a “`Shape A ∪ Shape B`” style API.

The current work is the “intersection & canonical curves” phase. Next phase is “cut surfaces & classify regions”; after that comes Z³ and user-level boolean APIs.

---

## 1. Current architecture

### 1.1 Core geometry and predicates

At the bottom you have:

- `Point`, `Vector`, `Triangle`, barycentric solvers.
- Geometric predicates:
  - Orientation,
  - Coplanarity / non-coplanarity,
  - Triangle–triangle intersection predicates.

This layer is assumed stable and is used everywhere else.

### 1.2 Intersection classification: `IntersectionSet`

For two triangle lists A and B:

- `IntersectionSet`:
  - Builds a BVH / bounding-box tree over B.
  - For each triangle in A:
    - Queries candidate triangles in B.
    - Uses intersection predicates to classify each pair as `None`, `Point`, `Segment`, or `Area`.
  - Stores a sparse array of `Intersection { TriangleIndexA, TriangleIndexB, Type }`.

Key idea: **no coordinates yet**, just “which triangles intersect and how (0D/1D/2D)”.

### 1.3 Per-pair geometry: `PairFeatures`

For a single intersecting pair `(iA, iB)`:

- `PairFeatures` holds:
  - `Intersection` (the `IntersectionSet` entry),
  - `Vertices`: `PairVertex` objects with:
    - barycentric on A,
    - barycentric on B,
    - a **pair-local** `VertexId` (0..N−1),
  - `Segments`: `PairSegment`s between those pair-local vertices.

The factory:

- Non-coplanar path:
  - Computes 3D intersection samples.
  - Deduplicates in world space.
  - Converts to barycentric on A and B.
  - Degrades to match `IntersectionType` (pure point, single segment, or minimal area loop).

- Coplanar path:
  - Works in 2D projection, same pattern, but can build polygonal loops for area intersections.

At this layer everything is still **per pair**, no shared global vertices yet.

### 1.4 Global graph: `IntersectionGraph`

`IntersectionGraph.FromIntersectionSet` now does:

- Builds all `PairFeatures` from the `IntersectionSet`.
- Computes **global vertices**:
  - For every `PairVertex`:
    - Computes world position from barycentric on triangle A.
    - Quantizes the position using `TrianglePredicateEpsilon`.
    - Deduplicates by quantized key → `IntersectionVertexId`.
  - Builds `Vertices: ImmutableArray<(IntersectionVertexId, RealPoint)>`.

- Computes **global edges**:
  - For every `PairSegment`:
    - Maps its pair-local endpoints to global `IntersectionVertexId`s.
    - Discards degenerate segments.
    - Normalizes edges as undirected `(minId, maxId)`.
    - Deduplicates across all pairs → `IntersectionEdgeId`s.
  - Builds `Edges: ImmutableArray<(IntersectionEdgeId, StartVertexId, EndVertexId)>`.

So `IntersectionGraph` is your **global intersection graph** in world space, built from all local pair features.

### 1.5 Per-triangle view: `TriangleIntersectionIndex`

`TriangleIntersectionIndex.Build(graph)`:

- For each triangle in A and B:
  - Uses barycentric data from `PairFeatures` + world deduplication rules.
  - Produces `TriangleIntersectionVertex[]`:
    - `VertexId` (global),
    - `Barycentric` on that triangle.
- Ensures consistency:
  - Every triangle referenced by `IntersectionSet.Intersections` has non-empty per-triangle vertices.
  - Every per-triangle vertex maps back to a global vertex whose world position matches the barycentric reconstruction (within a small tolerance).

This is your **“which global intersection vertices lie on which triangles, and how”** map.

### 1.6 Mesh-local topology: `MeshATopology`

For mesh A (and analogously possible for B):

- `MeshATopology.Build(graph, index)`:
  - For each triangle in A:
    - Builds a fast lookup of `IntersectionVertexId`s on that triangle.
  - For each global edge in `IntersectionGraph.Edges`:
    - Recognizes that edge lies on triangle i if both endpoints are in triangle i’s vertex set.
  - Produces:
    - `TriangleEdges[i]`: all `IntersectionEdgeId`s lying on triangle i.
    - `Edges`: all global edges that touch at least one triangle in A.
    - `VertexEdges`: adjacency:
      - `IntersectionVertexId → [IntersectionEdgeId …]` restricted to edges on A.
    - A coarse set of **components** (sequences of vertices) via a naive component walk.

This is the **raw intersection graph restricted to mesh A**.

### 1.7 Strict cleaner: `IntersectionCurveCleaner` (pure 2-core)

You added a very strict cleaner that:

- Computes the 2-core (peeling off degree-0/1 vertices) of each component.
- Only accepts a component if:
  - the remaining subgraph is 2-regular (degree 2 at every vertex),
  - it can extract a closed cycle that uses all remaining edges exactly once.

On the sphere–sphere case, this returns **no loops**, because the raw graph for the spheres has small gaps and never forms a perfect 2-regular subgraph. This is useful as a diagnostic tool and a spec for “ideal curves”, but not directly usable for boolean meshing.

### 1.8 Regularizer: `IntersectionCurveRegularizer`

This is the important new layer.

`IntersectionCurveRegularizer.RegularizeMeshA`:

- Starts from `IntersectionGraph` + `MeshATopology`.
- Rebuilds connected components of the mesh-A graph.
- For each component:

  1. **Component stats**:
     - `vertexCount`, `edgeCount`.
     - Degree histogram: `deg1`, `deg2`, `degMore`.
     - Edge lengths, `totalLength`, `medianEdgeLength`.

  2. **Classification**:
     - `TinyNoise` if:
       - `edgeCount ≤ 3` and  
       - `totalLength ≤ 2 * medianEdgeLength`.
     - `StrongLoopCandidate` if:
       - `degMore == 0`,
       - `deg1 ≤ 2`,
       - `edgeCount ≥ 4`,
       - `totalLength ≥ 4 * medianEdgeLength`.
     - Else, `Ambiguous`.

  3. **Regularization for strong candidates**:
     - If `deg1 == 0`:
       - Treat as 2-regular, attempt to extract a simple cycle using **all** edges.
     - If `deg1 == 2`:
       - Identify the two degree-1 endpoints.
       - If their world-space distance is small relative to component scale:
         - Add a **synthetic closure edge** between them (negative id, tracked locally).
         - Now all vertices are degree-2.
         - Extract a simple cycle using all edges (original + synthetic).
       - If too far apart, skip.

- For each successful component, produces an `IntersectionCurve`:

  - `Vertices`: ordered `IntersectionVertexId`s, closed (`first == last`).
  - `Edges`: ordered `IntersectionEdgeId`s, including at most one synthetic closure id.
  - `TotalLength`: sum of geodesic length along the curve.
  - A flag for whether a synthetic closure was used.

On the two-sphere test:

- Diagnostics show:
  - Mesh-A components: 11 raw, 2 strong candidates.
  - Regularizer yields **one** canonical intersection curve on mesh A:
    - ~138 vertices, ~137 edges, closed,
    - length ≈ 1136, which is ~1% off the analytic circle length for this sphere configuration.
    - `hasSyntheticClosure = true`.

This curve is exactly what higher layers should use as the 1D intersection between the two surfaces on mesh A.

---

## 2. Current guarantees and limitations

### 2.1 Guarantees

For any `IntersectionCurve` produced by `IntersectionCurveRegularizer`:

- It is a **closed cycle** of global intersection vertices:
  - `Vertices[0] == Vertices[^1]`.
- Internally 2-regular:
  - Each vertex has degree 2 within the curve.
- All (non-synthetic) edges correspond to **actual intersection edges** in `IntersectionGraph`.
- Synthetic closure edges (if any) are:
  - limited to at most one per component,
  - clearly marked (negative ids),
  - purely between existing intersection vertices,
  - geometrically short relative to the component length.
- Each underlying edge in the (augmented) component is used **exactly once** in the cycle.

The raw pipeline `IntersectionSet → PairFeatures → IntersectionGraph → TriangleIntersectionIndex → MeshATopology` is exercised on real, non-toy geometry (two subdivided spheres) and passes strong consistency checks (barycentric reconstructions, triangle participation, etc.).

### 2.2 Limitations

- The regularizer is currently implemented and tested for **mesh A only**. Mesh B is still pending symmetric treatment.
- No actual **triangle cutting** is implemented yet:
  - Triangles are not yet subdivided into patches by these curves.
- No **region classification**:
  - You don’t yet decide which patches to keep for union / intersection / difference.
- No **output mesh construction**:
  - There is no watertight boolean result mesh being emitted.
- The **Z³ / integer-grid snapping layer** is not integrated with this intersection pipeline yet.
- There is no high-level public API like `Shape.BooleanUnion(otherShape)` built on top of this.

---

## 3. Roadmap: from here to `Union(ShapeA, ShapeB)`

Below is a realistic sequence of steps to get from the current stack to full boolean operations. Each “step” can still be broken into micro-commits like you’ve been doing.

### 3.1 Step 1: Symmetric curves on mesh B

Right now, you have canonical curves on mesh A. For boolean meshing, you want the same on both surfaces.

Concrete tasks:

- Implement `MeshBTopology` (or generalize `MeshATopology` to “per mesh”).
- Implement `IntersectionCurveRegularizer.RegularizeMeshB` using the same logic as for A.
- Add diagnostics/tests that:
  - The set of **global vertices** and **real edges** used by A-curves and B-curves match (modulo synthetic closures).
  - The approximate length of the canonical A-curve and B-curve is the same (within tolerance).
  - For the sphere/sphere case, both sides produce at least one high-quality intersection curve.

This ensures that the 1D intersection geometry is consistent from both mesh perspectives.

### 3.2 Step 2: Per-triangle cutting primitives

Goal: Given a regularized curve and per-triangle barycentrics, split a single triangle into polygonal pieces along intersection segments.

Concrete tasks for mesh A (then B):

- For a triangle `T` in A:
  - Get its `TriangleIntersectionVertex[]` from `TriangleIntersectionIndex.TrianglesA`.
  - From the curve, determine which segments pass over `T`:
    - Each pair `(vi, v{i+1})` in a curve:
      - If both vertices lie on triangle `T` (via vertex id lookup), then `T` has a segment between those barycentric points.
- Implement a helper that:
  - Takes:
    - the original triangle (3 vertices in barycentric or world coordinates),
    - N intersection points on its surface,
    - M segments between them,
  - Returns:
    - A set of polygonal sub-faces (triangulated or left as general polygons).
- Ensure:
  - The union of sub-faces reconstructs the original triangle area.
  - Sub-faces touching the intersection curves have explicit border edges aligned with the curves.

At this stage, pick a simple, robust cutting scheme (e.g. triangulate everything locally) rather than something too clever; you can always optimize later.

### 3.3 Step 3: Patch connectivity and region classification

Now each original triangle in A and B can be split into patches (sub-faces). Next step: determine which patches belong to:

- A\B,
- B\A,
- A∩B,
- A∪B.

Standard strategy:

- For each patch, pick an interior sample point in world space.
- Use a solid membership test (e.g. winding number / ray casting against the *other* mesh) to decide whether that point is “inside B” or “outside B” (for A’s patches), and vice versa.
- Classification:
  - Patch from A is:
    - part of `A\B` if it is outside B and not on the intersection;  
    - part of `A∩B` (for intersection operation) if it is inside B;  
    - part of `A∪B` if it is outside B (A side), etc.
- Patches exactly on the intersection loops (if any such concept appears) will be handled specially or simply triangulated as border faces.

This requires a robust “point in closed mesh” test. You already have a BVH / bounding structure; reuse that for fast ray tests.

For now, you can start with:

- Only support **closed, watertight meshes** as input.
- Only support generic, well-behaved intersection (no disjoint shapes, no tangencies).

### 3.4 Step 4: Output mesh assembly

Once patches are labeled, you must glue them back together into a result mesh.

Concrete tasks:

- Decide on an output mesh data structure:
  - Likely a `Mesh` type with:
    - vertex list (world positions),
    - triangle list,
    - adjacency optional.
- For each included patch:
  - Emit its vertices:
    - Either reuse original mesh vertices and intersection vertices as shared indices.
    - Or build fresh vertices but deduplicate (similar quantization as `IntersectionGraph`).
  - Emit triangulated faces.
- Ensure the result is **watertight**:
  - Every interior edge is shared by exactly two triangles.
  - No cracks along intersection curves; all curve edges are shared by exactly two output triangles where appropriate.
- Add tests:
  - Sphere–sphere intersection / union / difference should:
    - produce a mesh with no open edges,
    - approximate the expected volume and surface area.

At this stage you have a continuous-space boolean engine (still using doubles).

### 3.5 Step 5: Integrate Z³ / integer grid

Your long-term goal is integer-grid exactness. Once continuous booleans work:

- Introduce a **snapping step** for intersection vertices:
  - Map world coordinates to Z³ (e.g. `Int128` grid).
  - Ensure that:
    - Nearby vertices snap consistently,
    - Edges do not invert or cross incorrectly when snapped.
- Rebuild:
  - Intersection curves in Z³,
  - Cut patches, but now with integer coordinates.
- Revise:
  - Tolerances to be consistent with the snapping strategy.
- Add tests:
  - Show that snapping preserves topology:
    - No self intersections introduced,
    - No stray gaps opened.

This is its own big topic, but the key point is: do it **after** continuous boolean logic is verified.

### 3.6 Step 6: High-level API surface

Only once the above is solid should you wrap everything in high-level APIs like:

```csharp
Mesh BooleanUnion(Mesh a, Mesh b);
Mesh BooleanIntersection(Mesh a, Mesh b);
Mesh BooleanDifference(Mesh a, Mesh b);
or object-oriented shapes:

csharp
Copy code
Shape A = new Sphere(...);
Shape B = new Box(...);
Shape union = Shape.BooleanUnion(A, B);
Internally:

Construct meshes for shapes (if not already meshes).

Run the intersection + regularization + cutting + region classification pipeline.

Return a new Mesh or Shape that carries the result.

The high-level API is, in many ways, the least interesting piece as long as it correctly orchestrates the robust lower layers.

3.7 Step 7: Diagnostics, regression tests, and performance
Throughout:

Keep the Diagnostics app you already started, but extend it:

Per shape pair:

counts of intersections, components,

regularized curves and their lengths,

degree histograms.

Add a regression test suite of “canonical pairs”:

sphere–sphere,

box–box aligned,

box–box skew,

sphere–box,

some nasty non-convex shapes.

Address performance:

IntersectionSet BVH performance and scaling,

IntersectionGraph memory and dedup efficiency,

parallelization opportunities (per triangle / per component).

Eventually: integrate logging / debug output toggles at each layer.

4. Summary
From the perspective of today:

Your intersection and curve regularization pipeline is in good shape:

It is layered,

It has clean, testable invariants,

It is already exercised on realistic geometries (two subdivided spheres),

It produces canonical, regularized 1D intersection curves on mesh A.

To reach “union(A,B)”:

Mirror regularization on mesh B,

Implement per-triangle cutting on both meshes,

Classify patches by inside/outside relationships,

Assemble an output mesh,

Then layer Z³ snapping and a high-level API on top.

The hard part—getting from chaotic triangle intersections to clean, canonical intersection curves—is already largely done. The remaining work is substantial but conceptually straightforward: it’s surface cutting, region labeling, and mesh assembly, all of which can be built on the regularized curves you have now.