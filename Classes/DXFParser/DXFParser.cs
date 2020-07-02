using MSolvLib.MarkGeometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MarkGeometriesLib.Classes.DXFParser
{
    public class DXFParser
    {
        private int _count;
        private string _filePath;
        private IMarkGeometry _previousGeometry;

        private static readonly Regex SupportedEntities = new Regex(@"^\s*(ARC|LINE|CIRCLE|LWPOLYLINE|POINT|SPLINE)$", RegexOptions.Compiled);//POLYLINE
        private static readonly Regex AcDbEntity = new Regex(@"^\s*(AcDbEntity)$", RegexOptions.Compiled);
        private static readonly Regex LineEntity = new Regex(@"^\s*(AcDbLine)$", RegexOptions.Compiled);
        private static readonly Regex MatchLine = new Regex(@"^\s*(LINE)$", RegexOptions.Compiled);
        private static readonly Regex MatchDoubleParams = new Regex(@"^\s*(10|20|30|11|21|31)$", RegexOptions.Compiled);
        private static readonly Regex MatchDoubleLastParams = new Regex(@"^\s*(30|31)$", RegexOptions.Compiled);
        private static readonly Regex MatchDouble = new Regex(@"(-)\d+(\.\d+)*", RegexOptions.Compiled);

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public DXFParser(string filePathIn)
        {
            if (!File.Exists(filePathIn))
                throw new FileNotFoundException($"Could not find `{filePathIn}`");

            _filePath = filePathIn;
        }

        public string ReadVersion()
        {
            using (var reader = new AdvancedLineStreamReader(_filePath))
            {
                var result = reader.FindConsecutiveLines(
                    new Regex(@"^\s*9", RegexOptions.Compiled),
                    new Regex(@"^\s*\$ACADVER$", RegexOptions.Compiled)
                );

                if (result.Success)
                {
                    // skip group code
                    reader.ReadLine();

                    return reader.ReadLine().Trim();
                }

                return null;
            }
        }

        public HashSet<string> ReadLayers()
        {
            var buffer = new HashSet<string>();

            using (var reader = new AdvancedLineStreamReader(_filePath))
            {
                while (!reader.EndOfStream)
                {
                    var result = reader.FindConsecutiveLines(
                        new Regex(@"^\s*100", RegexOptions.Compiled),
                        new Regex(@"^\s*AcDbEntity$", RegexOptions.Compiled)
                    );

                    if (result.Success)
                    {
                        reader.FindLine(new Regex(@"^\s*8", RegexOptions.Compiled));
                        buffer.Add(reader.ReadLine().Trim());
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return buffer;
        }

        public Dictionary<string, List<IMarkGeometry>> ReadGeometries(int howmany=-1)
        {
            _count = 0;
            var buffer = new Dictionary<string, List<IMarkGeometry>>();

            using (var reader = new AdvancedLineStreamReader(_filePath))
            {
                string layerName = null;
                IMarkGeometry geometry = null;
                var entityGroupCode = new Regex(@"^\s*0", RegexOptions.Compiled);

                while (!reader.EndOfStream)
                {
                    var result = reader.FindConsecutiveLines(
                        entityGroupCode,
                        SupportedEntities
                    );

                    if (!result.Success)
                        break;

                    switch(result.LineB.Trim())
                    {
                        case "POINT":
                            (layerName, geometry) = ParsePoint(reader);
                            break;
                        case "LWPOLYLINE":
                            (layerName, geometry) = ParseLWPolyline(reader);
                            break;
                        case "SPLINE":
                            (layerName, geometry) = ParseSpline(reader);
                            break;
                        //case "POLYLINE":
                        //    (layerName, geometry) = ParsePolyline(reader);
                        //    break;
                        case "LINE":
                            (layerName, geometry) = ParseLine(reader);
                            break;
                        case "CIRCLE":
                            (layerName, geometry) = ParseCircle(reader);
                            break;
                        //case "ARC":
                        //    (layerName, geometry) = ParseArc(reader);
                        //    break;
                    }

                    if (geometry != null)
                    {
                        _count += 1;
                        layerName = (layerName == null) ? "0" : layerName;

                        if (!buffer.ContainsKey(layerName))
                        {
                            buffer[layerName] = new List<IMarkGeometry>(100000);
                        }

                        buffer[layerName].Add(geometry);
                        _previousGeometry = geometry;
                    }

                    if (howmany > 0 && _count > howmany)
                        break;
                }
            }

            return buffer;
        }

        private (string LayerName, MarkGeometryPoint Point) ParsePoint(AdvancedLineStreamReader readerIn)
        {
            var (success, layerName) = ReadLayerName(readerIn, "AcDbPoint");

            if (success)
            {
                var result1 = readerIn.FindConsecutiveLines(
                    "100",
                    "AcDbPoint"
                );

                if (!result1.Success)
                    return (null, null);
            }

            return (layerName, ReadPointFast(readerIn));
        }

        private (string LayerName, MarkGeometrySpline Spline) ParseSpline(AdvancedLineStreamReader readerIn)
        {
            var (success, layerName) = ReadLayerName(readerIn, "AcDbSpline");

            if (success)
            {
                var result1 = readerIn.FindConsecutiveLines(
                    "100",
                    "AcDbSpline"
                );

                if (!result1.Success)
                    return (null, null);
            }

            // read number of knots
            int numberOfKnots = ReadInteger(readerIn, "72");

            // read number of control points
            int numberOfControlPoints = ReadInteger(readerIn, "73");

            // read number of fit points
            int numberOfFitPoints = ReadInteger(readerIn, "74");

            var knots = new List<double>(numberOfKnots);

            for(int i=0; i<numberOfKnots; i++)
            {
                knots.Add(ReadDouble(readerIn, "40"));
            }

            var controlPoints = new List<MarkGeometryPoint>(numberOfControlPoints);

            for (int i = 0; i < numberOfControlPoints; i++)
            {
                controlPoints.Add(ReadPointFast(readerIn));
            }

            var fitPoints = new List<MarkGeometryPoint>(numberOfFitPoints);

            for (int i = 0; i < numberOfFitPoints; i++)
            {
                fitPoints.Add(ReadPointFast(readerIn));
            }

            return (layerName, new MarkGeometrySpline(knots, controlPoints, fitPoints));
        }

        private (string LayerName, MarkGeometryPath Path) ParseLWPolyline(AdvancedLineStreamReader readerIn)
        {
            var (success, layerName) = ReadLayerName(readerIn, "AcDbPolyline");

            if (success)
            {
                var result1 = readerIn.FindConsecutiveLines(
                    "100",
                    "AcDbPolyline"
                );

                if (!result1.Success)
                    return (null, null);
            }

            // read number of vertices 90
            var line = readerIn.ReadLine();
            int numberOfVertices = int.Parse(readerIn.ReadLine());

            var points = new List<MarkGeometryPoint>(numberOfVertices);

            for(int i=0; i<numberOfVertices; i++)
            {
                points.Add(ReadPointFast2D(readerIn));
            }

            return (layerName, new MarkGeometryPath(points.ToArray()));
        }

        private (string LayerName, MarkGeometryPath Path) ParsePolyline(AdvancedLineStreamReader readerIn)
        {
            var regex = new Regex(@"^\s*(AcDb2dPolyline|AcDb3dPolyline)$");
            var (success, layerName) = ReadLayerName(readerIn, regex);

            if (success)
            {
                var result1 = readerIn.FindConsecutiveLines(
                    new Regex(@"^\s*100$"),
                    regex
                );

                if (!result1.Success)
                    return (null, null);
            }

            // read number of vertices 90
            var line = readerIn.ReadLine();
            int numberOfVertices = int.Parse(readerIn.ReadLine());

            var points = new List<MarkGeometryPoint>(numberOfVertices);

            for (int i = 0; i < numberOfVertices; i++)
            {
                points.Add(ReadPointFast2D(readerIn));
            }

            return (layerName, new MarkGeometryPath(points.ToArray()));
        }

        private (string LayerName, MarkGeometryCircle Circle) ParseCircle(AdvancedLineStreamReader readerIn)
        {
            var (success, layerName) = ReadLayerName(readerIn, "AcDbCircle");

            if (success)
            {
                var result1 = readerIn.FindConsecutiveLines(
                    "100",
                    "AcDbCircle"
                );

                if (!result1.Success)
                    return (null, null);
            }

            MarkGeometryPoint centrePoint = ReadPointFast(readerIn);

            // read radius 40
            readerIn.ReadLine();
            double radius = double.Parse(readerIn.ReadLine());

            var circle = new MarkGeometryCircle(centrePoint, radius);
            return (layerName, circle);
        }

        private (string LayerName, MarkGeometryArc Arc) ParseArc(AdvancedLineStreamReader readerIn)
        {
            var (success, layerName) = ReadLayerName(readerIn, "AcDbCircle");

            if (success)
            {
                var result1 = readerIn.FindConsecutiveLines(
                    "100",
                    "AcDbCircle"
                );

                if (!result1.Success)
                    return (null, null);
            }

            MarkGeometryPoint centrePoint = ReadPointFast(readerIn);

            // read radius 40
            readerIn.ReadLine();
            double radius = double.Parse(readerIn.ReadLine());

            var result2 = readerIn.FindConsecutiveLines(
                "100",
                "AcDbArc"
            );

            if (!result2.Success)
                return (null, null);

            // read angle 50
            readerIn.ReadLine();
            double startAngle = Math.Abs(double.Parse(readerIn.ReadLine()) % 360);

            // read angle 60
            readerIn.ReadLine();
            double endAngle = Math.Abs(double.Parse(readerIn.ReadLine()) % 360);

            if (startAngle > endAngle)
            {
                startAngle = -(360 - startAngle);
            }

            var arc = new MarkGeometryArc(
                centrePoint,
                radius, // convert angle to radians
                startAngle / 180.0 * Math.PI,
                endAngle / 180.0 * Math.PI
            );

            return (layerName, arc);
        }

        private (string LayerName, MarkGeometryLine Line) ParseLine(AdvancedLineStreamReader readerIn)
        {

            var (success, layerName) = ReadLayerName(readerIn, "AcDbLine");

            if (success)
            {
                var result = readerIn.FindConsecutiveLines(
                    "100",
                    "AcDbLine"
                );

                if (!result.Success)
                    return (null, null);
            }

            var line = new MarkGeometryLine(
                ReadPointFast(readerIn),
                ReadPointFast(readerIn)
            );

            return (layerName, line);
        }

        private (bool Success, string) ReadLayerName(AdvancedLineStreamReader readerIn, string terminationIn)
        {
            var result = readerIn.FindConsecutiveLines(
                "100",
                "AcDbEntity"
            );

            if (!result.Success)
                return (false, "0");

            var result2 = readerIn.FindLineUntil(
                "8",
                terminationIn
            );

            return (result2.Success, result2.Success ? readerIn.ReadLine().Trim() : "0");
        }

        private (bool Success, string) ReadLayerName(AdvancedLineStreamReader readerIn, Regex terminationRegexIn)
        {
            var result = readerIn.FindConsecutiveLines(
                "100",
                "AcDbEntity"
            );

            if (!result.Success)
                return (false, "0");

            var result2 = readerIn.FindLineUntil(
                new Regex(@"^\s*8$"),
                terminationRegexIn
            );

            return (result2.Success, result2.Success ? readerIn.ReadLine().Trim() : "0");
        }

        private MarkGeometryPoint ReadPointFast(AdvancedLineStreamReader readerIn)
        {
            // read doble params 10, 11, etc
            readerIn.FindLine(MatchDoubleParams);
            double X = double.Parse(readerIn.ReadLine());

            // read doble params 20, 21, etc
            readerIn.ReadLine();
            double Y = double.Parse(readerIn.ReadLine());

            double Z = 0;

            if (MatchDoubleLastParams.IsMatch(readerIn.PeekLine()))
            {
                // read doble params 30, 31, etc
                readerIn.ReadLine();
                Z = double.Parse(readerIn.ReadLine());
            }

            return new MarkGeometryPoint(X, Y, Z);
        }

        private double ReadDouble(AdvancedLineStreamReader readerIn, string paramCode)
        {
            readerIn.FindLine(paramCode);
            return double.Parse(readerIn.ReadLine());
        }

        private int ReadInteger(AdvancedLineStreamReader readerIn, string paramCode)
        {
            readerIn.FindLine(paramCode);
            return int.Parse(readerIn.ReadLine());
        }

        private MarkGeometryPoint ReadPointFast2D(AdvancedLineStreamReader readerIn)
        {
            // read doble params 10, 11, etc
            readerIn.FindLine(MatchDoubleParams);
            double X = double.Parse(readerIn.ReadLine());

            // read doble params 20, 21, etc
            readerIn.ReadLine();
            double Y = double.Parse(readerIn.ReadLine());

            return new MarkGeometryPoint(X, Y);
        }

        private MarkGeometryPoint ReadPointAdvance(AdvancedLineStreamReader readerIn)
        {
            var result = readerIn.FindConsecutiveLines(
                MatchDoubleParams,
                MatchDouble
            );

            if (result.Success)
            {
                double X = double.Parse(MatchDouble.Match(result.LineB).Value);

                result = readerIn.FindConsecutiveLines(
                    MatchDoubleParams,
                    MatchDouble
                );

                double Y = double.Parse(MatchDouble.Match(result.LineB).Value);
                double Z = 0;

                if (MatchDoubleParams.IsMatch(readerIn.PeekLine()))
                {
                    result = readerIn.FindConsecutiveLines(
                        MatchDoubleParams,
                        MatchDouble
                    );

                    Z = double.Parse(MatchDouble.Match(result.LineB).Value);
                }

                return new MarkGeometryPoint(X, Y, Z);
            }

            return null;
        }

        private void Log(Exception exp)
        {
            Console.WriteLine(exp.ToString());
            if (exp.InnerException != null)
            {
                Log(exp.InnerException);
            }
        }
    }
}
