using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using MSolvLib.MarkGeometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MarkGeometriesLib.Classes.DXFParser
{
    public class DXFParser
    {
        #region Section: Private Static Properties

        private static readonly Regex SupportedEntities = new Regex(@"^\s*(ARC|LINE|CIRCLE|LWPOLYLINE|POINT|SPLINE)$", RegexOptions.Compiled);//POLYLINE
        private static readonly Regex AcadVersionMarker = new Regex(@"^\s*(\$ACADVER)$", RegexOptions.Compiled);
        private static readonly Regex AcDbEntity = new Regex(@"^\s*(AcDbEntity)$", RegexOptions.Compiled);
        private static readonly Regex LineEntity = new Regex(@"^\s*(AcDbLine)$", RegexOptions.Compiled);
        private static readonly Regex MatchLine = new Regex(@"^\s*(LINE)$", RegexOptions.Compiled);
        private static readonly Regex MatchDoubleParams = new Regex(@"^\s*(10|20|30|11|21|31)$", RegexOptions.Compiled);
        private static readonly Regex MatchDoubleLastParams = new Regex(@"^\s*(30|31)$", RegexOptions.Compiled);
        private static readonly Regex MatchDouble = new Regex(@"(-)\d+(\.\d+)*", RegexOptions.Compiled);
        private static readonly Regex MatchEntityGroupCode = new Regex(@"^\s*0", RegexOptions.Compiled);
        private static readonly Regex MatchLayerGroupCode = new Regex(@"^\s*8", RegexOptions.Compiled);
        private static readonly Regex MatchVersionGroupCode = new Regex(@"^\s*9", RegexOptions.Compiled);
        private static readonly Regex MatchSubClassMarker = new Regex(@"^\s*100", RegexOptions.Compiled);
        private static readonly Regex MatchPolylineEntity = new Regex(@"^\s*(AcDb2dPolyline|AcDb3dPolyline)$", RegexOptions.Compiled);
        private static readonly Regex MatchLWPolylineEntity = new Regex(@"^\s*(AcDbPolyline)$", RegexOptions.Compiled);
        private static readonly Regex MatchSplineEntity = new Regex(@"^\s*(AcDbSpline)$", RegexOptions.Compiled);

        #endregion

        #region Section: Public Static Properties

        public static readonly string[] SupportedVersions = { "AC1015", "AC1018", "AC1021", "AC1024" };

        #endregion

        #region Section: Private Properties

        private int _count;
        private string _filePath;

        #endregion

        #region Section: Public Properties
        
        public int Count
        {
            get
            {
                return _count;
            }
        }

        #endregion

        #region Section: Constructor

        public DXFParser(string filePathIn)
        {
            if (!File.Exists(filePathIn))
                throw new FileNotFoundException($"Could not find `{filePathIn}`");

            _filePath = filePathIn;
        }

        #endregion

        #region Section: Public Methods

        public bool IsVersionSupported()
        {
            return SupportedVersions.Contains(ReadVersion());
        }

        public string ReadVersion()
        {
            using (var reader = new AdvancedLineStreamReader(_filePath))
            {
                var result = reader.FindConsecutiveLines(
                    MatchVersionGroupCode,
                    AcadVersionMarker
                );

                if (result.Success)
                {
                    // skip sub group code
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
                        MatchSubClassMarker,
                        AcDbEntity
                    );

                    if (result.Success)
                    {
                        reader.FindLine(MatchLayerGroupCode);
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

        public Dictionary<string, List<IMarkGeometry>> ReadGeometries(int howmany = int.MaxValue)
        {
            _count = 0;
            var buffer = new Dictionary<string, List<IMarkGeometry>>();

            BeginGetGeometries((layerName, geometry) =>
                {
                    if (!buffer.ContainsKey(layerName))
                    {
                        buffer[layerName] = new List<IMarkGeometry>();
                    }

                    buffer[layerName].Add(geometry);
                },
                howmany
            );

            return buffer;
        }

        /// <summary>
        ///     Get geometries in DXF file.
        /// </summary>
        /// <param name="callback">An action to consume geometries as they are read from the file.</param>
        /// <param name="howmany">Howmany geometries to read</param>
        /// <returns>Returns a count and Extents of the geometries consumed.</returns>
        public (int Count, GeometryExtents<double> Extents) BeginGetGeometries(Action<string, IMarkGeometry> callback, int howmany = int.MaxValue)
        {
            _count = 0;
            howmany = howmany < 0 ? int.MaxValue : howmany;
            var extents = GeometryExtents<double>.CreateDefaultDouble();

            using (var reader = new AdvancedLineStreamReader(_filePath))
            {

                string layerName;
                List<IMarkGeometry> geometries = null;

                while (
                    !reader.EndOfStream &&
                    _count < howmany
                )
                {
                    (layerName, geometries) = ParseEntity(reader);

                    if (geometries != null)
                    {
                        foreach (var geometry in geometries)
                        {
                            _count += 1;
                            extents = GeometryExtents<double>.Combine(extents, geometry.Extents);
                            callback((layerName == null) ? "0" : layerName, geometry);
                        }
                    }
                }
            }

            return (_count, extents);
        }

        public (string LayerName, List<IMarkGeometry> Entity) ParseEntity(AdvancedLineStreamReader readerIn)
        {
            var result = readerIn.FindConsecutiveLines(
                MatchEntityGroupCode,
                SupportedEntities
            );

            if (!result.Success)
                return (null, null);

            switch (result.LineB.Trim())
            {
                case "POINT":
                    var pData = ParsePoint(readerIn);
                    return (pData.LayerName, new List<IMarkGeometry>() { pData.Point });
                case "LINE":
                    var lData = ParseLine(readerIn);
                    return (lData.LayerName, new List<IMarkGeometry>() { lData.Line });
                case "CIRCLE":
                    var cData = ParseCircle(readerIn);
                    return (cData.LayerName, new List<IMarkGeometry>() { cData.Circle });
                case "ARC":
                    var aData = ParseArc(readerIn);
                    return (aData.LayerName, new List<IMarkGeometry>() { aData.Arc });
                case "LWPOLYLINE":
                    return ParseLWPolyline(readerIn);
                case "SPLINE":
                    var sData = ParseSpline(readerIn);
                    return (sData.LayerName, new List<IMarkGeometry>() { sData.Spline });
            }

            return (null, null);
        }

        #endregion

        #region Section: Public Static Methods

        public static bool Save(string filePathIn, IList<IMarkGeometry> geometriesIn)
        {

            using(var file = new StreamWriter(filePathIn))
            {
                WriteHeader(file);

                file.Write($"\n0\nSECTION\n2\nENTITIES");
                foreach (var geometry in geometriesIn)
                {
                    WriteGeometry(file, geometry);
                }
                file.Write($"\n0\nENDSEC\n0\nEOF");
            }

            return File.Exists(filePathIn);
        }

        #endregion

        #region Section: Private Helpers

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
            var (success, layerName) = ReadLayerName(readerIn, MatchSplineEntity);

            if (success)
            {
                var result1 = readerIn.FindConsecutiveLines(
                    MatchSubClassMarker,
                    MatchSplineEntity
                );

                if (!result1.Success)
                    return (null, null);
            }

            // read spline flag
            int flag = ReadInteger(readerIn, "70");

            // read degree of spline curve
            int degree = ReadInteger(readerIn, "71");

            // read number of knots
            int nKnots = ReadInteger(readerIn, "72");

            // read number of control points
            int nControlPoints = ReadInteger(readerIn, "73");

            // read number of fit points
            int nFitPoints = ReadInteger(readerIn, "74");

            var knots = new List<double>(nKnots);
            for (int i = 0; i < nKnots; i++)
            {
                knots.Add(ReadDouble(readerIn, "40"));
            }

            var controlPoints = new List<MarkGeometryPoint>(nControlPoints);
            for (int i = 0; i < nControlPoints; i++)
            {
                controlPoints.Add(ReadPointFast(readerIn));
            }

            var fitPoints = new List<MarkGeometryPoint>(nFitPoints);
            for (int i = 0; i < nFitPoints; i++)
            {
                fitPoints.Add(ReadPointFast(readerIn));
            }

            return (layerName, new MarkGeometrySpline(flag, degree, knots, controlPoints, fitPoints));
        }

        private (string LayerName, List<IMarkGeometry> Path) ParseLWPolyline(AdvancedLineStreamReader readerIn)
        {
            var (success, layerName) = ReadLayerName(readerIn, MatchLWPolylineEntity);

            if (success)
            {
                var result1 = readerIn.FindConsecutiveLines(
                    MatchSubClassMarker,
                    MatchLWPolylineEntity
                );

                if (!result1.Success)
                    return (null, null);
            }

            readerIn.ReadLine(); // consume number of vertices 90
            int numberOfVertices = int.Parse(readerIn.ReadLine());
            int flag = ReadInteger(readerIn, "70");

            var bulges = new List<double>(numberOfVertices);
            var points = new List<MarkGeometryPoint>(numberOfVertices-1);

            for (int i = 0; i < numberOfVertices; i++)
            {
                points.Add(ReadPointFast2D(readerIn));
                bulges.Add((readerIn.PeekLine().Trim() == "42") ? ReadDouble(readerIn, "42") : 0d);
            }

            if (points.Count > 0 && flag == 1) // i.e. is closed
                points.Add(points[0]);

            var buffer = new List<IMarkGeometry>();
            var path = new MarkGeometryPath();

            for (int i = 0; i < points.Count-1; i++)
            {
                var p1 = points[i];
                var p2 = points[i+1];
                var bulge = bulges[i];

                if (Math.Abs(bulge) <= double.Epsilon)
                {
                    path.Add(p1, true);
                    path.Add(p2, true);
                }
                else
                {
                    if (path.Points.Count > 0)
                    {
                        path.Update(); // force path to re-compute it's properties
                        buffer.Add(path);
                        path = new MarkGeometryPath();
                    }

                    buffer.Add(new MarkGeometryArc(p1, p2, bulge));
                }
            }

            if (path.Points.Count > 0)
            {
                path.Update(); // force path to re-compute it's properties
                buffer.Add(path);
            }

            return (layerName, buffer);
        }

        private (string LayerName, MarkGeometryPath Path) ParsePolyline(AdvancedLineStreamReader readerIn)
        {
            var (success, layerName) = ReadLayerName(readerIn, MatchPolylineEntity);

            if (success)
            {
                var result = readerIn.FindConsecutiveLines(
                    MatchSubClassMarker,
                    MatchPolylineEntity
                );

                if (!result.Success)
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

            var centrePoint = ReadPointFast(readerIn);

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
            var startAngle = double.Parse(readerIn.ReadLine());

            // read angle 60
            readerIn.ReadLine();
            var endAngle = double.Parse(readerIn.ReadLine());

            var arc = new MarkGeometryArc(
                centrePoint,
                radius, // convert angle to radians
                GeometricArithmeticModule.ToRadians(startAngle),
                GeometricArithmeticModule.ToRadians(endAngle)
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

        private (bool Success, string LayerName) ReadLayerName(AdvancedLineStreamReader readerIn, string terminationIn)
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

        private double ReadDoubleAdvanced(AdvancedLineStreamReader readerIn, string paramCode)
        {
            readerIn.FindLine(paramCode);
            var line = readerIn.ReadLine();

            if (line.Contains("e"))
            {
                var components = line.Split('e');
                return double.Parse(components[0]) * Math.Pow(10, double.Parse(components[1]));
            }

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

        #endregion

        #region Section: Static Private Helpers

        private static void WriteHeader(StreamWriter writerIn)
        {
            writerIn.Write(
                $"999\nM-Solv DXF Parser (2020) written by Chibuike Okpaluba\n0\nSECTION\n2\nHEADER\n9\n$ACADVER\n1\n{SupportedVersions.Last()}\n0\nENDSEC"
            );
        }

        private static void WriteGeometry(StreamWriter writerIn, IMarkGeometry geometryIn, string layerName="0")
        {

            if (geometryIn is MarkGeometryPoint point)
            {
                writerIn.Write($"\n0\nPOINT\n100\nAcDbEntity\n8\n{layerName}100\nAcDbPoint\n10\n{point.X}\n20\n{point.Y}\n30\n{point.Z}");
            }
            else if (geometryIn is MarkGeometryLine line)
            {
                writerIn.Write($"\n0\nLINE\n100\nAcDbEntity\n8\n{layerName}100\nAcDbLine\n10\n{line.StartPoint.X}\n20\n{line.StartPoint.Y}\n30\n{line.StartPoint.Z}\n11\n{line.EndPoint.X}\n21\n{line.EndPoint.Y}\n31\n{line.EndPoint.Z}");
            }
            else if (geometryIn is MarkGeometryCircle circle)
            {
                writerIn.Write($"\n0\nCIRCLE\n100\nAcDbEntity\n8\n{layerName}100\nAcDbCircle\n10\n{circle.CentrePoint.X}\n20\n{circle.CentrePoint.Y}\n30\n{circle.CentrePoint.Z}\n40\n{circle.Radius}");
            }
            else if (geometryIn is MarkGeometryArc arc)
            {
                writerIn.Write($"\n0\nARC\n100\nAcDbEntity\n8\n{layerName}100\nAcDbCircle\n10\n{arc.CentrePoint.X}\n20\n{arc.CentrePoint.Y}\n30\n{arc.CentrePoint.Z}\n40\n{arc.Radius}\n100\nAcDbArc\n50\n{arc.StartAngle}\n51\n{arc.EndAngle}");
            }
            else if (geometryIn is MarkGeometrySpline spline)
            {
                int flag = spline.IsClosed ? 1 : spline.IsPeriodic ? 2 : 8;
                writerIn.Write($"\n0\nSPLINE\n100\nAcDbEntity\n8\n{layerName}100\nAcDbSpline\n210\n0\n220\n0\n230\n0\n70\n{flag}\n71\n{spline.Degree}\n72\n{spline.Knots.Count}\n73\n{spline.ControlPoints.Count}\n74\n{spline.FitPoints.Count}\n42\n1e-007\n43\n1e-007\n44\n1e-007");

                foreach (var knot in spline.Knots)
                {
                    writerIn.Write($"\n40\n{knot}");
                }

                foreach (var cPoint in spline.ControlPoints)
                {
                    writerIn.Write($"\n10\n{cPoint.X}\n20\n{cPoint.Y}\n30\n{cPoint.Z}");
                }

                foreach (var fPoint in spline.FitPoints)
                {
                    writerIn.Write($"\n11\n{fPoint.X}\n21\n{fPoint.Y}\n31\n{fPoint.Z}");
                }
            }
            else if (geometryIn is MarkGeometryPath path)
            {
                writerIn.Write($"\n0\nLWPOLYLINE\n100\nAcDbEntity\n8\n{layerName}100\nAcDbPolyline\n90\n{path.Points.Count}\n70\n0");

                foreach (var pPoint in path.Points)
                {
                    writerIn.Write($"\n10\n{pPoint.X}\n20\n{pPoint.Y}");
                }
            }
        }

        #endregion
    }
}
