using ContourHatcher.Utils;
using MRecipeStructure.Classes.MRecipeStructure;
using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using MSolvLib.MarkGeometry;
using netDxf;
using netDxf.Header;
using STLSlicer.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace MRecipeStructure.Classes.MRecipeStructure.Utils
{
    public class MSTLSlice
    {
        private double _totalContourMarkDistance;
        private double _totalHatchesMarkDistance;
        private double _totalContourJumpDistance;
        private double _totalHatchesJumpDistance;
        private ContourQuadTree _contourQuadTree;

        public List<MarkGeometryLine> HatchLines { get; set; }
        public List<MarkGeometryLine> ContourLines { get; set; }
        public List<MarkGeometryRectangle> Tiles { get; set; }
        public GeometryExtents<double> Extents { get; set; }
        public double MinVectorLength { get; set; }
        public double TotalMarkDistance => _totalContourMarkDistance + _totalHatchesMarkDistance;
        public double TotalJumpDistance => _totalContourJumpDistance + _totalHatchesJumpDistance;

        public int NumberOfContours { get; set; }
        public int NumberOfJoints { get; set; }

        public MSTLSlice()
        {
            // refers to the total number of joints in the
            // given contours. Currently used to estimate
            // the effects of the polygon delay.
            NumberOfJoints = 0;

            // refers to the sum of all jumps required to
            // mark this pattern.
            _totalContourJumpDistance = 0;
            _totalHatchesJumpDistance = 0;

            NumberOfContours = 0;
            _totalContourMarkDistance = 0;
            _totalHatchesMarkDistance = 0;
            MinVectorLength = double.MaxValue;
            ContourLines = new List<MarkGeometryLine>();
            HatchLines = new List<MarkGeometryLine>();
            Tiles = new List<MarkGeometryRectangle>();
            Extents = new GeometryExtents<double>();
        }

        public MSTLSlice(List<MContourStructure> contours)
            : this()
        {
            NumberOfContours = contours.Count;

            MarkGeometryPoint lastPoint = null;

            foreach (var contourStructure in contours)
            {
                var (lines, minLineLength, perimeter) = GeometricArithmeticModule.GetLinesAndStatistics(
                    contourStructure.ToPoints()
                );

                ContourLines.AddRange(
                    lines
                );

                if (minLineLength < MinVectorLength)
                    MinVectorLength = minLineLength;

                _totalContourMarkDistance += perimeter;
                NumberOfJoints += (lines.Count - 1);

                if (lines.Count > 0)
                {
                    if (lastPoint != null)
                    {
                        // measure and track the jump distance between the last contour and this
                        _totalContourJumpDistance += GeometricArithmeticModule.ABSMeasure(lastPoint, lines[0].StartPoint);
                    }

                    lastPoint = lines[0].StartPoint;
                }
            }

            _contourQuadTree = new ContourQuadTree(ContourLines);
            Extents = GeometricArithmeticModule.CalculateExtents(ContourLines);
        }

        public bool GenerateHatches(MHatchSettings settings)
        {
            HatchLines.Clear();
            _totalHatchesJumpDistance = 0;
            _totalHatchesMarkDistance = 0;

            var lines = new List<MarkGeometryLine>();
            var angleRad = GeometricArithmeticModule.ToRadians(settings.Angle);

            var size = Extents.Hypotenuse;
            var howmany = (int)Math.Ceiling(size / settings.Pitch);
            var yStart = Extents.Centre.Y - (0.5 * size);

            // generate lines to calculate intersections for hatch
            if (settings.Style == HatchStyle.RASTER || settings.Style == HatchStyle.RASTER_GRID)
            {
                for (int i = 0; i < howmany; i++)
                {
                    double y = yStart + (i * settings.Pitch);

                    var line = new MarkGeometryLine(
                        new MarkGeometryPoint(
                            -size + Extents.Centre.X, y
                        ),
                        new MarkGeometryPoint(
                            size + Extents.Centre.X, y
                        )
                    );

                    // apply angular rotation
                    GeometricArithmeticModule.Rotate(line, 0, 0, angleRad, Extents.Centre.X, Extents.Centre.Y, Extents.Centre.Z);

                    lines.Add(line);
                }
            }
            else if (settings.Style == HatchStyle.SERPENTINE || settings.Style == HatchStyle.SERPENTINE_GRID)
            {
                for (int i = 0; i < howmany; i++)
                {
                    double y = yStart + (i * settings.Pitch);

                    if (i % 2 == 0)
                    {
                        var line = new MarkGeometryLine(
                            new MarkGeometryPoint(
                                -size + Extents.Centre.X, y
                            ),
                            new MarkGeometryPoint(
                                size + Extents.Centre.X, y
                            )
                        );

                        // apply angular rotation
                        GeometricArithmeticModule.Rotate(line, 0, 0, angleRad, Extents.Centre.X, Extents.Centre.Y, Extents.Centre.Z);

                        lines.Add(line);
                    }
                    else
                    {
                        var line = new MarkGeometryLine(
                            new MarkGeometryPoint(
                                size + Extents.Centre.X, y
                            ),
                            new MarkGeometryPoint(
                                -size + Extents.Centre.X, y
                            )
                        );

                        // apply angular rotation
                        GeometricArithmeticModule.Rotate(line, 0, 0, angleRad, Extents.Centre.X, Extents.Centre.Y, Extents.Centre.Z);

                        lines.Add(line);
                    }
                }
            }

            // duplicate lines if using grid
            var perpendicularAngleForGridLines = GeometricArithmeticModule.ToRadians(90);
            if (settings.Style == HatchStyle.RASTER_GRID || settings.Style == HatchStyle.SERPENTINE_GRID)
            {
                int startIndex = lines.Count - 1;
                for (int i = startIndex; i >= 0; i--)
                {
                    var ln = (MarkGeometryLine)lines[i].Clone();
                    GeometricArithmeticModule.Rotate(ln, 0, 0, perpendicularAngleForGridLines, Extents.Centre.X, Extents.Centre.Y, Extents.Centre.Z);

                    lines.Add(ln);
                }
            }

            // used to track jumps
            MarkGeometryPoint lastPoint = null;

            // generate hatch lines with extension
            for (int i = 0; i < lines.Count; i++)
            {
                List<MarkGeometryPoint> intersections = _contourQuadTree.Intersect(lines[i])?.ToList();

                if (intersections == null)
                    continue;

                int startIndex = (settings.Invert) ? 1 : 0;
                int endIndex = intersections.Count - 1;

                while (startIndex < endIndex)
                {
                    var hatch = new MarkGeometryLine(
                        intersections[startIndex], intersections[startIndex + 1]
                    );

                    HatchLines.Add(hatch);

                    // increase mark and jump distance
                    if (lastPoint != null)
                        _totalHatchesJumpDistance += GeometricArithmeticModule.ABSMeasure2D(
                            lastPoint, hatch.StartPoint
                        );
                    _totalHatchesMarkDistance += hatch.Length;

                    lastPoint = hatch.EndPoint;
                    startIndex += 2;
                }
            }

            return true;
        }

        public bool GenerateTiles(MTileSettings settings)
        {
            // delete previous tiles
            Tiles.Clear();

            double refWidth = Extents.Width + settings.XPadding;
            double refHeight = Extents.Height + settings.YPadding;

            int nRows = (int)Math.Ceiling(refHeight / settings.YSize);
            int nColumns = (int)Math.Ceiling(refWidth / settings.XSize);

            var _halfTileWidth = 0.5 * settings.XSize;
            var _halfTileHeight = 0.5 * settings.YSize;
            var _centre = Extents.Centre - new MarkGeometryPoint(0.5 * (nColumns * settings.XSize), 0.5 * (nRows * settings.YSize));

            for (int row = 0; row < nRows; row++)
            {
                for (int col = 0; col < nColumns; col++)
                {
                    var centrePoint = new MarkGeometryPoint(
                        (col * settings.XSize) + _halfTileWidth,
                        (row * settings.YSize) + _halfTileHeight
                    );

                    GeometricArithmeticModule.Translate(centrePoint, _centre.X + settings.XOffset, _centre.Y + settings.YOffset);

                    Tiles.Add(new MarkGeometryRectangle(centrePoint, settings.XSize, settings.YSize));
                }
            }

            return true;
        }

        public bool SaveAsDXF(string outputFilePath)
        {
            var document = new DxfDocument(new HeaderVariables());

            // combine contour and hatches
            foreach (var geometry in HatchLines.Concat(ContourLines))
                document.AddEntity(
                    geometry.GetAsDXFEntity()
                );


            document.Save(outputFilePath);
            return File.Exists(outputFilePath);
        }

        /// <summary>
        ///     Saves the contours and hatches in layers corresponding to its tile
        /// </summary>
        /// <param name="outputDirectory"></param>
        /// <param name="outputName"></param>
        /// <returns></returns>
        public bool ExportAsDXF(string outputDirectory, string outputName)
        {
            var outputFilePath = Path.Combine(outputDirectory, outputName);
            var document = new DxfDocument(new HeaderVariables());

            // combine geometries
            var geometries = HatchLines.Concat(ContourLines).ToArray();

            // clip geometries in tiles and results to dxf layer
            for (int i = 0; i < Tiles.Count; i++)
                for (int j = 0; j < geometries.Length; j++)
                {
                    var results = GeometricArithmeticModule.ClipGeometry(
                        geometries[j],
                        Tiles[i]
                    );

                    for (int k = 0; k < results?.Count; k++)
                        document.AddEntity(
                            results[k].GetAsDXFEntity(
                                $"Tile {i}"
                            )
                        );
                }

            document.Save(outputFilePath);
            return File.Exists(outputFilePath);
        }

        public void BeginGetTiles(Action<(int Id, MarkGeometryRectangle TileBoundary, List<IMarkGeometry> Pattern)> callbackIn)
        {
            // combine geometries
            var geometries = HatchLines.Concat(ContourLines).ToArray();

            // clip geometries in tiles and results to dxf layer
            for (int i = 0; i < Tiles.Count; i++)
            {
                var data = new List<IMarkGeometry>();

                for (int j = 0; j < geometries.Length; j++)
                {
                    var results = GeometricArithmeticModule.ClipGeometry(
                        geometries[j],
                        Tiles[i]
                    );

                    if (results != null)
                        data.AddRange(results);
                }

                if (data.Count > 0)
                    callbackIn((i, Tiles[i], data));
            }
        }
    }
}
