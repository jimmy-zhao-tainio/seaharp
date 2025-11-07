using System;
using Seaharp.World;
using Seaharp.Geometry;

internal class Program
{
    private static void Main()
    {
        var world = new World();

        // Clean solar system: sun + 8 planets, each at a different orbital phase,
        // with moons in distinct orbits (no asteroids, no extra geometry).

        var sunCenter = new Point(0, 0, 0);
        world.Add(new Sphere(radius: 180, center: sunCenter));

        // Approximate orbital inclinations (deg) and ascending node (deg) per orbit radius
        static (double incDeg, double ascNodeDeg) OrbitOrientation(long orbit)
            => orbit switch
            {
                350  => (7.0, 48.3),   // Mercury
                520  => (3.4, 76.7),   // Venus
                720  => (0.0, 0.0),    // Earth
                920  => (1.85, 49.6),  // Mars
                1250 => (1.3, 100.6),  // Jupiter
                1600 => (2.49, 113.7), // Saturn
                1950 => (0.77, 74.0),  // Uranus
                2280 => (1.77, 131.8), // Neptune
                _    => (0.0, 0.0),
            };

        static (double x, double y, double z) RotateXZ(double x, double y, double z, double xDeg, double zDeg)
        {
            double rx = xDeg * Math.PI / 180.0;
            double rz = zDeg * Math.PI / 180.0;
            double cx = Math.Cos(rx), sx = Math.Sin(rx);
            double cz = Math.Cos(rz), sz = Math.Sin(rz);
            // First rotate around X by xDeg
            double y1 = y * cx - z * sx;
            double z1 = y * sx + z * cx;
            double x1 = x;
            // Then rotate around Z by zDeg
            double x2 = x1 * cz - y1 * sz;
            double y2 = x1 * sz + y1 * cz;
            double z2 = z1;
            return (x2, y2, z2);
        }

        void AddPlanet(long orbit, int planetRadius, double phaseDeg,
                       long[]? moonOrbits = null, int moonRadius = 10,
                       double moonIncline = 8.0)
        {
            // Determine realistic orbit tilt and node
            var (incDeg, ascDeg) = OrbitOrientation(orbit);

            // Trace the planet's orbit with a very thin tilted ecliptic cylinder
            var orbitRing = new EclipticCylinder(radius: orbit, thickness: 2, height: 2,
                                                center: sunCenter,
                                                segments: null,
                                                xTiltDeg: incDeg, yTiltDeg: 0, zSpinDeg: ascDeg);
            world.Add(orbitRing);

            double ph = phaseDeg * Math.PI / 180.0;
            // Position planet on the tilted orbit plane
            var pv = RotateXZ(Math.Cos(ph) * orbit, Math.Sin(ph) * orbit, 0.0, incDeg, ascDeg);
            long px = sunCenter.X + (long)Math.Round(pv.x);
            long py = sunCenter.Y + (long)Math.Round(pv.y);
            long pz = sunCenter.Z + (long)Math.Round(pv.z);
            var pc = new Point(px, py, pz);
            world.Add(new Sphere(planetRadius, pc));

            if (moonOrbits is null || moonOrbits.Length == 0) return;
            for (int i = 0; i < moonOrbits.Length; i++)
            {
                // Moon orbit ring around the planet with an additional inclination (degrees)
                // Use same node as planet, vary slightly by moon index to avoid perfect alignment
                double moonAsc = ascDeg + i * (360.0 / (moonOrbits.Length * 3.0));
                double moonInc = incDeg + moonIncline;
                var moonRing = new EclipticCylinder(radius: moonOrbits[i], thickness: 2, height: 2,
                                                    center: pc, segments: null,
                                                    xTiltDeg: moonInc, yTiltDeg: 0, zSpinDeg: moonAsc);
                world.Add(moonRing);

                // Place moon on its tilted ring
                double ma = (phaseDeg * 2.0 + i * (360.0 / moonOrbits.Length)) * Math.PI / 180.0;
                var mv = RotateXZ(Math.Cos(ma) * moonOrbits[i], Math.Sin(ma) * moonOrbits[i], 0.0, moonInc, moonAsc);
                long mx = pc.X + (long)Math.Round(mv.x);
                long my = pc.Y + (long)Math.Round(mv.y);
                long mz = pc.Z + (long)Math.Round(mv.z);
                world.Add(new Sphere(moonRadius, new Point(mx, my, mz)));
            }
        }

        // Mercury, Venus, Earth(+moon), Mars(+2), Jupiter(+4), Saturn(+3), Uranus(+2), Neptune(+1)
        AddPlanet(orbit: 350,  planetRadius: 22, phaseDeg: 10);
        AddPlanet(orbit: 520,  planetRadius: 32, phaseDeg: 60);
        AddPlanet(orbit: 720,  planetRadius: 34, phaseDeg: 130,
                  moonOrbits: new long[] { 70 }, moonRadius: 12);
        AddPlanet(orbit: 920,  planetRadius: 26, phaseDeg: 210,
                  moonOrbits: new long[] { 55, 80 }, moonRadius: 9);
        AddPlanet(orbit: 1250, planetRadius: 78, phaseDeg: 280,
                  moonOrbits: new long[] { 140, 190, 260, 340 }, moonRadius: 15, moonIncline: 12);
        AddPlanet(orbit: 1600, planetRadius: 66, phaseDeg: 330,
                  moonOrbits: new long[] { 160, 240, 320 }, moonRadius: 13, moonIncline: 10);
        AddPlanet(orbit: 1950, planetRadius: 54, phaseDeg: 45,
                  moonOrbits: new long[] { 120, 180 }, moonRadius: 11);
        AddPlanet(orbit: 2280, planetRadius: 52, phaseDeg: 95,
                  moonOrbits: new long[] { 140 }, moonRadius: 11);

        world.Save("clean_system.stl");
    }
}




