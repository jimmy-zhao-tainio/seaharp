using Geometry;
using WorldModel = global::World.World;
using Sphere = global::World.Sphere;

internal static class Program
{
    private static void Main(string[] args)
    {
        long r = 200;
        var aCenter = new Point(0, 0, 0);
        var bCenter = new Point(150, 50, -30);

        var a = new Sphere(r, subdivisions: 3, center: aCenter);
        var b = new Sphere(r, subdivisions: 3, center: bCenter);

        var world = new WorldModel();
        world.Add(a);
        world.Add(b);

        // Intersection demo temporarily disabled; writing both meshes as-is.
        var triangleCount = 0;
        foreach (var shape in world.Shapes)
        {
            triangleCount += shape.Mesh.Triangles.Count;
        }

        var outPath = "spheres_with_disc.stl";
        world.Save(outPath);
        Console.WriteLine($"Wrote placeholder (no intersection): {System.IO.Path.GetFullPath(outPath)} with {triangleCount} triangles");
    }
}
