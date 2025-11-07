# Seaharp

Seaharp is a small geometry project that builds 3D shapes from integer-grid tetrahedra and exports a binary STL. The demo renders a simple solar system with a sun, eight planets, their moons, and thin tilted orbit rings.

Note: This repository is a fully GPT-5 Codex experiment. All code and documentation were produced end-to-end by the agent in Codex CLI (no hands-on coding).

## Pure ℤ³, Only Tetrahedra

- All vertices live on the integer lattice ℤ³ (no persistent floats).
- Every shape is a union of tetrahedra only; surfaces are derived from boundary triangles.
- Construction uses double math only transiently (e.g., rotations), then rounds back to the grid.
- Tetrahedra are immutable and orientation-correct; degenerate tets are rejected.
- Higher-level shapes (sphere, cylinder shell, etc.) are produced by decomposing into tetrahedra (e.g., prism → 3 tets).

![Clean Solar System](clean_system.png)

## Demo Code

The demo uses a tiny data helper (`Seaharp.Demo/Planets.cs`) so `Program.cs` reads like a short scene description. Here is the essence of the program:

```csharp
using Seaharp.World;
using Seaharp.Geometry;
using Seaharp.Demo; // Planets helper (data only)

var world = new World();
var sunCenter = new Point(0, 0, 0);
world.Add(new Sphere(radius: 180, center: sunCenter));

void AddPlanet(in Planets.Planet p, double phaseDeg)
{
    // Thin tilted orbit ring
    world.Add(new Cylinder(radius: p.OrbitRadius, thickness: 2, height: 2,
                                   center: sunCenter, segments: null,
                                   xTiltDeg: p.InclinationDeg, yTiltDeg: 0, zSpinDeg: p.AscendingNodeDeg));

    // Place the planet on its tilted plane (details in Program.cs)
    // ... compute position and add spheres for planet and moons ...
}

// Mercury, Venus, Earth(+moon), Mars(+2), Jupiter(+4), Saturn(+3), Uranus(+2), Neptune(+1)
AddPlanet(Planets.Mercury, phaseDeg: 10);
AddPlanet(Planets.Venus, phaseDeg: 60);
AddPlanet(Planets.Earth, phaseDeg: 130);
AddPlanet(Planets.Mars, phaseDeg: 210);
AddPlanet(Planets.Jupiter, phaseDeg: 280);
AddPlanet(Planets.Saturn, phaseDeg: 330);
AddPlanet(Planets.Uranus, phaseDeg: 45);
AddPlanet(Planets.Neptune, phaseDeg: 95);

world.Save("clean_system.stl");
```

See the full, runnable code in:
- `Seaharp.Demo/Program.cs`
- `Seaharp.Demo/Planets.cs`

## Building and Running

- Build: `dotnet build Seaharp.sln -c Release`
- Run demo: `dotnet run --project Seaharp.Demo -c Release`
- Output: `Seaharp.Demo/bin/Release/net9.0/clean_system.stl`

The screenshot above is `clean_system.png` generated from that STL.
