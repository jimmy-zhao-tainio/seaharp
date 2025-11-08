# Bridge Builder Design (Seed-and-Grow, Multi-Seed)

This document captures a practical design to construct a bridge solid between two solids using only new tetrahedra. No CSG, no mutation of inputs. Integer grid only (Z^3), integer predicates only.

## Goals
- Build a watertight bridge solid using new tetrahedra only.
- Keep A and B immutable; never modify or re-mesh inputs.
- Use exact integer predicates for all decisions.

## Core Rules
- Inputs: closed, two-manifold solids with outward-oriented boundary triangles; vertices in Z^3 and within Tetrahedron range constraints.
- Visibility (classification-aware): triangles t1 and t2 are mutually visible when, respecting the intended contact for the classification,
  - Non-contact vertices lie strictly on the positive side of the other’s plane (strict integer test).
  - The plane of each triangle does not cut the interior of the other triangle (mixed signed volumes test is false both ways).
  - Coplanar pairs are rejected.
- Admissible contacts: allow only the shared vertex/edge dictated by the classification case; no other face or edge intersections.
- Positive volume: every tetrahedron added must have strictly positive Int128 volume6.

## Classification and Packs
Re-use the existing TriangleBridgeBuilder cases and pack selection:
- SharedEdge: one tet spanning opposite vertices.
- VertexOnEdge: up to 3 tets; include equator only if volume > 0.
- SharedVertex: 2–3 tets; choose pack maximizing minimum absolute volume.
- Prism3Tets: general facing case; build 3-tet prism; evaluate far-triangle cyclic permutations; choose pack maximizing minimum absolute volume; tie-break by minimal total squared vertex distance.
- Optional later: Octa4Tets if needed.

### Contact-Aware Visibility Rules
- SharedEdge: two vertices coincide (identical edge) across non-coplanar triangles.
  - Allowed contacts: both shared edge vertices may be exactly on both planes (zero signed volume).
  - Requirement: each triangle’s opposite vertex must lie strictly on the positive side of the other’s plane; no plane-cuts-other-interior in either direction.
  - Pack: single tetrahedron spanning the two opposite vertices across the shared edge (if positive volume and clearance holds).
- VertexOnEdge: one vertex of one triangle lies on an edge of the other.
  - Allowed contact: the specific vertex-on-edge may be on plane; the two non-contact vertices must be strictly positive-side relative to the other plane.
  - Pack: up to 3 tets (include equator only if volume > 0) subject to clearance.
- SharedVertex: one vertex shared.
  - Allowed contact: the shared vertex may be on plane; the other two vertices must be strictly positive-side relative to the other plane.
  - Pack: 2–3 tets chosen by max-min-volume, subject to clearance.
- Prism3Tets: no shared elements.
  - All three vertices of each triangle must be strictly positive-side relative to the other’s plane; and no plane-cuts-other-interior.

## Clearance Validation
For each candidate pack:
- Face intersection: no new bridge face may intersect any boundary face in A/B/Bridge except at the intended shared vertex/edge for the paired triangles.
- Vertex-in-face: reject if any new vertex lies strictly inside any boundary triangle (A/B/Bridge).
- Self-consistency: contacts inside the same pack are allowed; boundary symmetric difference will remove internal faces.

## Boundary Maintenance (Atomic)
- Maintain Bridge.FaceSet (hash set of TriangleKey) for the current bridge boundary.
- Commit pack atomically by symmetric difference: for each face, toggle its TriangleKey in the set; then append the pack tets.
- Newly added keys after the toggle are the new outward faces to enqueue in the frontier.

## Frontier Growth (Single Wavefront)
- Frontier is a queue of Bridge boundary triangles.
- Loop: pop a frontier triangle F, search opponents in A_boundary, B_boundary, and current Bridge boundary. For each opponent T that passes Visibility:
  - Classify(F, T), build candidate pack(s), run Clearance Validation.
  - If valid, commit, enqueue new faces, continue with next frontier face.
- Maintain a visited set of (F_key, T_key) to avoid repeated attempts.

