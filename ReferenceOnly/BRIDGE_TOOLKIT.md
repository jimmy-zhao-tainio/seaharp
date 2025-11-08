# Bridge Builder Toolkit — Outside‑In Checklist

Use this as a playbook to drive implementation and avoid thrash.

## Technique Stack
- Invariants-first: enforce on every commit — positive tet volume6 > 0, no external face intersections, admissible contacts only.
- Contact-aware visibility: non-contact vertices strictly positive-side both ways; no plane-cuts‑other‑interior; reject coplanar.
- CEGAR loop: Counterexample → tighten predicate/clearance → rerun. No heuristic tweaks without a demonstrated gap.
- Property-based generation: synthesize triangle pairs and box pairs (axis-aligned/rotated); shrink failures to a 2‑triangle repro.
- Metamorphic tests: invariance under translate/axis‑permute/vertex order/mirroring; A↔B swap yields equivalent result.
- Symmetry oracles: classification and packs independent of vertex labeling.
- Reason codes: one crisp failure tag per rejection (see below).
- Minimal repro bundles: two triangles + chosen pack + boolean checks + reason code.

## Search Strategy
- Multi-seed default: maintain/compute visible A↔B pairs; start new seeds when a frontier stalls.
- Deterministic tie-breaks: min‑volume → total squared distance; fixed iteration orders.
- Limits: cap attempts per frontier face (top‑N nearest opponents), cap total packs/seeds; abort gracefully.
- Component seeding: cluster visible pairs by face adjacency; seed at least one per component to jump behind warps.

## Boundary + Clearance
- Boundary engine: atomic symmetric‑difference of TriangleKeys; assert no duplicate external faces remain.
- Clearance: early rejects (plane‑side, AABB), then exact triangle‑triangle; allow only intended shared vertex/edge contacts.

## Failure Reason Codes
- VIS_POS_SIDE_FAIL — non-contact vertex not strictly positive-side.
- VIS_PLANE_CUT — plane cuts the other triangle interior (mixed signs).
- VIS_COPLANAR — triangles coplanar.
- CLR_FACE_INTERSECT — bridge face would intersect an A/B/Bridge face.
- CLR_VERTEX_IN_FACE — a new vertex lies strictly in a boundary triangle interior.
- PACK_ZERO_OR_NEG_VOL — candidate pack has non‑positive tet volume.
- PACK_CLEARANCE_FAIL — aggregate clearance failure.
- DUP_OR_INTERNAL_FACE — boundary toggle inconsistency/internalization.
- FRONTIER_STALL — no valid opponents for this frontier within attempt limit.
- SEED_NONE_AVAILABLE — no seed validates under rules.

## Test Matrix (minimal but surgical)
- Prism3Tets only: box↔box (axis‑aligned, rotated); seeds near/away from corners.
- SharedEdge: identical edge, non‑coplanar; single‑tet bridge.
- VertexOnEdge / SharedVertex: include/omit equator; verify clearance/admissible contacts.
- Occluded corridor: warped faces behind seed; multi‑seed fills and merges cleanly.

## Outside‑In Cadence
- Phase 1: predicates + boundary engine + Prism3Tets/SharedEdge; prove the matrix.
- Phase 2: add VertexOnEdge/SharedVertex with contact‑aware visibility.
- Phase 3: indexing (bins/BVH) for scale; optional Octa4Tets.

