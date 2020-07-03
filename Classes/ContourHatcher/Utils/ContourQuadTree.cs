using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLSlicer.Utils;
using SharpGLShader.Utils;
using MSolvLib.MarkGeometry;
using MSolvLib.MarkGeometry.Helpers;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;

namespace ContourHatcher.Utils
{
    public class ContourQuadTree
    {
        public ContourQuadTree NorthWest { get; set; }
        public ContourQuadTree NorthEast { get; set; }
        public ContourQuadTree SouthWest { get; set; }
        public ContourQuadTree SouthEast { get; set; }

        public List<MarkGeometryLine> Segments { get; set; }

        public double Size { get; private set; }
        public double SubSize { get; private set; }
        public double MinSize { get; private set; } = 0.1;

        public bool ChildrenExists { get; private set; }
        public MarkGeometryRectangle Boundary { get; private set; }

        private ContourQuadTree(double centreX, double centreY, double size)
        {
            Create(centreX, centreY, size);
        }

        public ContourQuadTree(IEnumerable<MVertex> vertices)
        {
            Create(
                GeometricArithmeticModule.ToLines(
                    vertices.Select(v => new MarkGeometryPoint(v.X, v.Y)).ToArray()
                )
            );
        }

        public ContourQuadTree(IList<MarkGeometryLine> lines)
        {
            Create(lines);
        }

        private void Create(IList<MarkGeometryLine> lines)
        {
            var extents = GeometricArithmeticModule.CalculateExtents(lines);
            Create(extents.Centre.X, extents.Centre.Y, extents.Hypotenuse);

            foreach (var line in lines)
                if (!Insert(line))
                    throw new Exception($"Error while attempting to insert line: {line}");
        }

        private void Create(double centreX, double centreY, double size)
        {
            Size = size;
            SubSize = Size / 2;
            ChildrenExists = false;

            Segments = new List<MarkGeometryLine>();

            Boundary = new MarkGeometryRectangle(
                new MarkGeometryPoint(centreX, centreY),
                Size,
                Size
            );
        }

        public bool Insert(MarkGeometryLine line)
        {
            if (
                !(
                    GeometricArithmeticModule.IsWithin2D(line.StartPoint, Boundary.Extents) &&
                    GeometricArithmeticModule.IsWithin2D(line.EndPoint, Boundary.Extents)
                )
            )
                return false;

            // ensure quads exist
            if (!ChildrenExists)
            {
                var radius = 0.5 * SubSize;

                NorthWest = new ContourQuadTree(
                    Boundary.Extents.MinX + radius, // west
                    Boundary.Extents.MaxY - radius, // north
                    SubSize
                );
                NorthEast = new ContourQuadTree(
                    Boundary.Extents.MaxX - radius, // east
                    Boundary.Extents.MaxY - radius, // north
                    SubSize
                );
                SouthWest = new ContourQuadTree(
                    Boundary.Extents.MinX + radius, // west
                    Boundary.Extents.MinY + radius, // south
                    SubSize
                );
                SouthEast = new ContourQuadTree(
                    Boundary.Extents.MaxX - radius, // east
                    Boundary.Extents.MinY + radius, // south
                    SubSize
                );

                ChildrenExists = true;
            }

            if (
                (line.Length <= MinSize) || 
                !(
                    NorthWest.Insert(line) ||
                    NorthEast.Insert(line) ||
                    SouthWest.Insert(line) ||
                    SouthEast.Insert(line)
                )
            )
                Segments.Add(line);

            return true;
        }

        public IntersectionsBinaryTree Intersect(MarkGeometryLine line)
        {
            return Intersect(line, new LineEquation(line));
        }

        private IntersectionsBinaryTree Intersect(MarkGeometryLine line, LineEquation equation)
        {
            if (!equation.PassesThroughRect(Boundary))
                return null;

            // using binary tree to sort points relative to the line's starting point
            var intersections = new IntersectionsBinaryTree(line.StartPoint);

            if (ChildrenExists)
            {
                IntersectionsBinaryTree childIntersections;

                if ((childIntersections = NorthWest.Intersect(line)) != null)
                    intersections.InsertRange(childIntersections);
                if ((childIntersections = NorthEast.Intersect(line)) != null)
                    intersections.InsertRange(childIntersections);
                if ((childIntersections = SouthWest.Intersect(line)) != null)
                    intersections.InsertRange(childIntersections);
                if ((childIntersections = SouthEast.Intersect(line)) != null)
                    intersections.InsertRange(childIntersections);
            }

            MarkGeometryPoint intersection;
            for (int i = 0; i < Segments.Count; i++)
                if ((
                    intersection = GeometricArithmeticModule.CalculateIntersection2D(
                        line,
                        Segments[i]
                    )) != null
                )
                    intersections.Insert(intersection);

            return intersections;
        }

        private void Draw(Bitmap bitmap, GeometryShader2D shader, Matrix4x4 transform)
        {
            var geometries = new List<IMarkGeometry>();

            var border = (MarkGeometryRectangle)Boundary.Clone();
            border.Stroke = Color.White;

            border.Transform(transform);
            geometries.Add(border);

            // apply transformation to geometries
            foreach (var line in Segments)
            {
                var geometry = ((IMarkGeometry)line.Clone());
                geometry.Transform(transform);
                geometries.Add(geometry);
            }

            // draw used cells in red
            shader.Draw(
                bitmap,
                geometries
            );

            if (ChildrenExists)
            {
                NorthWest.Draw(bitmap, shader, transform);
                NorthEast.Draw(bitmap, shader, transform);
                SouthWest.Draw(bitmap, shader, transform);
                SouthEast.Draw(bitmap, shader, transform);
            }
        }

        public bool SaveImage(string filePath)
        {
            var bitmap = new Bitmap(
                480, 480,
                PixelFormat.Format24bppRgb
            );

            var shader = new GeometryShader2D();
            shader.UpdateSettings(1, 1, Color.Red, Color.Green, Color.Transparent);

            shader.Reset(bitmap, Color.Black);

            var xScale = bitmap.Width / Boundary.Extents.Width;
            var yScale = bitmap.Height / Boundary.Extents.Height;

            // calculate transform
            var transformationMatrix = GeometricArithmeticModule.CombineTransformations(
                // centre geometries at origin before scaling
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    -Boundary.Extents.Centre.X,
                    -Boundary.Extents.Centre.Y
                ),
                // scale geometries to fit the target bitmap
                GeometricArithmeticModule.GetScalingTransformationMatrix(
                    xScale,
                    -yScale
                ),
                // centre geometries in target bitmap
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    0.5 * bitmap.Width,
                    0.5 * bitmap.Height
                )
            );

            Draw(bitmap, shader, transformationMatrix);
            return shader.WriteToFile(bitmap, filePath, PixelFormat.Format24bppRgb, 120, 120, GeometryShader2D.OptimisationSetting.Default);
        }
    }
}
