using System;
using Seaharp.World;
using Seaharp.Geometry;
using Seaharp.Demo;

internal class Program
{
    private static void Main()
    {
        var world = new World();

        // Clean solar system: sun + 8 planets, each at a different orbital phase,
        // with moons in distinct orbits (no asteroids, no extra geometry).

        var sunCenter = new Point(0, 0, 0);
        world.Add(new Sphere(radius: 180, center: sunCenter));

        // Planets and moons by name (helper in Planets.cs)

        // Simple helper to add a planet by name using Planets data
        static (double x, double y, double z) RotateXZ(double x, double y, double z, double xDeg, double zDeg)
        {
            double rx = xDeg * Math.PI / 180.0;
            double rz = zDeg * Math.PI / 180.0;
            double cx = Math.Cos(rx), sx = Math.Sin(rx);
            double cz = Math.Cos(rz), sz = Math.Sin(rz);
            // X
            double y1 = y * cx - z * sx;
            double z1 = y * sx + z * cx;
            double x1 = x;
            // Z
            double x2 = x1 * cz - y1 * sz;
            double y2 = x1 * sz + y1 * cz;
            double z2 = z1;
            return (x2, y2, z2);
        }

        void AddPlanet(in Planets.Planet p, double phaseDeg)
        {
            // Orbit ring
            world.Add(new EclipticCylinder(radius: p.OrbitRadius, thickness: 2, height: 2,
                                           center: sunCenter, segments: null,
                                           xTiltDeg: p.InclinationDeg, yTiltDeg: 0, zSpinDeg: p.AscendingNodeDeg));

            // Body position on tilted orbit
            double ph = phaseDeg * Math.PI / 180.0;
            var pv = RotateXZ(Math.Cos(ph) * p.OrbitRadius, Math.Sin(ph) * p.OrbitRadius, 0.0,
                               p.InclinationDeg, p.AscendingNodeDeg);
            var pc = new Point(
                sunCenter.X + (long)Math.Round(pv.x),
                sunCenter.Y + (long)Math.Round(pv.y),
                sunCenter.Z + (long)Math.Round(pv.z));
            world.Add(new Sphere(p.Radius, pc));

            // Moons
            if (p.MoonOrbits.Length == 0) return;
            for (int i = 0; i < p.MoonOrbits.Length; i++)
            {
                double moonAsc = p.AscendingNodeDeg + i * (360.0 / (p.MoonOrbits.Length * 3.0));
                double moonInc = p.InclinationDeg + p.MoonExtraInclineDeg;
                world.Add(new EclipticCylinder(radius: p.MoonOrbits[i], thickness: 2, height: 2,
                                               center: pc, segments: null,
                                               xTiltDeg: moonInc, yTiltDeg: 0, zSpinDeg: moonAsc));

                double ma = (phaseDeg * 2.0 + i * (360.0 / p.MoonOrbits.Length)) * Math.PI / 180.0;
                var mv = RotateXZ(Math.Cos(ma) * p.MoonOrbits[i], Math.Sin(ma) * p.MoonOrbits[i], 0.0,
                                   moonInc, moonAsc);
                long mx = pc.X + (long)Math.Round(mv.x);
                long my = pc.Y + (long)Math.Round(mv.y);
                long mz = pc.Z + (long)Math.Round(mv.z);
                world.Add(new Sphere(p.MoonRadius, new Point(mx, my, mz)));
            }
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
    }
}