## Occlusion and Horizon Handling (Multi-Seed)
When the surface behind an initial seed warps and hides other triangles, do not force a single wavefront.
- Visible pair pool: maintain a pool of all A↔B pairs that pass Visibility and are non-intersecting (precomputed or lazily updated).
- Multi-seed: when the current frontier stalls (no valid opponents), start another seed from the unused visible pair pool deeper along the corridor. Keep separate wavefronts growing in parallel (conceptually).
- Wavefront merge: when two wavefronts meet, the boundary symmetric-difference removes internal faces, leaving a unified Bridge.
- Component awareness (optional): build a graph over A↔B visible pairs using face adjacency on A and B, and seed at least one pair per connected component to jump behind warps.

## Halt Criteria
- Stop when all frontiers are exhausted AND there are no unused visible A↔B pairs that validate as seeds.
- If a region has no valid seed under admissible-contact rules, it is not bridgeable at the current triangulation and grid scale.

## Data Structures
- TriangleKey: canonical, order-independent key of 3 integer vertices.
- FaceSet: boundary triangle key set for Bridge; similar sets for A and B (static).
- Frontier: queue of Bridge boundary triangle keys.
- VisiblePairs: set or index of A↔B visible pairs. Optionally indexed by plane/centroid bins for faster search.
- Visited: set of attempted (frontier_key, opponent_key) pairs.

## Determinism
- Fix iteration orders, tie-breakers (min-volume then total squared distance), and queue policy to make runs reproducible.

## Performance Notes
- Without indexing, visible pair search is O(|A|*|B|). For larger inputs, add simple spatial bins or a BVH over triangle centroids and plane hashes.
- Keep early rejections cheap: plane-side, coplanarity, bounding boxes, then triangle-triangle tests.

## MVP Implementation Steps
- Boundary extraction: surface triangles for A and B (already available per-shape).
- Integer predicates: SignedTetrahedronVolume6, triangle-triangle intersection, vertex-in-face, no-plane-cut check.
- Classification and pack selection: reuse existing ReferenceOnly logic; ensure integer-only checks around it.
- Bridge state: FaceSet + tetra list; atomic symmetric-difference commit.
- Frontier loop: single wavefront growth with visited set and opponents search.
- Multi-seed: visible pair pool; when stalled, seed a new wavefront.
- Tests: axis-aligned box↔box and rotated box↔box; verify watertightness and invariants after each commit.

## Short Pseudocode
```
Bridge = empty
A = boundary(A)
B = boundary(B)
Pairs = visible_pairs(A,B)

seed = pick_seed(Pairs)
if not seed: return empty
commit(seed)
Frontier = queue(Bridge.boundary)
Visited  = set()

while Frontier:
  F = Frontier.pop()
  for T in opponents(F, A, B, Bridge.boundary):
    if (F,T) in Visited: continue
    Visited.add((F,T))
    if not visible(F,T): continue
    pack = best_pack(F,T)
    if valid(pack, A, B, Bridge):
      commit(pack)
      Frontier.push(new_faces(pack))
      break

if not Frontier:
  while Pairs.has_unused_seed():
    seed = pick_seed(Pairs)
    if not seed: break
    if commit_if_valid(seed):
      Frontier.push_all(Bridge.boundary)
      # jump back to the while Frontier loop

return Bridge
```

## Red‑Team Traps (Deliberately False — Do Not Implement)

These lines exist to keep us sharp. They are intentionally wrong and must be rejected during review. If any of these ever ships, we file a bug immediately.

- Accept coplanar triangles if their centroids differ. (False: coplanar must be rejected.)
- Allow “epsilon volume” tets with volume6 == 0 or near 0. (False: all tets must have strictly positive integer volume6.)
- If visibility fails, flip triangle winding and retry. (False: visibility rules are invariant to winding.)
- Permit face–face overlap when overlap area < threshold. (False: only exact shared edge/vertex contacts are allowed.)
- Use float normals for speed; downgrade integer predicates. (False: integer predicates are required.)
- Prefer greedy pack with maximal total volume, ignore min‑volume. (False: use max‑min‑volume; greed can box us in.)
- Treat plane‑cuts‑triangle‑interior as a warning only. (False: it is a hard failure.)
- Single‑seed only; disable multi‑seed. (False: multi‑seed is required for occluded corridors.)

Reviewer checklist: prove none of the above appears in code, tests, or config. If it does, remove and add a failing test.


