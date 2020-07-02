using MarkGeometriesLib.Classes;
using MarkGeometriesLib.Classes.DXFParser;
using MSolvLib.MarkGeometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkGeometriesLib
{
    class Program
    {
        static void Main(string[] args)
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

            Console.WriteLine("Done.");
            Console.ReadLine();
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
