using netDxf;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace MSolvLib.MarkGeometry
{
    public class MarkGeometriesWrapper : MarkGeometry, IMarkGeometryWrapper
    {
        public double GridWidth { get; set; } = 0;
        public double GridHeight { get; set; } = 0;
        public double GridDepth { get; set; } = 0;
        public MarkGeometryPoint Origin { get; set; }

        public MarkGeometryPoint Offset { get; set; } = new MarkGeometryPoint();

        public List<MarkGeometryPoint> Points { get; set; } = new List<MarkGeometryPoint>();
        public List<MarkGeometryLine> Lines { get; set; } = new List<MarkGeometryLine>();
        public List<MarkGeometryArc> Arcs { get; set; } = new List<MarkGeometryArc>();
        public List<MarkGeometryCircle> Circles { get; set; } = new List<MarkGeometryCircle>();
        public List<MarkGeometryPath> Paths { get; set; } = new List<MarkGeometryPath>();

        public override double Area { get; protected set; } = 0;
        public override double Perimeter { get; protected set; } = 0;

        public MarkGeometriesWrapper()
        {
            Points = new List<MarkGeometryPoint>();
            Lines = new List<MarkGeometryLine>();
            Arcs = new List<MarkGeometryArc>();
            Circles = new List<MarkGeometryCircle>();
            Paths = new List<MarkGeometryPath>();
            Origin = new MarkGeometryPoint();

            Update();
        }

        /// <summary>
        ///     The copy constructor
        /// </summary>
        /// <param name="input">Reference to object to copy</param>
        internal MarkGeometriesWrapper(MarkGeometriesWrapper input)
        {
            Points = input.Points.ConvertAll(x => (MarkGeometryPoint)x.Clone());
            Lines = input.Lines.ConvertAll(x => (MarkGeometryLine)x.Clone());
            Arcs = input.Arcs.ConvertAll(x => (MarkGeometryArc)x.Clone());
            Circles = input.Circles.ConvertAll(x => (MarkGeometryCircle)x.Clone());
            Paths = input.Paths.ConvertAll(x => (MarkGeometryPath)x.Clone());
            Origin = (MarkGeometryPoint) input.Origin.Clone();

            Update();
        }

        public MarkGeometriesWrapper(IMarkGeometry[] geometries)
        {
            foreach(var geometry in geometries)
            {
                if (geometry is MarkGeometryPoint point)
                {
                    Points.Add(point);
                }
                else if (geometry is MarkGeometryLine line)
                {
                    Lines.Add(line);
                }
                else if (geometry is MarkGeometryPath path)
                {
                    Paths.Add(path);
                }
                else if (geometry is MarkGeometryArc arc)
                {
                    Arcs.Add(arc);
                }
                else if (geometry is MarkGeometryCircle circle)
                {
                    Circles.Add(circle);
                }
            }

            Update();
        }

        public static explicit operator MarkGeometryPoint[](MarkGeometriesWrapper wrapper)
        {
            List<MarkGeometryPoint> points = new List<MarkGeometryPoint>();

            foreach (var geometry in wrapper.Flatten())
            {
                if (geometry is MarkGeometryPoint point)
                {
                    points.Add(point);
                }
                else if (geometry is MarkGeometryLine line)
                {
                    points.AddRange((MarkGeometryPoint[]) line);
                }
                else if (geometry is MarkGeometryCircle circle)
                {
                    points.AddRange((MarkGeometryPoint[]) circle);
                }
                else if (geometry is MarkGeometryArc arc)
                {
                    points.AddRange((MarkGeometryPoint[]) arc);
                }
                else if (geometry is MarkGeometryPath path)
                {
                    points.AddRange((MarkGeometryPoint[]) path);
                }
            }

            return points.ToArray();
        }

        public List<IMarkGeometry> Flatten()
        {
            List<IMarkGeometry> geometries = new List<IMarkGeometry>();

            geometries.AddRange(Arcs);
            geometries.AddRange(Circles);
            geometries.AddRange(Lines);
            geometries.AddRange(Points);
            geometries.AddRange(Paths);

            return geometries;
        }

        public void AddRange(IEnumerable<MarkGeometryArc> arcs)
        {
            Arcs.AddRange(arcs);
            Update();
        }

        public void AddRange(IEnumerable<MarkGeometryPoint> points)
        {
            Points.AddRange(points);
            Update();
        }

        public void AddRange(IEnumerable<MarkGeometryLine> lines)
        {
            Lines.AddRange(lines);
            Update();
        }

        public void AddRange(IEnumerable<MarkGeometryCircle> circles)
        {
            Circles.AddRange(circles);
            Update();
        }

        public void AddRange(IEnumerable<MarkGeometryPath> paths)
        {
            Paths.AddRange(paths);
            Update();
        }

        public void Add(MarkGeometryArc arc)
        {
            Arcs.Add(arc);
            Update();
        }

        public void Add(MarkGeometryCircle circle)
        {
            Circles.Add(circle);
            Update();
        }

        public void Add(MarkGeometryPoint points)
        {
            Points.Add(points);
            Update();
        }

        public void Add(MarkGeometryLine lines)
        {
            Lines.Add(lines);
            Update();
        }

        public void Add(MarkGeometryPath path)
        {
            AddRange(GeometricArithmeticModule.ToLines(path.Points));

            // doesn't call Update(); because it is already called by the above method AddGeometry
            // change if this is no longer the case
        }

        /// <summary>
        ///     Applies function to all geometries
        /// </summary>
        /// <param name="function">The function apply to all geometries</param>
        public void MapFunc(Func<IMarkGeometry, IMarkGeometry> function)
        {
            Parallel.For(0, Points.Count(),
                index =>
                {
                    Points[index] = function(Points[index]) as MarkGeometryPoint;
                }
            );

            Parallel.For(0, Lines.Count(),
                index =>
                {
                    Lines[index] = function(Lines[index]) as MarkGeometryLine;
                }
            );

            Parallel.For(0, Arcs.Count(),
                index =>
                {
                    Arcs[index] = function(Arcs[index]) as MarkGeometryArc;
                }
            );

            Parallel.For(0, Circles.Count(),
                index =>
                {
                    Circles[index] = function(Circles[index]) as MarkGeometryCircle;
                }
            );

            Parallel.For(0, Paths.Count(),
                index =>
                {
                    Paths[index] = function(Paths[index]) as MarkGeometryPath;
                }
            );
        }

        public void BeginGetAll(Func<IMarkGeometry, bool> callback)
        {
            Parallel.For(0, Points.Count(),
                index =>
                {
                    callback(Points[index]);
                }
            );

            Parallel.For(0, Lines.Count(),
                index =>
                {
                    callback(Lines[index]);
                }
            );

            Parallel.For(0, Arcs.Count(),
                index =>
                {
                    callback(Arcs[index]);
                }
            );

            Parallel.For(0, Circles.Count(),
                index =>
                {
                    callback(Circles[index]);
                }
            );

            Parallel.For(0, Paths.Count(),
                index =>
                {
                    callback(Paths[index]);
                }
            );
        }

        public override void SetFill(Color? colorIn)
        {
            MapFunc((geometry) => {
                geometry.SetFill(colorIn);
                return geometry;
            });

            base.SetFill(colorIn);
        }

        public override void SetStroke(Color? colorIn)
        {
            MapFunc((geometry) => {
                geometry.SetStroke(colorIn);
                return geometry;
            });

            base.SetStroke(colorIn);
        }

        public override void Transform(Matrix4x4 transformationMatrixIn)
        {
            MapFunc((geometry) => 
            {
                geometry.Transform(transformationMatrixIn);
                return geometry;
            });

            Update();
        }

        /// <summary>
        ///     Create clone of this object
        /// </summary>
        /// <returns></returns>
        public override object Clone()
        {
            return new MarkGeometriesWrapper(this);
        }

        public void Clear()
        {
            Points.Clear();
            Lines.Clear();
            Arcs.Clear();
            Circles.Clear();
            Paths.Clear();
        }

        public bool SaveAsDXF(string filename)
        {
            if (filename == null || filename.Length <= 0)
            {
                return false;
            }

            string layerName;
            DxfDocument dxfDocument = new DxfDocument(new netDxf.Header.HeaderVariables());

            layerName = "points";
            foreach (MarkGeometryPoint point in Points)
            {
                dxfDocument.AddEntity(point.GetAsDXFEntity(layerName));
            }

            layerName = "arcs";
            foreach (MarkGeometryArc arc in Arcs)
            {
                dxfDocument.AddEntity(arc.GetAsDXFEntity(layerName));
            }

            layerName = "arcs";
            foreach (MarkGeometryCircle circle in Circles)
            {
                dxfDocument.AddEntity(circle.GetAsDXFEntity(layerName));
            }

            layerName = "lines";
            foreach (MarkGeometryLine line in Lines)
            {
                dxfDocument.AddEntity(line.GetAsDXFEntity(layerName));
            }

            layerName = "paths";
            foreach (MarkGeometryPath path in Paths)
            {
                foreach(MarkGeometryLine line in GeometricArithmeticModule.ToLines(path.Points))
                {
                    dxfDocument.AddEntity(line.GetAsDXFEntity(layerName));
                }
            }

            // TODO : add support for other geometric objects and shapes e.g. splines, ellipses, etc
            // Also could optimise the object heirarchy (i.e. saving entities in an efficient order)

            dxfDocument.Save(filename);
            return File.Exists(filename);
        }

        public override void SetExtents()
        {
            Extents.MinX = double.MaxValue;
            Extents.MaxX = double.MinValue;

            Extents.MinY = double.MaxValue;
            Extents.MaxY = double.MinValue;

            Extents.MinZ = double.MaxValue;
            Extents.MaxZ = double.MinValue;

            int[] ns = {
                Points.Count(),
                Arcs.Count(),
                Circles.Count(),
                Lines.Count(),
                Paths.Count()
            };

            int n = GeometricArithmeticModule.Max<int>(ns);

            for (int i=0; i<n; i++)
            {
                if (i < Points.Count())
                {
                    Extents.MaxX = Math.Max(Extents.MaxX, Points[i].Extents.MaxX);
                    Extents.MaxY = Math.Max(Extents.MaxY, Points[i].Extents.MaxY);
                    Extents.MaxZ = Math.Max(Extents.MaxZ, Points[i].Extents.MaxZ);

                    Extents.MinX = Math.Min(Extents.MinX, Points[i].Extents.MinX);
                    Extents.MinY = Math.Min(Extents.MinY, Points[i].Extents.MinY);
                    Extents.MinZ = Math.Min(Extents.MinZ, Points[i].Extents.MinZ);
                }
                
                if (i < Arcs.Count())
                {
                    Extents.MaxX = Math.Max(Extents.MaxX, Arcs[i].Extents.MaxX);
                    Extents.MaxY = Math.Max(Extents.MaxY, Arcs[i].Extents.MaxY);
                    Extents.MaxZ = Math.Max(Extents.MaxZ, Arcs[i].Extents.MaxZ);

                    Extents.MinX = Math.Min(Extents.MinX, Arcs[i].Extents.MinX);
                    Extents.MinY = Math.Min(Extents.MinY, Arcs[i].Extents.MinY);
                    Extents.MinZ = Math.Min(Extents.MinZ, Arcs[i].Extents.MinZ);
                }

                if (i < Circles.Count())
                {
                    Extents.MaxX = Math.Max(Extents.MaxX, Circles[i].Extents.MaxX);
                    Extents.MaxY = Math.Max(Extents.MaxY, Circles[i].Extents.MaxY);
                    Extents.MaxZ = Math.Max(Extents.MaxZ, Circles[i].Extents.MaxZ);

                    Extents.MinX = Math.Min(Extents.MinX, Circles[i].Extents.MinX);
                    Extents.MinY = Math.Min(Extents.MinY, Circles[i].Extents.MinY);
                    Extents.MinZ = Math.Min(Extents.MinZ, Circles[i].Extents.MinZ);
                }

                if (i < Lines.Count())
                {
                    Extents.MaxX = Math.Max(Extents.MaxX, Lines[i].Extents.MaxX);
                    Extents.MaxY = Math.Max(Extents.MaxY, Lines[i].Extents.MaxY);
                    Extents.MaxZ = Math.Max(Extents.MaxZ, Lines[i].Extents.MaxZ);

                    Extents.MinX = Math.Min(Extents.MinX, Lines[i].Extents.MinX);
                    Extents.MinY = Math.Min(Extents.MinY, Lines[i].Extents.MinY);
                    Extents.MinZ = Math.Min(Extents.MinZ, Lines[i].Extents.MinZ);
                }

                if (i < Paths.Count())
                {
                    Extents.MaxX = Math.Max(Extents.MaxX, Paths[i].Extents.MaxX);
                    Extents.MaxY = Math.Max(Extents.MaxY, Paths[i].Extents.MaxY);
                    Extents.MaxZ = Math.Max(Extents.MaxZ, Paths[i].Extents.MaxZ);

                    Extents.MinX = Math.Min(Extents.MinX, Paths[i].Extents.MinX);
                    Extents.MinY = Math.Min(Extents.MinY, Paths[i].Extents.MinY);
                    Extents.MinZ = Math.Min(Extents.MinZ, Paths[i].Extents.MinZ);
                }
            }

            GridWidth = Extents.MaxX - Extents.MinX;
            GridHeight = Extents.MaxY - Extents.MinY;
            GridDepth = Extents.MaxZ - Extents.MinZ;

            Origin = new MarkGeometryPoint(
                    Extents.MinX + (0.5 * GridWidth),
                    Extents.MinY + (0.5 * GridHeight),
                    Extents.MinZ + (0.5 * GridDepth)
                );
        }

        public override void Update()
        {
            SetExtents();
        }

        public override void Draw2D(IMarkGeometryVisualizer2D view, bool shouldShowVertex)
        {
            // TODO : please review/optimize this solution
            // Is it a good idea to use Parallels here?

            //// stash the current offsets
            //view.PushOffset();
            //view.AddOffset(Offset);

            int n = GeometricArithmeticModule.Max<int>(new int[] {
                Points.Count,
                Arcs.Count,
                Circles.Count,
                Lines.Count,
                Paths.Count
            });

            for (int i = 0; i < n; i++)
            {
                if (i < Points.Count)
                {
                    view.Draw2D(Points[i]);
                }

                if (i < Arcs.Count)
                {
                    view.Draw2D(Arcs[i], shouldShowVertex);
                }

                if (i < Circles.Count)
                {
                    view.Draw2D(Circles[i], shouldShowVertex);
                }

                if (i < Lines.Count)
                {
                    view.Draw2D(Lines[i], shouldShowVertex);
                }

                if (i < Paths.Count)
                {
                    view.Draw2D(Paths[i], shouldShowVertex);
                }
            }
            //// revert to previous offsets

            //view.PopOffset();
        }

        #region Section: Not Used
        //public void DrawBoundary2D(IMarkGeometryVisualizer2D view, double padding)
        //{
        //    // stash the current offsets
        //    view.PushOffset();
        //    view.AddOffset(Offset);

        //    MarkGeometryPoint topLeft = new MarkGeometryPoint(Extents.MinX - padding, Extents.MinY - padding);
        //    MarkGeometryPoint topRight = new MarkGeometryPoint(Extents.MaxX + padding, Extents.MinY - padding);
        //    MarkGeometryPoint bottomLeft = new MarkGeometryPoint(Extents.MinX - padding, Extents.MaxY + padding);
        //    MarkGeometryPoint bottomRight = new MarkGeometryPoint(Extents.MaxX + padding, Extents.MaxY + padding);

        //    view.Draw2D(new MarkGeometryLine(topLeft, topRight), false);
        //    view.Draw2D(new MarkGeometryLine(topRight, bottomRight), false);
        //    view.Draw2D(new MarkGeometryLine(bottomRight, bottomLeft), false);
        //    view.Draw2D(new MarkGeometryLine(bottomLeft, topLeft), false);

        //    // revert to previous offsets
        //    view.PopOffset();
        //} 
        #endregion

        public override EntityObject GetAsDXFEntity()
        {
            throw new NotImplementedException();
        }

        public override EntityObject GetAsDXFEntity(string layerName)
        {
            throw new NotImplementedException();
        }
    }
}
