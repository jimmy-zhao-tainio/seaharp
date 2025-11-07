using System;
using Seaharp.World;
using Seaharp.Geometry;

internal class Program
{
    private static void Main()
    {
        var world = new World();

        // Gallery layout parameters
        const int spacing = 500; // center-to-center spacing
        const int zBase = 0;

        // Sizes
        const long solidRadius = 120; // for tetra/octa/icosa/dodeca/sphere
        const long cubeEdge = 240;
        const long cylRadius = 140;
        const long ringThickness = 8;
        const long ringHeight = 8;

        int col = 0;
        void AddAtCol(Shape s)
        {
            // Position by translating so its center lands at (col*spacing, 0, zBase)
            // Shapes that are center-aware use their own centers; Box needs centering.
            world.Add(s);
            col++;
        }

        // Row 1: Platonic solids + cube
        AddAtCol(new RegularTetra(solidRadius, new Point(-2 * spacing, 0, zBase)));
        {
            var b = new Box(cubeEdge, cubeEdge, cubeEdge);
            b.Position(-cubeEdge / 2, -cubeEdge / 2, -cubeEdge / 2);
            b.Position(-1 * spacing, 0, zBase);
            AddAtCol(b);
        }
        AddAtCol(new Octahedron(solidRadius, new Point(0 * spacing, 0, zBase)));
        AddAtCol(new Dodecahedron(solidRadius, new Point(1 * spacing, 0, zBase)));
        AddAtCol(new Icosahedron(solidRadius, new Point(2 * spacing, 0, zBase)));

        // Row 2: Common primitives
        AddAtCol(new Sphere(solidRadius, new Point(3 * spacing, 0, zBase)));
        AddAtCol(new SolidCylinder(cylRadius, height: 280, center: new Point(4 * spacing, 0, zBase), segments: 24));
        AddAtCol(new Cylinder(cylRadius, ringThickness, ringHeight, new Point(5 * spacing, 0, zBase), segments: 24));
        AddAtCol(new SquarePyramid(baseEdge: 260, height: 260, center: new Point(6 * spacing, 0, zBase)));
        AddAtCol(new TriangularPrism(edge: 240, height: 260, center: new Point(7 * spacing, 0, zBase)));
        AddAtCol(new Cone(radius: 140, height: 280, center: new Point(8 * spacing, 0, zBase), segments: 32));

        world.Save("shapes_gallery.stl");
    }
}
