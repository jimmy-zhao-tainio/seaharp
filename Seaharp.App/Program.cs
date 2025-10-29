using System;
using System.Collections.Generic;
using Seaharp.Geometry;

const UnitScale unit = UnitScale.Millimeter;
const int boxSize = 50;
const int padding = 50;

RunStackDemo();

static void RunStackDemo()
{
    var lowerBox = new Box(unit, boxSize, boxSize, boxSize)
        .Position(-boxSize / 2, -boxSize / 2, 0);

    var upperBox = new Box(unit, boxSize, boxSize, boxSize)
        .Position(-boxSize / 2, -boxSize / 2, 0)
        .Rotate(zDegrees: 30)
        .Position(0, 0, boxSize + padding);

    Console.WriteLine("=== Box Stack Demo ===");
    Console.WriteLine($"Unit: {unit.Abbreviation()}");
    Console.WriteLine($"Lower box top Z: {lowerBox.Bounds.Max.Z}");
    Console.WriteLine($"Upper box bottom Z: {upperBox.Bounds.Min.Z}");

    var parts = new List<Shape> { lowerBox, upperBox };
    var outputFile = "box-stack.obj";

    if (BridgeExtensions.TryBridge(lowerBox, upperBox, out var bridge, out var lowerFace, out var upperFace))
    {
        Console.WriteLine("Bridge found between boxes:");
        PrintTriangle("  Lower face", lowerFace);
        PrintTriangle("  Upper face", upperFace);
        Console.WriteLine($"  Loft tetrahedra: {bridge.Solid.Tetrahedra.Count}");

        parts.Add(bridge);
        outputFile = "box-bridge.obj";
    }
    else
    {
        Console.WriteLine("No mutually visible triangle pair found; skipping bridge.");
    }

    var assembly = Shape.Combine(parts.ToArray());
    Console.WriteLine($"Total tetrahedra: {assembly.Solid.Tetrahedra.Count}");

    var outputPath = Path.Combine(AppContext.BaseDirectory, outputFile);
    SolidExporter.WriteObj(assembly.Solid, outputPath);
    Console.WriteLine($"OBJ saved to: {outputPath}");
}

static void PrintTriangle(string label, TriangleFace face)
{
    Console.WriteLine($"{label}:");
    foreach (var vertex in face.Vertices)
    {
        Console.WriteLine($"  ({vertex.X}, {vertex.Y}, {vertex.Z})");
    }
}



