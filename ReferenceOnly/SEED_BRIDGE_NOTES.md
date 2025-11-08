# Seed Bridge Fill — Implementation Notes

This note specifies a concrete plan to construct a bridge solid between two solids using only new tetrahedra. No CSG, no mutation of inputs, integer grid only (Z^3), exact integer predicates.

## Scope and Assumptions

- Inputs A and B are closed, two‑manifold solids with outward‑oriented boundary triangles.
- Vertices lie on the integer grid (Z^3) and satisfy the existing Tetrahedron range constraints.
- Inputs are immutable; the bridge is a separate solid composed only of new tetrahedra.
- Predicates use exact integer math (e.g., SignedTetrahedronVolume6) for orientation/intersection.

## Definitions

- TriangleKey: canonical, order‑independent key of 3 integer vertices for deduplication.
- FaceSet: hash set of TriangleKey representing current boundary faces.
- Bridge: set of tetrahedra with a FaceSet boundary maintained by symmetric difference.

## Core Predicates (integer only)

- Orientation / plane‑side: IntegerMath.SignedTetrahedronVolume6(A,B,C,P).
- Colinearity / coplanarity: volume == 0 and Int128 cross products.
- Triangle‑triangle / line‑triangle intersection: integer GeometryChecks.
- Plane‑cuts‑triangle‑interior: the plane of triangle T1 cuts the interior of triangle T2 iff the signed volumes at T2’s vertices w.r.t. T1’s plane have mixed signs (min < 0 and max > 0).
- Visibility: triangles are mutually visible iff (a) all 3 vertices of each lie strictly on the positive side of the other’s plane and (b) plane‑cuts‑triangle‑interior is false in both directions; triangles are not coplanar; allowed contacts handled separately.

## Admissible Contacts

- Shared vertex: allowed only for the one shared vertex of a classified pair.
- Shared edge: allowed only for the shared edge of a classified pair.
- Triangle interiors: no new vertex may lie strictly in the interior of any boundary triangle (A, B, or Bridge).
- Triangle‑triangle: no new bridge face may intersect any boundary face except along the explicitly permitted shared edge/vertex of the pair.

## Classification and Tet Packs

- SharedEdge: triangles share exactly two vertices. Pack: one tet spanning the opposite vertices, if positive volume.
- VertexOnEdge: a vertex of one lies on an edge of the other. Pack: up to 3 tets, include equator only if volume > 0.
- SharedVertex: triangles share exactly one vertex. Pack: 2–3 tets; choose the variant maximizing the minimum absolute volume.
- Prism3Tets: general facing case (strict plane‑side in both directions). Pack: 3 tets forming a triangular prism. Evaluate all vertex permutations of the far triangle; choose pack maximizing min‑volume; tie‑break on minimal total squared vertex distance.
- (Optional later) Octa4Tets: can be added if needed; not required for box‑to‑box.

## Seed Selection

1) Extract A_boundary and B_boundary (outward triangles).
2) Enumerate candidate pairs (tA, tB) that pass Visibility and are non‑intersecting.
3) For each candidate, build a pack via classification and selection; run Clearance Validation.
4) Commit the first valid seed (or prefer largest min‑volume), creating Bridge.

## Frontier Growth

- Frontier is a queue of Bridge boundary triangles.
- While Frontier not empty:
  - Pop frontier triangle F.
  - Opponents set Opp = A_boundary ∪ B_boundary ∪ current Bridge boundary.
  - For T in Opp that is mutually visible with F and passes quick rejections:
    - Classify(F, T); build candidate pack(s); run Clearance Validation.
    - If valid, commit pack (tetrahedra + boundary update) and enqueue newly exposed Bridge faces; continue.
  - If none valid for F, discard F and continue.
- Maintain visited (F,T) to avoid loops.
- Multi‑seed: if Frontier empties and visible A↔B pairs remain, select another seed and continue.

## Clearance Validation

For a candidate pack:
- Positive volume: all tets have Int128 volume6 > 0.
- Bridge vs A/B: no new bridge face triangle‑intersects any A/B boundary face, except along the permitted shared edge/vertex with the paired triangles.
- Vertex‑in‑face: reject if any new vertex lies strictly in a boundary triangle interior (A/B/Bridge).
- Self‑consistency: face/edge contacts inside the pack are allowed; boundary symmetric difference removes internal faces.

## Boundary Maintenance (atomic)

- For each tet, enumerate faces (ABC, ABD, ACD, BCD) and compute TriangleKey.
- For each face, if key exists in Bridge.FaceSet remove it; otherwise add it.
- After processing the pack, append its tets to Bridge. Newly added keys are the faces to enqueue to Frontier.

## Search Policy

- Pop policy: FIFO is acceptable; priority by larger centroid span is optional.
- Candidate ordering per F: sort T by increasing centroid distance, then by decreasing min‑volume of best pack.
- Backtracking: not required initially; multi‑seed suffices. Add limited backtracking if dead‑ends are common.

## Termination and Guarantees

- Terminates when Frontier is empty and no further seed validates.
- Guarantees: committed tets have positive volume and no invalid intersections under admissible‑contact rules.
- Not guaranteed: single‑seed completeness. Multiple seeds may be required depending on triangulation and corridor geometry.

## Performance Notes

- Visible pairs are O(|A|·|B|) without indexing; add spatial bins or a BVH for scale.
- Clearance uses exact integer tests; short‑circuit with plane‑side, coplanarity, and bounding boxes.

## Determinism

- Fix iteration orders, tie‑breakers, and queue policy to ensure reproducible results.

## Minimal Integration

- Module: World.Bridging (or ReferenceOnly). API: BuildBridge(Shape a, Shape b) => Shape bridge.
- Reuse ReferenceOnly integer helpers: IntegerMath, GeometryChecks, TriangleOperations, TriangleBridgeBuilder.
- Do not mutate inputs; return a Shape with only the bridge tetrahedra.

## Pseudocode (concise)

```
Bridge = empty
A = boundary(A)
B = boundary(B)

def try_seed():
  for (tA, tB) in visible_pairs(A, B):
    pack = best_pack(tA, tB)
    if valid(pack, A, B, Bridge):
      commit(pack, Bridge)
      return True
  return False

if not try_seed():
  return empty

Frontier = queue(Bridge.boundary)
Visited  = set()

while Frontier:
  F = Frontier.pop()
  for T in opponents(F, A, B, Bridge.boundary):
    if (F,T) in Visited: continue
    Visited.add((F,T))
    if not visible(F,T): continue
    pack = best_pack(F, T)
    if valid(pack, A, B, Bridge):
      commit(pack, Bridge)
      Frontier.push(new_exposed_faces(pack, Bridge))
      break

if not Frontier and more_visible_pairs_exist(A,B,Bridge):
  if try_seed(): Frontier.push_all(Bridge.boundary)

return Bridge
```

