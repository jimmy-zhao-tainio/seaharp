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

        void AddPlanet(long orbit, int planetRadius, double phaseDeg,
                       long[]? moonOrbits = null, int moonRadius = 10,
                       double moonIncline = 8.0)
        {
            double ph = phaseDeg * Math.PI / 180.0;
            long px = sunCenter.X + (long)Math.Round(Math.Cos(ph) * orbit);
            long py = sunCenter.Y + (long)Math.Round(Math.Sin(ph) * orbit);
            long pz = (long)Math.Round(15 * Math.Sin(ph * 0.7));
            var pc = new Point(px, py, pz);
            world.Add(new Sphere(planetRadius, pc));

            if (moonOrbits is null || moonOrbits.Length == 0) return;
            for (int i = 0; i < moonOrbits.Length; i++)
            {
                double ma = (phaseDeg * 2.0 + i * (360.0 / moonOrbits.Length)) * Math.PI / 180.0;
                long mx = pc.X + (long)Math.Round(Math.Cos(ma) * moonOrbits[i]);
                long my = pc.Y + (long)Math.Round(Math.Sin(ma) * moonOrbits[i]);
                long mz = pc.Z + (long)Math.Round(moonIncline * Math.Sin((i + 1) * 0.9));
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




