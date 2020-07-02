using ContourHatcher.Utils;
using GenericTestProject;
using MarkGeometriesLib.Classes;
using MarkGeometriesLib.Classes.DXFParser;
using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using MSolvLib.MarkGeometry;
using SharpGLShader.Utils;
using STLSlicer;
using STLSlicer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MarkGeometriesLib
{
    class Program
    {
        static void Main(string[] args)
        {
            var result1 = GeometryToImageConverter.To1BppImageComplexRetainBounds(
                @"C:\Users\Chibuike.Okpaluba\Downloads\Test.dxf",
                @"C:\Users\Chibuike.Okpaluba\Downloads\Test.bmp",
                null,
                320, 320
            );

            var result2 = GeometryToImageConverter.To1BppImageComplexRetainBounds(
                @"C:\Users\Chibuike.Okpaluba\Downloads\Test.dxf",
                @"C:\Users\Chibuike.Okpaluba\Downloads\Test30.bmp",
                null,
                320, 320,
                angle: 0
            );

            var result3 = GeometryToImageConverter.To1BppImageComplexRetainBounds(
                @"C:\Users\Chibuike.Okpaluba\Downloads\Test.dxf",
                @"C:\Users\Chibuike.Okpaluba\Downloads\Test30b.bmp",
                null,
                320, 160,
                angle: 0
            );

            //Console.WriteLine("Done.");
            //Console.ReadLine();
        }

        private static void PrintSlicesCount(string tag, List<ContourStructure>[] slices)
        {
            Console.WriteLine($"{tag} has {slices.Length} slices");
            for (int i=0; i< slices.Length; i++)
            {
                Console.WriteLine($"\tSlice {i} has {slices[i].Count} contours");
                for(int j=0; j<slices[i].Count; j++)
                    Console.WriteLine($"\t\tContour {j} has {slices[i][j].IntersectionList.Count} joints");
            }
        }

        private static void PrintSlicesCount(string tag, List<MLinkedList<IntersectionStructure>>[] slices)
        {
            Console.WriteLine($"{tag} has {slices.Length} slices");
            for (int i = 0; i < slices.Length; i++)
            {
                Console.WriteLine($"\tSlice {i} has {slices[i].Count} contours");
                for (int j = 0; j < slices[i].Count; j++)
                    Console.WriteLine($"\t\tContour {j} has {slices[i][j].Count} joints");
            }
        }

        private static MarkGeometryPath ToPath(IEnumerable<MVertex> vertices)
        {
            return new MarkGeometryPath(vertices.Select(v => new MarkGeometryPoint(v.X, v.Y)));
        }

        private static List<MarkGeometryLine> ToLines(IEnumerable<MVertex> vertices)
        {
            return GeometricArithmeticModule.ToLines(vertices.Select(v => new MarkGeometryPoint(v.X, v.Y)).ToArray());
        }

        private static void Println(string tag, LineEquation eqn)
        {
            Console.WriteLine($"{tag}: {{Gradient: {Math.Round(eqn.Gradient, 4)}, Intercept: {Math.Round(eqn.YIntercept, 4)}}}");
        }

        private static void Println<T>(string tag, IEnumerable<T> values)
        {
            Console.WriteLine($"{tag}, Count: {values.Count()}");
            foreach(var value in values)
                Console.WriteLine($"\t{value?.ToString()}");
        }

        private static void Println(string tag, double val, int round = 3)
        {
            Console.WriteLine($"{tag}: {Math.Round(val, round)}");
        }

        private static void Println(string tag, object value)
        {
            Console.WriteLine($"{tag}: {value?.ToString()}");
        }

        private static void ApplyTransform(IEnumerable<IMarkGeometry> geometries, Matrix4x4 transformationMatrix)
        {
            foreach (var geometry in geometries)
                geometry.Transform(transformationMatrix);
        }

        private static void PrintExtents(string tag, IMarkGeometry geometry)
        {
            PrintExtents(tag, geometry.Extents);
            Console.WriteLine($"Area: {Math.Round(geometry.Area, 4)}");
            Console.WriteLine($"Perimeter: {Math.Round(geometry.Perimeter, 4)}");
        }

        private static void PrintExtents(string tag, GeometryExtents<double> extents)
        {
            Console.WriteLine($"\n{tag}");
            Console.WriteLine($"Centre: {Math.Round(extents.Centre.X, 4)}, {Math.Round(extents.Centre.Y, 4)}");
            Console.WriteLine($"Size: {Math.Round(extents.Width, 4)}, {Math.Round(extents.Height, 4)}");
            Console.WriteLine($"Extents: {extents}");
        }

        private static void Timeit(string tag, Action action, int howmany=5)
        {
            var st = DateTime.Now;
            for (int i = 0; i < howmany; i++)
                action();
            var et = DateTime.Now;

            Console.WriteLine($"{tag} time : {Math.Round((et - st).TotalMilliseconds) / howmany, 5}ms");
        }

        private static void TestSlicing()
        {
            var _slicer = new MSTLSlicer();
            _slicer.Load(@"C:\MSOLV\STLs\Ice Cream Type 2.stl");
            //_slicer.Load(@"C:\MSOLV\STLs\tray.stl");
            //_slicer.Load(@"C:\MSOLV\STLs\flange.stl");
            //_slicer.Load(@"C:\MSOLV\STLs\TestBinaryKIT.stl");

            List<ContourStructure>[] control = new List<ContourStructure>[] { };
            List<MContourStructure>[] slices = new List<MContourStructure>[] { };

            Console.WriteLine(
                PerformanceHelper.Compare(
                    () =>
                    {
                        control = _slicer.Slice();
                    },
                    () =>
                    {
                        slices = _slicer.SliceParallel();
                    },
                    compareRepeat: 5,
                    timingRepeat: 5,
                    tagA: "Control",
                    tagB: "Slices"
                )
            );

            //PrintSlicesCount("Control", control);
            //PrintSlicesCount("Slices", slices);
        }

        private static void TestQuadTree()
        {
            var _slicer = new MSTLSlicer();
            _slicer.Load(@"C:\MSOLV\STLs\Ice Cream Type 2.stl");
            //_slicer.Load(@"C:\MSOLV\STLs\tray.stl");
            var slices = _slicer.Slice();

            IEnumerable<MVertex> contour = slices[7][0].ToVertices().Concat(slices[5][0].ToVertices());
            //IEnumerable<MVertex> contour = slices[5][1].ToVertices();

            var reference = ToPath(contour);
            var intersectingLine = new MarkGeometryLine(
                reference.Extents.MinimumPoint,
                reference.Extents.MaximumPoint
            );

            List<MarkGeometryPoint> controlResults = new List<MarkGeometryPoint>();
            List<MarkGeometryPoint> quadTreeResults = new List<MarkGeometryPoint>();

            var lines = ToLines(contour);
            var contourQuadTree = new ContourQuadTree(contour);

            Console.WriteLine(
                PerformanceHelper.Compare(
                    () =>
                    {
                        // setup up
                        controlResults.Clear();

                        MarkGeometryPoint intersection;
                        for (int i = 0; i < lines.Count; i++)
                            if ((
                                intersection = GeometricArithmeticModule.CalculateIntersection2D(
                                    intersectingLine,
                                    lines[i]
                                )) != null
                            )
                                controlResults.Add(intersection);
                    },
                    () =>
                    {
                        // setup up
                        quadTreeResults.Clear();

                        quadTreeResults = contourQuadTree.Intersect(intersectingLine).ToList();
                    },
                    tagA: "Lines (Control)",
                    tagB: "Quad Tree"
                )
            );

            Println("Control", controlResults);
            Println("Quad Tree Intersections", quadTreeResults);

            contourQuadTree.SaveImage(@"C:\Users\Chibuike.Okpaluba\Downloads\quad_tree_v2.png");
        }

        private static void Obsolete2()
        {
            #region Section: Not Used
            //List<MarkGeometryPath> openGeometries = new List<MarkGeometryPath>();
            //List<IMarkGeometry> closedGeometries = new List<IMarkGeometry>();

            //foreach (var kv in GeometricArithmeticModule.ExtractLabelledGeometriesFromDXF(@"C:\Users\Chibuike.Okpaluba\Downloads\#PLR printer data input assesment2_LibrecadPOints.ipt.dxf"))
            //{
            //    for (int i = 0; i < kv.Value.Count; i++)
            //    {
            //        if (kv.Value[i] is MarkGeometryLine line)
            //        {
            //            openGeometries.Add(new MarkGeometryPath(line));
            //        }
            //        else if (kv.Value[i] is MarkGeometryArc arc)
            //        {
            //            if (Math.Abs(arc.Sweep % (2 * Math.PI)) <= 0.0001)
            //                closedGeometries.Add(new MarkGeometryPath(arc));
            //            else
            //                openGeometries.Add(new MarkGeometryPath(arc));
            //        }
            //        else if (kv.Value[i] is MarkGeometryPath path)
            //        {
            //            if (path.IsClosed)
            //                closedGeometries.Add(path);
            //            else
            //                openGeometries.Add(path);
            //        }
            //        else
            //        {
            //            closedGeometries.Add(kv.Value[i]);
            //        }
            //    }
            //}

            //while(openGeometries.Count > 0)
            //{
            //    MarkGeometryPath reference = openGeometries[0];
            //    openGeometries.RemoveAt(0);

            //    var (trace, ___) = Trace(reference, openGeometries, 0.001);

            //    closedGeometries.Add(trace);
            //} 
            #endregion

            var slicer = new MSTLSlicer();

            //Timeit(
            //    "Load",
            //    () => {
            //        //slicer.Load(@"C:\MSOLV\STLs\TestASCII.stl");
            //        slicer.Load(@"C:\MSOLV\STLs\tray.stl");
            //    }
            //);

            //Console.WriteLine($"{slicer.Facets?.Count}");

            //Timeit(
            //    "Slice",
            //    () => {
            //        slicer.Slice(slicer.MinZ + 15, 0.1, 0.001);
            //    }
            //);
        }

        private static void Obsolete()
        {
            //var parser = new DXFParser(@"C:\MSOLV\DXF\Flongle v6 shallow trench Mask04 (202004013) ONT V4_ForGerber.dxf");
            //var parser = new DXFParser(@"C:\MSOLV\DXF\Thermal Shock Test Panel - LaserAblation.dxf");
            //var parser = new DXFParser(@"C:\MSOLV\DXF\arc-city.dxf");
            var parser = new DXFParser(@"C:\MSOLV\DXF\473-00001-DXF-1- Shield with fiducials.dxf");

            var layerNames = parser.ReadLayers();
            foreach (var name in layerNames)
                Console.WriteLine(name);

            //Console.WriteLine(parser.ReadVersion());

            //var startTime = DateTime.Now;

            //try
            //{
            //    Dictionary<string, List<IMarkGeometry>> geometries = parser.ReadGeometries(-1);

            //    //Dictionary<string, List<IMarkGeometry>> geometries = new Dictionary<string, List<IMarkGeometry>>()
            //    //{
            //    //    {"test", new List<IMarkGeometry>(){ new MarkGeometryArc(new MarkGeometryPoint(), 50, -180 * Math.PI / 180, -270 * Math.PI / 180) } }
            //    //};

            //    Console.WriteLine($"Count: {parser.Count}");
            //    Console.WriteLine($"Time Taken: {Math.Round((DateTime.Now - startTime).TotalSeconds, 4)} seconds");
            //    startTime = DateTime.Now;

            //    SaveImage(geometries, parser.Count);
            //}
            //catch(Exception exp)
            //{
            //    Log(exp);
            //}
            //finally
            //{
            //    Console.WriteLine($"Count: {parser.Count}");
            //    Console.WriteLine($"Time Taken: {Math.Round((DateTime.Now - startTime).TotalSeconds, 4)} seconds");
            //}
        }

        private static void SaveImage(Dictionary<string, List<IMarkGeometry>> geometriesIn, int capacity)
        {
            var _geometries = new List<IMarkGeometry>(capacity);
            
            foreach(var items in geometriesIn.Values)
            {
                _geometries.AddRange(items);
            }

            var extents = GeometricArithmeticModule.CalculateExtents(_geometries);
            Console.WriteLine($"Extents: {extents}");

            GeometryToImageConverter.To1BppImage(
                _geometries,
                @"D:\Downloads\demo.tiff",
                pixelSize: 25.4,
                dpiX: 720,
                dpiY: 720,
                lineWidth: -1 // smallest possible
            );
        }

        private static void Log(Exception exp)
        {
            Console.WriteLine(exp.ToString());
            if (exp.InnerException != null)
            {
                Log(exp.InnerException);
            }
        }
    }
}
