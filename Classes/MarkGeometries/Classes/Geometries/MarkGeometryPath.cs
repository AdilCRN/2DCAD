using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace MSolvLib.MarkGeometry
{
    public class MarkGeometryPath : MarkGeometry, IXmlSerializable
    {
        public static double ClosureTolerance { get; set; } = 0.0001;

        public override string Name => "Path";
        public MarkGeometryPoint CentrePoint { get; set; } = new MarkGeometryPoint(); 
        public List<MarkGeometryPoint> Points { get; set; } = new List<MarkGeometryPoint>();
        public bool IsClosed { get; set; } = false;
        public override double Area { get; protected set; } = 0;
        public override double Perimeter { get; protected set; } = 0;

        private MarkGeometryPoint __startPoint = null;
        public MarkGeometryPoint StartPoint
        {
            get
            {
                if (__startPoint != null)
                    return __startPoint;

                if (Points.Count <= 0)
                    return null;

                return Points[0];
            }

            protected set
            {
                __startPoint = value;
            }
        }

        private MarkGeometryPoint __endPoint = null;
        public MarkGeometryPoint EndPoint
        {
            get
            {
                if (__endPoint != null)
                    return __endPoint;

                if (Points.Count <= 0)
                    return null;

                return Points[Points.Count - 1];
            }

            protected set
            {
                __endPoint = value;
            }
        }

        public MarkGeometryPath()
            : base()
        {
            Update();
        }

        /// <summary>
        ///     The copy constructor.
        /// </summary>
        /// <param name="input"></param>
        protected MarkGeometryPath(MarkGeometryPath input)
            : base(input)
        {
            IsClosed = !!input.IsClosed;
            Points = input.Points.ConvertAll(point => (MarkGeometryPoint)point.Clone());
            CentrePoint = (MarkGeometryPoint) input.CentrePoint.Clone();

            Update();
        }

        public MarkGeometryPath(MarkGeometryLine line)
            : base()
        {
            Points.Add(line.StartPoint);
            Points.Add(line.EndPoint);
            CentrePoint = line.GetMidpoint();

            IsClosed = false;
            Update();
        }

        //public MarkGeometryPath(LwPolyline lwPolyline)
        //    : base()
        //{
        //    foreach (var entity in lwPolyline.Explode())
        //    {
        //        if (entity is Line lineEntity)
        //        {
        //            Points.Add(new MarkGeometryPoint(lineEntity.StartPoint));
        //            Points.Add(new MarkGeometryPoint(lineEntity.EndPoint));
        //        }
        //        else if (entity is Arc arcEntity)
        //        {
        //            arcEntity.PolygonalVertexes
        //        }
        //    }
        //    Points.AddRange(
        //        lwPolyline.Vertexes.ConvertAll(v => new MarkGeometryPoint(v))
        //    );

        //    if (
        //        lwPolyline.IsClosed &&
        //        GeometricArithmeticModule.Compare(StartPoint, EndPoint, ClosureTolerance) != 0)
        //    {
        //        Points.Add((MarkGeometryPoint)StartPoint.Clone());
        //    }

        //    // TODO : Calculate centroid
        //    CentrePoint = new MarkGeometryPoint();
        //    Update();
        //}

        public MarkGeometryPath(Polyline polyline)
            : base()
        {
            Points.AddRange(
                polyline.Vertexes.Select(v => new MarkGeometryPoint(v.Position))
            );

            if (
                polyline.IsClosed &&
                GeometricArithmeticModule.Compare(StartPoint, EndPoint, ClosureTolerance) != 0)
            {
                Points.Add((MarkGeometryPoint)StartPoint.Clone());
            }

            // TODO : Calculate centroid
            CentrePoint = new MarkGeometryPoint();
            Update();
        }

        public MarkGeometryPath(params MarkGeometryLine[] lines)
            : base()
        {
            if (lines != null && lines.Length > 0)
            {
                Points.Add(lines[0].StartPoint);
                for (int i=0; i<lines.Length; i++)
                {
                    Points.Add(lines[i].EndPoint);
                }
            }

            // TODO : Calculate centroid
            CentrePoint = new MarkGeometryPoint();
            Update();
        }

        public MarkGeometryPath(IEnumerable<MarkGeometryLine> lines)
            : this(lines.ToArray())
        {
        }

        public MarkGeometryPath(params MarkGeometryPoint[] points)
            : base()
        {
            Points.AddRange(points);

            // TODO : Calculate centroid
            CentrePoint = new MarkGeometryPoint();
            Update();
        }

        public MarkGeometryPath(IEnumerable<MarkGeometryPoint> points)
            : base()
        {
            Points.AddRange(points);

            // TODO : Calculate centroid
            CentrePoint = new MarkGeometryPoint();
            Update();
        }


        public MarkGeometryPath(MarkGeometryArc arc)
            : base()
        {
            Points.AddRange((MarkGeometryPoint[])arc);
            CentrePoint = arc.CentrePoint;
            Fill = arc.Fill;
            Stroke = arc.Stroke;

            Update();
        }

        public MarkGeometryPath(MarkGeometryArc arc, double minimumFacetLength)
            : base()
        {
            int nSegments = (int) Math.Floor(GeometricArithmeticModule.CalculatePerimeter(arc) / minimumFacetLength);
            Points.AddRange((MarkGeometryPoint[])arc);
            CentrePoint = arc.CentrePoint;
            Fill = arc.Fill;
            Stroke = arc.Stroke;

            Update();
        }

        public MarkGeometryPath(MarkGeometryCircle circle)
            : base()
        {
            Points.AddRange((MarkGeometryPoint[])circle);
            CentrePoint = circle.CentrePoint;
            Fill = circle.Fill;
            Stroke = circle.Stroke;

            Update();
        }

        public MarkGeometryPath(MarkGeometryCircle circle, double minimumFacetLength)
            : base()
        {
            int nSegments = (int)Math.Floor(GeometricArithmeticModule.CalculatePerimeter(circle) / minimumFacetLength);
            Points.AddRange(GeometricArithmeticModule.Explode(circle, nSegments + 1));
            CentrePoint = circle.CentrePoint;

            Update();
        }

        public static explicit operator MarkGeometryPoint[](MarkGeometryPath path)
        {
            return path.Points.ToArray();
        }

        public static explicit operator List<MarkGeometryPoint>(MarkGeometryPath path)
        {
            return path.Points;
        }

        public static explicit operator List<PointF>(MarkGeometryPath path)
        {
            return path.Points.ConvertAll(p => (PointF)p);
        }

        public void Merge(MarkGeometryPath path)
        {

            if (
                GeometricArithmeticModule.Compare(EndPoint, path.StartPoint, ClosureTolerance) == 0
            )
            {
                // skip the path's start point if its the
                // same as this path's end point
                Points.AddRange(path.Points.Skip(1));
            }
            else
            {
                Points.AddRange(path.Points);
            }

            
            Update();
        }

        public void Add(MarkGeometryPoint point, bool deferUpdate = false)
        {
            if (
                Points.Count > 0 &&
                GeometricArithmeticModule.Compare(EndPoint, point, ClosureTolerance) == 0
            )
                return; // ignore existing points

            Points.Add(point);

            if (!deferUpdate)
                Update();
        }

        /// <summary>
        /// Add point to the begininng of a path.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="deferUpdate"></param>
        public void Prepend(MarkGeometryPoint point, bool deferUpdate = false)
        {
            if (
                Points.Count > 0 &&
                GeometricArithmeticModule.Compare(StartPoint, point, ClosureTolerance) == 0
            )
                return; // ignore existing points

            Points.Insert(0, point);

            if (!deferUpdate)
                Update();
        }

        /// <summary>
        /// Add points to the begininng of a path.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="deferUpdate"></param>
        public void Prepend(IEnumerable<MarkGeometryPoint> points, bool deferUpdate = false)
        {

            foreach (var point in points.Reverse())
            {
                Prepend(point, true);
            }

            if (!deferUpdate)
                Update();
        }

        /// <summary>
        /// Add point to the end of the path
        /// </summary>
        /// <param name="point"></param>
        /// <param name="deferUpdate"></param>
        public void Append(MarkGeometryPoint point, bool deferUpdate = false)
        {
            if (
                Points.Count > 0 &&
                GeometricArithmeticModule.Compare(EndPoint, point, ClosureTolerance) == 0
            )
                return; // ignore existing points

            Points.Add(point);

            if (!deferUpdate)
                Update();
        }

        /// <summary>
        /// Add points to the end of the path
        /// </summary>
        /// <param name="points"></param>
        /// <param name="deferUpdate"></param>
        public void AppendRange(IEnumerable<MarkGeometryPoint> points, bool deferUpdate = false)
        {
            foreach (var point in points)
                Append(point, true);

            if (!deferUpdate)
                Update();
        }

        public override void SetFill(Color? colorIn)
        {
            Parallel.ForEach(Points, (point) =>
            {
                point.SetFill(colorIn);
            });

            base.SetFill(colorIn);
        }

        public override void SetStroke(Color? colorIn)
        {
            Parallel.ForEach(Points, (point) =>
            {
                point.SetStroke(colorIn);
            });

            base.SetStroke(colorIn);
        }

        public override object Clone()
        {
            return new MarkGeometryPath(this);
        }

        public override void Update()
        {
            // you need at least four points to
            // make a filled path
            IsClosed = (Points.Count >= 4) &&
                GeometricArithmeticModule.Compare(
                    StartPoint,
                    EndPoint,
                    ClosureTolerance
                ) == 0;

            // Perimeter is calculated within SetExtents
            SetExtents();

            if (Points.Count < 2)
            {
                Area = 0; // same as point
                return;
            }
            else if (Points.Count < 4)
            {
                Area = Extents.Hypotenuse * Double.Epsilon; // same as line
                return;
            }

            // calculating area of an irregular polygon
            double A = 0;
            double B = 0;

            for(var i=0; i< Points.Count - 1; i++)
            {
                A += Points[i].X * Points[i+1].Y;
                B += Points[i].Y * Points[i+1].X;
            }

            if (!IsClosed)
            {
                A += EndPoint.X * StartPoint.Y;
                B += EndPoint.Y * StartPoint.X;
            }

            Area = Math.Abs((A - B) / 2.0);
        }

        public override void SetExtents()
        {
            Perimeter = 0;

            if (Points.Count > 0)
            {
                Extents.MinX = Double.MaxValue;
                Extents.MaxX = Double.MinValue;

                Extents.MinY = Double.MaxValue;
                Extents.MaxY = Double.MinValue;

                Extents.MinZ = Double.MaxValue;
                Extents.MaxZ = Double.MinValue;

                for (int i = 0; i < Points.Count; i++)
                {
                    Extents.MinX = GeometricArithmeticModule.Min(Extents.MinX, Points[i].X);
                    Extents.MaxX = GeometricArithmeticModule.Max(Extents.MaxX, Points[i].X);

                    Extents.MinY = GeometricArithmeticModule.Min(Extents.MinY, Points[i].Y);
                    Extents.MaxY = GeometricArithmeticModule.Max(Extents.MaxY, Points[i].Y);

                    Extents.MinZ = GeometricArithmeticModule.Min(Extents.MinZ, Points[i].Z);
                    Extents.MaxZ = GeometricArithmeticModule.Max(Extents.MaxZ, Points[i].Z);

                    if (i < Points.Count-1)
                    {
                        Perimeter += GeometricArithmeticModule.ABSMeasure(Points[i], Points[i + 1]);
                    }
                }
            }
            else
            {
                Extents.MinX = 0;
                Extents.MaxX = 0;

                Extents.MinY = 0;
                Extents.MaxY = 0;

                Extents.MinZ = 0;
                Extents.MaxZ = 0;
            }
        }

        public override void Transform(Matrix4x4 transformationMatrixIn)
        {
            Parallel.For(0, Points.Count, (i) =>
            {
                Points[i].Transform(transformationMatrixIn);
            });

            //foreach (var point in Points)
            //    point.Transform(transformationMatrixIn);

            SetExtents();

            // TODO : Compute Centroid
            //CentrePoint.Transform(transformationMatrixIn);
            // TODO : Doesn't need to run update as it's properties (e.g. Area and Perimeter) hasn't changed
        }

        public override void Draw2D(IMarkGeometryVisualizer2D view, bool shouldShowVertex)
        {
            view.Draw2D(
                GeometricArithmeticModule.ToLines(Points), 
                shouldShowVertex
            );
        }

        public override void ReadXml(XmlReader reader)
        {
            reader.Read(); // Skip ahead to next node
            base.ReadXml(reader);

            IsClosed = bool.Parse(reader.GetAttribute(nameof(IsClosed)));

            CentrePoint = new MarkGeometryPoint();
            CentrePoint.ReadXml(reader);

            Points = new List<MarkGeometryPoint>();

            if (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == nameof(Points))
            {
                while(reader.MoveToContent() == XmlNodeType.Element)
                {
                    var point = new MarkGeometryPoint();
                    point.ReadXml(reader);
                    Points.Add(point);
                }
            }

            Update();
        }

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(GetType().ToString());
            base.WriteXml(writer);

            writer.WriteAttributeString(nameof(IsClosed), IsClosed.ToString());
            CentrePoint.WriteXml(writer);

            writer.WriteStartElement(nameof(Points));
            foreach(var point in Points)
            {
                point.WriteXml(writer);
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        public override EntityObject GetAsDXFEntity()
        {
            List<netDxf.Vector2> entityPoints = new List<netDxf.Vector2>();

            for(int i=0; i<Points.Count; i++)
            {
                entityPoints.Add((netDxf.Vector2)Points[i]);
            }

            return new LwPolyline(entityPoints);
        }

        public override EntityObject GetAsDXFEntity(string layerName)
        {
            List<netDxf.Vector2> entityPoints = new List<netDxf.Vector2>();

            for (int i = 0; i < Points.Count; i++)
            {
                entityPoints.Add((netDxf.Vector2)Points[i]);
            }

            return new LwPolyline(entityPoints)
            {
                Layer = new netDxf.Tables.Layer(layerName)
            };
        }
    }
}
