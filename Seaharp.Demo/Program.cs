using System;
using System.Globalization;
using System.IO;
using Seaharp.World;

internal class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var outPath = args.Length > 0 ? args[0] : "demo.obj";
        var outDir = Path.GetDirectoryName(outPath);
        if (!string.IsNullOrWhiteSpace(outDir)) Directory.CreateDirectory(outDir!);

        var world = new Seaharp.World.World();

        // Box standing roughly on a corner (rotate around X then Y)
        var cornerBox = new Box(width: 20, depth: 20, height: 20);
        cornerBox.Rotate(35.26438968, 45.0, 0.0); // body diagonal near vertical
        cornerBox.Position(0, 0, 0);
        world.Add(cornerBox);

        // A second rotated box to the side for visual variety
        var sideBox = new Box(width: 12, depth: 8, height: 18);
        sideBox.Rotate(10.0, 0.0, 30.0);
        sideBox.Position(35, 0, 0);
        world.Add(sideBox);

        // Save OBJ
        world.Save(outPath);
        Console.WriteLine($"Wrote OBJ: {outPath}");
    }
}

