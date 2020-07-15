using MarkGeometriesLib.Classes.DXFParser;
using MSolvLib.MarkGeometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace MarkGeometriesLib.Classes.DRLParser
{

    /// <summary>
    /// Adpated from https://www.artwork.com/gerber/drl2laser/excellon/index.htm
    /// and https://www.artwork.com/gerber/appl2.htm
    /// </summary>
    public class DRLParser
    {
        #region Section: Private Static Properties
        
        // match file content
        private static readonly Regex MatchXValue = new Regex(@"X(\-{0,1}((\d+(\.\d+)?)|(\.\d+)))", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchYValue = new Regex(@"Y(\-{0,1}((\d+(\.\d+)?)|(\.\d+)))", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchEndOfFile = new Regex(@"^(\s)*(M30)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchDrillCommand = new Regex(@"^(\s)*(X|Y)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchValueCommand = new Regex(@"(X|Y)(\-{0,1}((\d+(\.\d+)?)|(\.\d+)))", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchToolSelectCommand = new Regex(@"^\s*(T\d+)", RegexOptions.Compiled | RegexOptions.Singleline);

        // match header content
        private static readonly Regex MatchCoordinatesCommand = new Regex(@"^\s*(INCH|METRIC)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchDecimalPointNotationStyleCommand = new Regex(@"(TZ|LZ)\s*$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchEndOfHeaderCommand = new Regex(@"^(\s)*(M95)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchVersionCommand = new Regex(@"^\s*VER,\s*(\d+)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchFormatCommand = new Regex(@"^\s*FMAT,\s*(\d+)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchToolCommand = new Regex(@"^\s*(T\d+)C((\d+(\.\d+)?)|(\.\d+))", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchTurnOnRoutingModeCommand = new Regex(@"^\s*(G00)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchTurnOnDrillModeCommand = new Regex(@"^\s*(G81|G05)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchBeginPatternCommand = new Regex(@"^\s*(M25)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchEndPatternCommand = new Regex(@"^\s*(M01)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchRepeatPatternOffsetV1Command = new Regex(@"^\s*(M26)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchRepeatPatternOffsetV2Command = new Regex(@"^\s*(M02)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchEndOfStepAndRepeatCommand = new Regex(@"^\s*(M08)", RegexOptions.Compiled | RegexOptions.Singleline);
        //private static readonly Regex MatchVersionCommand = new Regex(@"", RegexOptions.Compiled | RegexOptions.Singleline);
        //private static readonly Regex MatchVersionCommand = new Regex(@"", RegexOptions.Compiled | RegexOptions.Singleline);

        #endregion

        #region Section: Private Properties

        private long _count;
        private string _filePath;

        #endregion

        #region Section: Public Properties
        
        public long Count
        {
            get
            {
                return _count;
            }
        }

        public CoordinateSystem CoordinateSystem { get; protected set; } = CoordinateSystem.METRIC;

        /// <summary>
        /// LZ indicates that the leading zeros in the coordinate data are included.
        /// </summary>
        public TrailingZeroSuppressionStyle TrailingZeroSuppressionStyle { get; protected set; } = TrailingZeroSuppressionStyle.BOTH;

        /// <summary>
        /// Decimal Point Suppression
        /// </summary>
        public double UnitConversionScale { get; protected set; } = 1d;

        /// <summary>
        /// Use version 1 X and Y axis layout. (As opposed to Version 2)
        /// </summary>
        public int Version { get; protected set; } = 1;

        /// <summary>
        /// Use Format 2 commands; alternative would be FMAT,1
        /// </summary>
        public int Format { get; protected set; } = 1;

        /// <summary>
        /// Defines tool 01 as having a diameter of 0.020 inch. 
        /// For each tool used in the data the diameter should be defined here. 
        /// There are additional parameters but if you are a PCB designer 
        /// it is not up to you to specify feed rates and such.
        /// </summary>
        public Dictionary<string, double> Tools { get; protected set; } = new Dictionary<string, double>();

        #endregion

        #region Section: Constructor
        
        /// <summary>
        ///     Adpated from https://www.artwork.com/gerber/drl2laser/excellon/index.htm
        /// </summary>
        /// <param name="filePathIn"></param>
        public DRLParser(string filePathIn)
        {
            if (!File.Exists(filePathIn))
                throw new FileNotFoundException($"Could not find `{filePathIn}`");

            _filePath = filePathIn;

            // automatically load header
            ReadHeader();
            EstimateUnitConversionScale();
        }

        #endregion

        #region Section: Private Class Helpers

        private void ReadHeader()
        {
            using (var reader = new AdvancedLineStreamReader(_filePath))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine()?.Trim();

                    if (MatchCoordinatesCommand.IsMatch(line))
                    {
                        CoordinateSystem = MatchCoordinatesCommand.Matches(line)[0].Groups[1].Value.StartsWith("METRIC", StringComparison.OrdinalIgnoreCase) ? CoordinateSystem.METRIC : CoordinateSystem.INCH;
                        if (MatchDecimalPointNotationStyleCommand.IsMatch(line))
                        {
                            TrailingZeroSuppressionStyle = line.EndsWith("LZ", StringComparison.OrdinalIgnoreCase) ? TrailingZeroSuppressionStyle.LZ : TrailingZeroSuppressionStyle.TZ;
                        }
                    }
                    else if (MatchVersionCommand.IsMatch(line))
                    {
                        Version = int.Parse(MatchVersionCommand.Matches(line)[0].Groups[1].Value);
                    }
                    else if (MatchFormatCommand.IsMatch(line))
                    {
                        Format = int.Parse(MatchFormatCommand.Matches(line)[0].Groups[1].Value);
                    }
                    else if (MatchToolCommand.IsMatch(line))
                    {
                        var match = MatchToolCommand.Matches(line)[0];
                        Tools[match.Groups[1].Value] = double.Parse(match.Groups[2].Value);
                    }
                    else if (
                        MatchDrillCommand.IsMatch(line) ||
                        MatchEndOfHeaderCommand.IsMatch(line)
                    )
                    {
                        break;
                    }
                }
            }
        }

        private void EstimateUnitConversionScale()
        {
            using (var reader = new AdvancedLineStreamReader(_filePath))
            {
                while (!reader.EndOfStream)
                {
                    var (successful, line) = reader.FindLineUntil(MatchDrillCommand, MatchEndOfFile);

                    if (successful)
                    {
                        var value = MatchValueCommand.Matches(line)[0].Groups[2].Value.Trim('-');

                        if (CoordinateSystem == CoordinateSystem.INCH)
                            SetUnitConversionScale(1d / 10000d);
                        else if (value.Length <= 5)
                            SetUnitConversionScale(1d / 100);
                        else
                            SetUnitConversionScale(1d / 1000);
                    }

                    break;
                }
            }
        } 

        private double ParseValue(string value)
        {
            return double.Parse(value) * UnitConversionScale;
        }

        #endregion

        public void SetUnitConversionScale(double unitConversionScale)
        {
            UnitConversionScale = unitConversionScale;
        }

        /// <summary>
        ///     Read DRL data as points.
        /// </summary>
        /// <param name="howmany">The number of elements to read; use -1 to read all.</param>
        /// <returns>A list of points</returns>
        public List<IMarkGeometry> ReadPoints(long howmany = -1)
        {
            var buffer = new List<IMarkGeometry>();

            BeginGetAll(
                (data) => 
                {
                    if (data.Geometry is MarkGeometryPoint point)
                    {
                        buffer.Add(point);
                    }
                    else
                    {
                        buffer.Add(data.Geometry);
                    }
                }, 
                howmany
            );

            return buffer;
        }

        /// <summary>
        ///     Read DRL data as circles.
        /// </summary>
        /// <param name="howmany">The number of elements to read; use -1 to read all.</param>
        /// <returns>A list of circles</returns>
        public List<IMarkGeometry> ReadCircles(long howmany = -1)
        {
            var buffer = new List<IMarkGeometry>();

            BeginGetAll(
                (data) =>
                {
                    if (data.Geometry is MarkGeometryPoint point)
                    {
                        buffer.Add(new MarkGeometryCircle(point, 0.5 * data.ToolDiameter));
                    }
                    else
                    {
                        buffer.Add(data.Geometry);
                    }
                },
                howmany
            );

            return buffer;
        }

        /// <summary>
        ///     Read DRL data replacing the drill positions with a custom pattern
        /// </summary>
        /// <param name="pattern">A pattern relative to it's origin 0,0</param>
        /// <param name="howmany">The number of elements to read</param>
        /// <returns>A list of geometries</returns>
        public List<IMarkGeometry> ReadAndReplaceWithCustom(List<IMarkGeometry> pattern, long howmany = -1)
        {
            var buffer = new List<IMarkGeometry>();

            BeginGetAll(
                (data) =>
                {
                    if (data.Geometry is MarkGeometryPoint point)
                    {
                        var transform = GeometricArithmeticModule.GetTranslationTransformationMatrix(point);

                        for (int i = 0; i < pattern.Count; i++)
                        {
                            var clone = (IMarkGeometry)pattern[i].Clone();
                            clone.Transform(transform);
                            buffer.Add(clone);
                        }
                    }
                    else
                    {
                        buffer.Add(data.Geometry);
                    }
                },
                howmany
            );

            return buffer;
        }

        #region Section: Obsolete

        ///// <summary>
        /////     Use method to get DRL positions.
        ///// </summary>
        ///// <param name="callback">Action to recieve to positions</param>
        ///// <param name="howmany">The number of elements to fetch</param>
        //public void BeginGetAll(Action<(double X, double Y, string toolName, double ToolDiameter)> callback, long howmany = -1)
        //{
        //    _count = 0;
        //    using (var reader = new AdvancedLineStreamReader(_filePath))
        //    {
        //        double x = 0;
        //        double y = 0;
        //        string toolName = "0";
        //        double toolDiameter = 0.1;

        //        while ((howmany < 0 || (_count < howmany)) && !reader.EndOfStream)
        //        {
        //            var line = reader.ReadLine()?.Trim();

        //            if (MatchDrillCommand.IsMatch(line))
        //            {
        //                if (MatchXValue.IsMatch(line))
        //                {
        //                    x = ParseValue(MatchXValue.Matches(line)[0].Groups[1].Value);
        //                }

        //                if (MatchYValue.IsMatch(line))
        //                {
        //                    y = ParseValue(MatchYValue.Matches(line)[0].Groups[1].Value);
        //                }

        //                callback((x, y, toolName, toolDiameter));
        //            }
        //            else if (MatchToolSelectCommand.IsMatch(line))
        //            {
        //                var _toolName = MatchToolSelectCommand.Matches(line)[0].Groups[1].Value;
        //                if (Tools.ContainsKey(_toolName))
        //                {
        //                    toolName = _toolName;
        //                    toolDiameter = Tools[toolName];
        //                }
        //            }

        //            _count++;
        //        }
        //    }
        //} 

        #endregion

        /// <summary>
        ///     Use method to get DRL positions.
        ///     adapted from https://gist.github.com/katyo/5692b935abc085b1037e
        /// </summary>
        /// <param name="callback">Action to recieve to positions</param>
        /// <param name="howmany">The number of elements to fetch</param>
        public void BeginGetAll(Action<(IMarkGeometry Geometry, string ToolName, double ToolDiameter)> callback, long howmany = -1)
        {
            _count = 0;
            using (var reader = new AdvancedLineStreamReader(_filePath))
            {
                double x = 0;
                double y = 0;
                double xOffset = 0;
                double yOffset = 0;
                string toolName = "0";
                double toolDiameter = 0.1;
                bool inRoutingMode = false;
                bool isTrackingPattern = false;
                var buffer = new List<(IMarkGeometry Geometry, string ToolName, double ToolDiameter)>();

                while ((howmany < 0 || (_count < howmany)) && !reader.EndOfStream)
                {
                    var line = reader.ReadLine()?.Trim();

                    if (MatchDrillCommand.IsMatch(line))
                    {
                        if (MatchXValue.IsMatch(line))
                        {
                            x = ParseValue(MatchXValue.Matches(line)[0].Groups[1].Value);
                        }

                        if (MatchYValue.IsMatch(line))
                        {
                            y = ParseValue(MatchYValue.Matches(line)[0].Groups[1].Value);
                        }

                        if (inRoutingMode)
                        {

                        }
                        else
                        {
                            var pattern = (new MarkGeometryPoint(x, y), toolName, toolDiameter);
                            callback(pattern);

                            if (isTrackingPattern)
                                buffer.Add(pattern);
                        }
                    }
                    else if (MatchToolSelectCommand.IsMatch(line))
                    {
                        var _toolName = MatchToolSelectCommand.Matches(line)[0].Groups[1].Value;
                        if (Tools.ContainsKey(_toolName))
                        {
                            toolName = _toolName;
                            toolDiameter = Tools[toolName];
                        }
                    }
                    else if (MatchTurnOnRoutingModeCommand.IsMatch(line))
                    {
                        inRoutingMode = true;

                        if (MatchXValue.IsMatch(line))
                        {
                            x = ParseValue(MatchXValue.Matches(line)[0].Groups[1].Value);
                        }

                        if (MatchYValue.IsMatch(line))
                        {
                            y = ParseValue(MatchYValue.Matches(line)[0].Groups[1].Value);
                        }

                        continue;
                    }
                    else if (MatchTurnOnDrillModeCommand.IsMatch(line))
                    {
                        inRoutingMode = false;
                        continue;
                    }
                    else if (MatchBeginPatternCommand.IsMatch(line))
                    {
                        xOffset = 0;
                        yOffset = 0;
                        buffer.Clear();
                        isTrackingPattern = true;

                        continue;
                    }
                    else if (MatchEndPatternCommand.IsMatch(line))
                    {
                        isTrackingPattern = false;
                        continue;
                    }
                    else if (MatchRepeatPatternOffsetV2Command.IsMatch(line))
                    {
                        bool containsOffsetsData = false;

                        if (MatchXValue.IsMatch(line))
                        {
                            containsOffsetsData = true;
                            xOffset += ParseValue(MatchXValue.Matches(line)[0].Groups[1].Value);
                        }

                        if (MatchYValue.IsMatch(line))
                        {
                            containsOffsetsData = true;
                            yOffset += ParseValue(MatchYValue.Matches(line)[0].Groups[1].Value);
                        }

                        if (containsOffsetsData)
                        {
                            var offsetTransform = GeometricArithmeticModule.GetTranslationTransformationMatrix(
                                xOffset, yOffset
                            );

                            for (int i = 0; i < buffer.Count; i++)
                            {
                                (IMarkGeometry Geometry, string ToolName, double ToolDiameter) pattern = ((IMarkGeometry)buffer[i].Geometry.Clone(), buffer[i].ToolName, buffer[i].ToolDiameter);
                                
                                pattern.Geometry.Transform(offsetTransform);

                                callback(pattern);

                                if (isTrackingPattern)
                                    buffer.Add(pattern);
                            }
                        }
                        continue;
                    }
                    else if (MatchEndOfStepAndRepeatCommand.IsMatch(line))
                    {
                        xOffset = 0;
                        yOffset = 0;
                        buffer.Clear();
                        continue;
                    }

                    _count++;
                }
            }
        }
    }
}
