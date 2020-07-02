using MathNet.Numerics.LinearAlgebra;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
        public List<MarkGeometryLine> Lines { get; set; } = new List<MarkGeometryLine>();
        public bool IsClosed { get; set; } = false;
        public override double Area { get; protected set; } = 0;
        public override double Perimeter { get; protected set; } = 0;

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
            Lines = input.Lines.ConvertAll(line => (MarkGeometryLine)line.Clone());
            CentrePoint = (MarkGeometryPoint) input.CentrePoint.Clone();

            Update();
        }

        public MarkGeometryPath(MarkGeometryLine line)
            : base()
        {
            Lines.Add(line);
            CentrePoint = line.GetMidpoint();

            IsClosed = false;
            Update();
        }

        public MarkGeometryPath(LwPolyline lwPolyline)
            : base()
        {
            for (int i = 0; i < lwPolyline.Vertexes.Count()-1; i++)
            {
                Lines.Add(
                    new MarkGeometryLine(
                        new MarkGeometryPoint(lwPolyline.Vertexes[i]),
                        new MarkGeometryPoint(lwPolyline.Vertexes[i + 1])
                    )
                );
            }

            if (lwPolyline.IsClosed && lwPolyline.Vertexes.Count() > 1)
            {
                Lines.Add(
                    new MarkGeometryLine(
                        new MarkGeometryPoint(lwPolyline.Vertexes[lwPolyline.Vertexes.Count() - 1]),
                        new MarkGeometryPoint(lwPolyline.Vertexes[0])
                    )
                );
            }

            //Color = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, lwPolyline.Color.R, lwPolyline.Color.G, lwPolyline.Color.B));
            //Transparency = lwPolyline.Transparency.Value;

            // TODO : Calculate centroid
            CentrePoint = new MarkGeometryPoint();
            Update();
        }

        public MarkGeometryPath(Polyline polyline)
            : base()
        {
            for (int i = 0; i < polyline.Vertexes.Count()-1; i++)
            {
                Lines.Add(
                    new MarkGeometryLine(
                        new MarkGeometryPoint(polyline.Vertexes[i].Position),
                        new MarkGeometryPoint(polyline.Vertexes[i + 1].Position)
                    )
                );
            }

            if (polyline.IsClosed && polyline.Vertexes.Count() > 1)
            {
                Lines.Add(
                    new MarkGeometryLine(
                        new MarkGeometryPoint(polyline.Vertexes[polyline.Vertexes.Count() - 1].Position),
                        new MarkGeometryPoint(polyline.Vertexes[0].Position)
                    )
                );
            }

            //Color = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, polyline.Color.R, polyline.Color.G, polyline.Color.B));
            //Transparency = polyline.Transparency.Value;

            // TODO : Calculate centroid
            CentrePoint = new MarkGeometryPoint();
            Update();
        }

        public MarkGeometryPath(params MarkGeometryLine[] lines)
            : base()
        {
            Lines = new List<MarkGeometryLine>(lines);

            // TODO : Calculate centroid
            CentrePoint = new MarkGeometryPoint();
            Update();
        }

        public MarkGeometryPath(params MarkGeometryPoint[] points)
            : base()
        {
            FromPoints(points);
        }


        public MarkGeometryPath(MarkGeometryArc arc)
            : base()
        {
            Lines = new List<MarkGeometryLine>((MarkGeometryLine[])arc);
            CentrePoint = arc.CentrePoint;
            Fill = arc.Fill;
            Stroke = arc.Stroke;

            Update();
        }

        public MarkGeometryPath(MarkGeometryArc arc, double minimumFacetLength)
            : base()
        {
            int nSegments = (int) Math.Floor(GeometricArithmeticModule.CalculatePerimeter(arc) / minimumFacetLength);
            Lines = GeometricArithmeticModule.SplitGeometry(arc, nSegments);
            CentrePoint = arc.CentrePoint;

            Update();
        }

        public MarkGeometryPath(MarkGeometryCircle circle)
            : base()
        {
            Lines = GeometricArithmeticModule.SplitGeometry(circle, circle.VertexCount);
            CentrePoint = circle.CentrePoint;
            Fill = circle.Fill;
            Stroke = circle.Stroke;

            Update();
        }

        public MarkGeometryPath(MarkGeometryCircle circle, double minimumFacetLength)
            : base()
        {
            int nSegments = (int)Math.Floor(GeometricArithmeticModule.CalculatePerimeter(circle) / minimumFacetLength);
            Lines = GeometricArithmeticModule.SplitGeometry(circle, nSegments);
            CentrePoint = circle.CentrePoint;

            Update();
        }

        public static explicit operator MarkGeometryPoint[](MarkGeometryPath path)
        {
            List<MarkGeometryPoint> points = new List<MarkGeometryPoint>();

            if (path.Lines.Count > 0)
            {
                points.Add(path.Lines[0].StartPoint);

                foreach (var line in path.Lines)
                {
                    points.Add(line.EndPoint);
                }
            }

            return points.ToArray();
        }

        public static explicit operator List<MarkGeometryPoint>(MarkGeometryPath path)
        {
            List<MarkGeometryPoint> points = new List<MarkGeometryPoint>();

            if (path.Lines.Count > 0)
            {
                points.Add(path.Lines[0].StartPoint);

                foreach (var line in path.Lines)
                {
                    points.Add(line.EndPoint);
                }
            }

            return points;
        }

        public static explicit operator List<PointF>(MarkGeometryPath path)
        {
            var points = new List<PointF>();

            if (path.Lines.Count > 0)
            {
                points.Add(path.Lines[0].StartPoint);

                foreach (var line in path.Lines)
                {
                    points.Add(line.EndPoint);
                }
            }

            return points;
        }

        public void Merge(MarkGeometryPath path)
        {
            if (Lines.Count() <= 0)
            {
                Lines = path.Lines;
                CentrePoint = path.CentrePoint;
                
            }
            else if (Lines.Count() > 0 && path.Lines.Count() > 0)
            {
                MarkGeometryPoint currentEndpoint = Lines[Lines.Count() - 1].EndPoint;
                MarkGeometryPoint nextStartEndpoint = path.Lines[0].StartPoint;

                if (GeometricArithmeticModule.Compare(currentEndpoint, nextStartEndpoint) != 0)
                {
                    Lines.Add(new MarkGeometryLine(currentEndpoint, nextStartEndpoint));
                }
            }

            Lines.AddRange(path.Lines);
            Update();
        }

        public override void SetFill(Color? colorIn)
        {
            Parallel.ForEach(Lines, (line) =>
            {
                line.SetFill(colorIn);
            });

            base.SetFill(colorIn);
        }

        public override void SetStroke(Color? colorIn)
        {
            Parallel.ForEach(Lines, (line) =>
            {
                line.SetStroke(colorIn);
            });

            base.SetStroke(colorIn);
        }

        public override object Clone()
        {
            return new MarkGeometryPath(this);
        }

        public override void Update()
        {
            IsClosed = (Lines.Count > 2) &&
                GeometricArithmeticModule.Compare(
                    Lines.First().StartPoint,
                    Lines.Last().EndPoint,
                    ClosureTolerance
                ) == 0;

            // Perimeter is calculated within SetExtents
            SetExtents();

            if (Lines.Count <= 0)
            {
                Area = 0;
                return;
            }
            else if (Lines.Count <= 1)
            {
                Area = Lines[0].Area;
                return;
            }

            // calculating area of an irregular polygon
            double A = 0;
            double B = 0;

            var __points = (List<MarkGeometryPoint>)this;

            if (!IsClosed)
            {
                __points.Add(__points[0]);
            }

            for(var i=0; i< __points.Count - 1; i++)
            {
                var currPoint = __points[i];
                var nxtPoint = __points[i + 1];

                A += currPoint.X * nxtPoint.Y;
                B += currPoint.Y * nxtPoint.X;
            }

            Area = Math.Abs((A - B) / 2.0);
        }

        public override void SetExtents()
        {
            Perimeter = 0;

            if (Lines.Count > 0)
            {
                Extents.MinX = Double.MaxValue;
                Extents.MaxX = Double.MinValue;

                Extents.MinY = Double.MaxValue;
                Extents.MaxY = Double.MinValue;

                Extents.MinZ = Double.MaxValue;
                Extents.MaxZ = Double.MinValue;

                foreach (MarkGeometryLine line in Lines)
                {
                    Extents.MinX = GeometricArithmeticModule.Min(Extents.MinX, line.Extents.MinX);
                    Extents.MaxX = GeometricArithmeticModule.Max(Extents.MaxX, line.Extents.MaxX);

                    Extents.MinY = GeometricArithmeticModule.Min(Extents.MinY, line.Extents.MinY);
                    Extents.MaxY = GeometricArithmeticModule.Max(Extents.MaxY, line.Extents.MaxY);

                    Extents.MinZ = GeometricArithmeticModule.Min(Extents.MinZ, line.Extents.MinZ);
                    Extents.MaxZ = GeometricArithmeticModule.Max(Extents.MaxZ, line.Extents.MaxZ);

                    Perimeter += line.Length;
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

        public void FromPoints(params MarkGeometryPoint[] points)
        {
            Lines = new List<MarkGeometryLine>();

            Lines.AddRange(GeometricArithmeticModule.ToLines(points));

            // TODO : Calculate centroid
            CentrePoint = new MarkGeometryPoint();
            Update();
        }

        public override void Transform(Matrix<double> transformationMatrixIn)
        {
            foreach (var line in Lines)
            {
                line.Transform(transformationMatrixIn);
            }

            CentrePoint.Transform(transformationMatrixIn);

            Update();
        }

        public override void Draw2D(IMarkGeometryVisualizer2D view, bool shouldShowVertex)
        {
            view.Draw2D(Lines, shouldShowVertex);
        }

        public override void ReadXml(XmlReader reader)
        {
            reader.Read(); // Skip ahead to next node
            base.ReadXml(reader);

            IsClosed = bool.Parse(reader.GetAttribute(nameof(IsClosed)));

            CentrePoint = new MarkGeometryPoint();
            CentrePoint.ReadXml(reader);

            Lines = new List<MarkGeometryLine>();

            if (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == nameof(Lines))
            {
                while(reader.MoveToContent() == XmlNodeType.Element)
                {
                    var line = new MarkGeometryLine();
                    line.ReadXml(reader);
                    Lines.Add(line);
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

            writer.WriteStartElement(nameof(Lines));
            foreach(var line in Lines)
            {
                line.WriteXml(writer);
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        public override EntityObject GetAsDXFEntity()
        {
            List<netDxf.Vector2> entityPoints = new List<netDxf.Vector2>();

            if (Lines.Count > 0)
            {
                entityPoints.Add((netDxf.Vector2)Lines[0].StartPoint);

                foreach (var line in Lines)
                {
                    entityPoints.Add((netDxf.Vector2)line.EndPoint);
                }
            }

            return new LwPolyline(entityPoints);
        }

        public override EntityObject GetAsDXFEntity(string layerName)
        {
            List<netDxf.Vector2> entityPoints = new List<netDxf.Vector2>();

            if (Lines.Count > 0)
            {
                entityPoints.Add((netDxf.Vector2)Lines[0].StartPoint);

                foreach (var line in Lines)
                {
                    entityPoints.Add((netDxf.Vector2)line.EndPoint);
                }
            }

            return new LwPolyline(entityPoints)
            {
                Layer = new netDxf.Tables.Layer(layerName)
            };
        }
    }
}
