using Emgu.CV.Structure;
using System;
using System.Numerics;
using System.Xml;
using System.Xml.Serialization;

namespace MSolvLib.MarkGeometry
{
    public class MarkGeometryLine : MarkGeometry, IXmlSerializable
    {
        public override string Name => "Line";
        public MarkGeometryPoint StartPoint { get; set; } = new MarkGeometryPoint();
        public MarkGeometryPoint EndPoint { get; set; } = new MarkGeometryPoint();
        public MarkGeometryPoint ReferencePoint { get; set; } = new MarkGeometryPoint();

        public override double Area { get; protected set; }
        public override double Perimeter { get; protected set; }

        private double length = 0;
        public double Length
        {
            get
            {
                return length;
            }
            set
            {
                length = value;
            }
        }

        private double angle = 0;
        public double Angle
        {
            get
            {
                return GeometricArithmeticModule.CalculateOrientation(this).Yaw;
            }
            set
            {
                angle = value;
            }
        }

        private double gradient = 0;
        public double Gradient
        {
            get
            {
                return GeometricArithmeticModule.CalculateGradient2D(this);
            }
            set
            {
                gradient = value;
            }
        }

        private GeometricEulerOrientation orientation = new GeometricEulerOrientation();
        public GeometricEulerOrientation Orientation
        {
            get
            {
                return GeometricArithmeticModule.CalculateOrientation(this);
            }
            set
            {
                orientation = value;
            }
        }

        public MarkGeometryPoint[] Points { get; set; } = new MarkGeometryPoint[2];

        public MarkGeometryLine()
            : base()
        {
            ReferencePoint = StartPoint;

            Update();
        }

        public MarkGeometryLine(netDxf.Entities.Line line)
            : base()
        {
            StartPoint = new MarkGeometryPoint(line.StartPoint);
            EndPoint = new MarkGeometryPoint(line.EndPoint);
            ReferencePoint = (MarkGeometryPoint)StartPoint.Clone();

            Update();
        }

        /// <summary>
        ///     This is the copy constructor
        /// </summary>
        /// <param name="input">The input to copy</param>
        internal MarkGeometryLine(MarkGeometryLine input)
            : base(input)
        {
            StartPoint = (MarkGeometryPoint) input.StartPoint.Clone();
            EndPoint = (MarkGeometryPoint) input.EndPoint.Clone();
            ReferencePoint = (MarkGeometryPoint) input.ReferencePoint.Clone();

            Update();
        }

        public MarkGeometryLine(MarkGeometryPoint p1, MarkGeometryPoint p2)
            : base()
        {
            StartPoint = p1;
            EndPoint = p2;
            ReferencePoint = (MarkGeometryPoint) StartPoint.Clone();

            Update();
        }

        public MarkGeometryLine(double x1, double y1, double x2, double y2)
            : base()
        {
            StartPoint = new MarkGeometryPoint(x1, y1);
            EndPoint = new MarkGeometryPoint(x2, y2);
            ReferencePoint = (MarkGeometryPoint) StartPoint.Clone();

            Update();
        }

        public MarkGeometryLine(double x1, double y1, double z1, double x2, double y2, double z2)
            : base()
        {
            StartPoint = new MarkGeometryPoint(x1, y1, z1);
            EndPoint = new MarkGeometryPoint(x2, y2, z2);
            ReferencePoint = (MarkGeometryPoint) StartPoint.Clone();

            Update();
        }

        public static explicit operator Matrix4x4(MarkGeometryLine line)
        {
            return new Matrix4x4(
                (float)line.StartPoint.X, (float)line.EndPoint.X, 0, 0,
                (float)line.StartPoint.Y, (float)line.EndPoint.Y, 0, 0,
                (float)line.StartPoint.Z, (float)line.EndPoint.Z, 1, 0,
                1, 1, 0, 1
            );
        }

        public static explicit operator MarkGeometryPoint[](MarkGeometryLine line)
        {
            return new MarkGeometryPoint[] { line.StartPoint, line.EndPoint };
        }

        public static implicit operator LineSegment2DF(MarkGeometryLine line)
        {
            return new LineSegment2DF(line.StartPoint, line.EndPoint);
        }

        public override void Update()
        {
            Points[0] = StartPoint;
            Points[1] = EndPoint;

            SetExtents();
        }

        public override object Clone()
        {
            return new MarkGeometryLine(this);
        }

        public void Reverse()
        {
            (StartPoint, EndPoint) = (EndPoint, StartPoint);
            Update();
        }

        public MarkGeometryPoint GetMidpoint()
        {
            return GeometricArithmeticModule.GetPointAtPosition(this, 0.5);
        }

        public override void SetExtents()
        {
            Extents.MinX = Math.Min(StartPoint.X, EndPoint.X);
            Extents.MaxX = Math.Max(StartPoint.X, EndPoint.X);

            Extents.MinY = Math.Min(StartPoint.Y, EndPoint.Y);
            Extents.MaxY = Math.Max(StartPoint.Y, EndPoint.Y);

            Extents.MinZ = Math.Min(StartPoint.Z, EndPoint.Z);
            Extents.MaxZ = Math.Max(StartPoint.Z, EndPoint.Z);

            Length = GeometricArithmeticModule.CalculatePerimeter(this);
            Perimeter = Length; // don't use perimeter of a rectangle {2 * (Length + Double.Epsilon)} - the perimeter of a line should be its length;
            Area = Length * Double.Epsilon;
        }

        public override void Transform(Matrix4x4 transformationMatrixIn)
        {
            StartPoint.Transform(transformationMatrixIn);
            EndPoint.Transform(transformationMatrixIn);

            Update();
        }

        public override void ReadXml(XmlReader reader)
        {
            reader.Read();// Skip ahead to next node
            base.ReadXml(reader);

            StartPoint = new MarkGeometryPoint();
            StartPoint.ReadXml(reader);

            EndPoint = new MarkGeometryPoint();
            EndPoint.ReadXml(reader);

            ReferencePoint = new MarkGeometryPoint();
            ReferencePoint.ReadXml(reader);

            Update();
        }

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(GetType().ToString());
            base.WriteXml(writer);

            StartPoint.WriteXml(writer);
            EndPoint.WriteXml(writer);
            ReferencePoint.WriteXml(writer);
            writer.WriteEndElement();
        }

        public override netDxf.Entities.EntityObject GetAsDXFEntity()
        {
            return new netDxf.Entities.Line(
                    StartPoint.GetAsDXFVector(), EndPoint.GetAsDXFVector()
                );
        }

        public override netDxf.Entities.EntityObject GetAsDXFEntity(string layer)
        {
            return new netDxf.Entities.Line(
                    StartPoint.GetAsDXFVector(), EndPoint.GetAsDXFVector()
                ) { Layer = new netDxf.Tables.Layer(layer) };
        }

        public override void Draw2D(IMarkGeometryVisualizer2D view, bool shouldShowVertex)
        {
            view.Draw2D(this, shouldShowVertex);
        }

        public override string ToString()
        {
            return $"{{'StartPoint': {StartPoint}, 'EndPoint': {EndPoint}}}";
        }
    }
}
