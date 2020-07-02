using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MSolvLib.MarkGeometry.Helpers
{
    public class DXFParser
    {
        private static Regex MatchVersion = new Regex(@"^\s*\$ACADVER$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex MatchSupportedEntities = new Regex(@"^\s*(LINE|CIRCLE)$", RegexOptions.Compiled | RegexOptions.Singleline);//ARC|LINE
        private static Regex MatchLayerOrArc = new Regex(@"^\s*(8|AcDbArc|AcDbCircle)$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex MatchLayerOrCircle = new Regex(@"^\s*(8|AcDbCircle)$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex MatchLayerOrLine = new Regex(@"^\s*(8|AcDbLine)$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex MatchGroupCodeForStartCentreEndPointX = new Regex(@"^\s*(10|11)$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex MatchGroupCodeForStartCentreEndPointY = new Regex(@"^\s*(20|21)$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex MatchGroupCodeForStartCentreEndPointZ = new Regex(@"^\s*(30|22)$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex MatchGroupCodeForRadius = new Regex(@"^\s*(40)$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex MatchGroupCodeForStartAngle = new Regex(@"^\s*(50)$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex MatchGroupCodeForEndAngle = new Regex(@"^\s*(51)$", RegexOptions.Compiled | RegexOptions.Singleline);

        private static (bool Found, string Result) SkipUntil(StreamReader readerIn, Regex pattern)
        {
            while (!readerIn.EndOfStream)
            {
                // read line
                var line = readerIn.ReadLine();

                if (pattern.IsMatch(line))
                    return (true, line);
            }

            return (false, null);
        }

        public static string ReadVersion(string filePathIn)
        {
            if (!File.Exists(filePathIn))
                throw new FileNotFoundException($"could not find {filePathIn}");

            using (var reader = new StreamReader(filePathIn))
            {
                if (SkipUntil(reader, MatchVersion).Found)
                {
                    // skip numeric tag `1`
                    reader.ReadLine();
                    var version = reader.ReadLine();

                    switch (version.Trim())
                    {
                        case "AC1006":
                            return "R10";
                        case "AC1009":
                            return "R11";
                        case "AC1012":
                            return "R13";
                        case "AC1014":
                            return "R14";
                        case "AC1015":
                            return "AutoCAD 2000";
                        case "AC1018":
                            return "AutoCAD 2004";
                        case "AC1021":
                            return "AutoCAD 2007";
                        case "AC1024":
                            return "AutoCAD 2010";
                        default:
                            return version;
                    }
                }
            }

            return null;
        }

        public static Dictionary<string, List<IMarkGeometry>> ReadLabelledGeometries(string filePathIn)
        {
            var geometries = new Dictionary<string, List<IMarkGeometry>>();
            return geometries;
        }

        public static List<IMarkGeometry> ReadAll(string filePathIn)
        {
            var geometries = new List<IMarkGeometry>();

            using (var reader = new StreamReader(filePathIn))
            {
                while (!reader.EndOfStream)
                {
                    // skip to next supported entity
                    var (successful, line) = SkipUntil(reader, MatchSupportedEntities);

                    // if unsuccessful terminate end of file reached
                    if (!successful)
                        break;

                    // create buffer to hold results
                    (string Layer, IMarkGeometry Geometry) result;

                    // parse and add geometry
                    switch (MatchSupportedEntities.Match(line).Value.Trim())
                    {
                        case "ARC":
                            result = TryParseArc(reader);
                            if (result.Geometry != null)
                                geometries.Add(result.Geometry);
                            break;
                        case "CIRCLE":
                            result = TryParseCircle(reader);
                            if (result.Geometry != null)
                                geometries.Add(result.Geometry);
                            break;
                        case "LINE":
                            result = TryParseLine(reader);
                            if (result.Geometry != null)
                                geometries.Add(result.Geometry);
                            break;
                        default:
                            throw new Exception($"Matched entity is not supported `{line}`");
                    }
                }
            }

            return geometries;
        }

        private static (string Layer, IMarkGeometry Geometry) TryParseArc(StreamReader readerIn)
        {
            string layerName = null;
            IMarkGeometry geometry = null;

            double radius = 0;
            MarkGeometryPoint centre = new MarkGeometryPoint();

            while (true)
            {
                var (found, line) = SkipUntil(readerIn, MatchLayerOrArc);

                if (!found)
                    break;

                switch (MatchLayerOrArc.Match(line).Value.Trim())
                {
                    case "8":
                        layerName = readerIn.ReadLine();
                        break;
                    case "AcDbCircle":
                        centre = TryParsePoint(readerIn);
                        if (centre == null)
                            throw new Exception("Failed to parse centre point of arc");

                        if (!SkipUntil(readerIn, MatchGroupCodeForRadius).Found)
                            throw new Exception("Failed to parse arc radius");

                        radius = double.Parse(readerIn.ReadLine());
                        break;
                    case "AcDbArc":
                        if (!SkipUntil(readerIn, MatchGroupCodeForStartAngle).Found)
                            throw new Exception("Failed to parse arc start's angle");

                        double startAngle = double.Parse(readerIn.ReadLine());

                        if (!SkipUntil(readerIn, MatchGroupCodeForEndAngle).Found)
                            throw new Exception("Failed to parse arc end's angle");

                        double endAngle = double.Parse(readerIn.ReadLine());

                        geometry = new MarkGeometryArc(centre, radius, startAngle, endAngle);
                        return (layerName, geometry);

                    default:
                        throw new Exception($"Matched circle attribute is not supported: `{line}`");
                }
            }

            return (layerName, geometry);
        }

        private static (string Layer, IMarkGeometry Geometry) TryParseLine(StreamReader readerIn)
        {
            string layerName = null;
            IMarkGeometry geometry = null;

            while (true)
            {
                var (found, line) = SkipUntil(readerIn, MatchLayerOrLine);

                if (!found)
                    break;

                switch (MatchLayerOrLine.Match(line).Value.Trim())
                {
                    case "8":
                        layerName = readerIn.ReadLine();
                        break;
                    case "AcDbLine":
                        var startPoint = TryParsePoint(readerIn);
                        var endPoint = TryParsePoint(readerIn);
                        geometry = new MarkGeometryLine(startPoint, endPoint);
                        return (layerName, geometry);
                    default:
                        throw new Exception($"Matched circle attribute is not supported: `{line}`");
                }
            }

            return (layerName, geometry);
        }

        private static (string Layer, IMarkGeometry Geometry) TryParseCircle(StreamReader readerIn)
        {
            string layerName = null;
            IMarkGeometry geometry = null;

            while(true)
            {
                var (found, line) = SkipUntil(readerIn, MatchLayerOrCircle);

                if (!found)
                    break;

                switch(MatchLayerOrCircle.Match(line).Value.Trim())
                {
                    case "8":
                        layerName = readerIn.ReadLine();
                        break;
                    case "AcDbCircle":
                        MarkGeometryPoint centre = TryParsePoint(readerIn);
                        if (centre == null)
                            throw new Exception("Failed to parse centre point of arc");

                        if (!SkipUntil(readerIn, MatchGroupCodeForRadius).Found)
                            throw new Exception("Failed to parse arc radius");

                        double radius = double.Parse(readerIn.ReadLine());

                        geometry = new MarkGeometryCircle(centre, radius);
                        return (layerName, geometry);
                    default:
                        throw new Exception($"Matched circle attribute is not supported: `{line}`");
                }
            }

            return (layerName, geometry);
        }

        private static MarkGeometryPoint TryParsePoint(StreamReader readerIn)
        {
            if (!SkipUntil(readerIn, MatchGroupCodeForStartCentreEndPointX).Found)
                return null;

            double x = double.Parse(readerIn.ReadLine());

            if (!SkipUntil(readerIn, MatchGroupCodeForStartCentreEndPointY).Found)
                return null;

            double y = double.Parse(readerIn.ReadLine());

            double z = 0;
            if ((char)readerIn.Peek() == '3')
            {
                if (!SkipUntil(readerIn, MatchGroupCodeForStartCentreEndPointZ).Found)
                    return null;

                z = double.Parse(readerIn.ReadLine());
            }

            return new MarkGeometryPoint(x, y, z);
        }
    }
}
