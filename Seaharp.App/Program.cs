using System;
using Seaharp.Geometry;

const UnitScale unit = UnitScale.Millimeter;
const int boxSize = 50;
const int padding = 50;

var lowerBox = new Box(unit, boxSize, boxSize, boxSize)
    .Position(-boxSize / 2, -boxSize / 2, 0);

var upperBox = new Box(unit, boxSize, boxSize, boxSize)
    .Position(-boxSize / 2, -boxSize / 2, 0)
    .Rotate(zDegrees: 30)
    .Position(0, 0, boxSize + padding);

var assembly = Shape.Combine(lowerBox, upperBox);

Console.WriteLine("=== Box Stack Demo ===");
Console.WriteLine($"Unit: {unit.Abbreviation()}");
Console.WriteLine($"Lower box top Z: {lowerBox.Bounds.Max.Z}");
Console.WriteLine($"Upper box bottom Z: {upperBox.Bounds.Min.Z}");
Console.WriteLine($"Total tetrahedra: {assembly.Solid.Tetrahedra.Count}");

var outputPath = Path.Combine(AppContext.BaseDirectory, "box-stack.obj");
SolidExporter.WriteObj(assembly.Solid, outputPath);
Console.WriteLine($"OBJ saved to: {outputPath}");
