using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace MSolvLib.MarkGeometry
{
    public class MarkGeometryRectangle : MarkGeometryPath, IXmlSerializable
    {
        public override string Name => "Rectangle";

        public double Width { get; set; }
        public double Height { get; set; }

        public override double Area { get; protected set; }
        public override double Perimeter { get; protected set; }

        public MarkGeometryPoint TopLeftPoint { get; private set; } = new MarkGeometryPoint();
        public MarkGeometryPoint TopRightPoint { get; private set; } = new MarkGeometryPoint();
        public MarkGeometryPoint BottomLeftPoint { get; private set; } = new MarkGeometryPoint();
        public MarkGeometryPoint BottomRightPoint { get; private set; } = new MarkGeometryPoint();

        public MarkGeometryLine TopEdge => new MarkGeometryLine(TopLeftPoint, TopRightPoint);
        public MarkGeometryLine BottomEdge => new MarkGeometryLine(BottomLeftPoint, BottomRightPoint);
        public MarkGeometryLine LeftEdge => new MarkGeometryLine(TopLeftPoint, BottomLeftPoint);
        public MarkGeometryLine RightEdge => new MarkGeometryLine(TopRightPoint, BottomRightPoint);

        public MarkGeometryRectangle()
            : base()
        {

        }

        public MarkGeometryRectangle(double width, double height)
            : base()
        {
            Width = width;
            Height = height;

            GenerateView();
            Update();
        }

        public MarkGeometryRectangle(MarkGeometryPoint centrePoint, double width, double height)
            : base()
        {
            CentrePoint = centrePoint;
            Width = width;
            Height = height;

            GenerateView();
            Update();
        }

        public MarkGeometryRectangle(MarkGeometryPoint pointA, MarkGeometryPoint pointB)
            : base()
        {
            MarkGeometryRectangle rect = new MarkGeometryLine(pointA, pointB).Extents.Boundary;
            Points.AddRange(rect.Points);
            CentrePoint = rect.CentrePoint;
            Width = rect.Width;
            Height = rect.Height;
            IsClosed = true;

            double halfWidth = Width / 2.0;
            double halfHeight = Height / 2.0;

            TopLeftPoint = new MarkGeometryPoint(CentrePoint.X - halfWidth, CentrePoint.Y + halfHeight);
            TopRightPoint = new MarkGeometryPoint(CentrePoint.X + halfWidth, CentrePoint.Y + halfHeight);
            BottomLeftPoint = new MarkGeometryPoint(CentrePoint.X - halfWidth, CentrePoint.Y - halfHeight);
            BottomRightPoint = new MarkGeometryPoint(CentrePoint.X + halfWidth, CentrePoint.Y - halfHeight);

            Update();
        }

        /// <summary>
        ///     The copy constructor.
        /// </summary>
        /// <param name="input"></param>
        protected MarkGeometryRectangle(MarkGeometryRectangle input)
            : base(input)
        {
            Width = input.Width;
            Height = input.Height;
            CentrePoint = (MarkGeometryPoint)input.CentrePoint.Clone();

            GenerateView();
            Update();
        }

        public static explicit operator RectangleF(MarkGeometryRectangle rectangle)
        {
            return new RectangleF(
                (float)rectangle.TopLeftPoint.X,
                (float)rectangle.TopLeftPoint.Y,
                (float)rectangle.Width,
                (float)rectangle.Height
            );
        }

        public void GenerateView()
        {
            double halfWidth = Width / 2.0;
            double halfHeight = Height / 2.0;

            TopLeftPoint = new MarkGeometryPoint(CentrePoint.X - halfWidth, CentrePoint.Y + halfHeight);
            TopRightPoint = new MarkGeometryPoint(CentrePoint.X + halfWidth, CentrePoint.Y + halfHeight);
            BottomLeftPoint = new MarkGeometryPoint(CentrePoint.X - halfWidth, CentrePoint.Y - halfHeight);
            BottomRightPoint = new MarkGeometryPoint(CentrePoint.X + halfWidth, CentrePoint.Y - halfHeight);

            IsClosed = true;
            Points = new List<MarkGeometryPoint>()
            {
                TopLeftPoint, TopRightPoint, BottomRightPoint, BottomLeftPoint, (MarkGeometryPoint)TopLeftPoint.Clone()
            };
        }

        public override string ToString()
        {
            return $"{{'CentrePoint': {CentrePoint}, 'Width': {Width}, 'Height': {Height}}}";
        }

        public override object Clone()
        {
            return new MarkGeometryRectangle(this);
        }

        public override void Update()
        {
            Area = Height * Width;
            Perimeter = 2 * (Height + Width);

            for (int i = 0; i < Points.Count; i++)
            {
                Points[i].Update();
            }

            base.SetExtents();
        }

        public override void Transform(Matrix4x4 transformationMatrixIn)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                Points[i].Transform(transformationMatrixIn);
            }

            Width = GeometricArithmeticModule.ABSMeasure2D(TopLeftPoint, TopRightPoint);
            Height = GeometricArithmeticModule.ABSMeasure2D(TopLeftPoint, BottomLeftPoint);

            Update();
        }

        public override void ReadXml(XmlReader reader)
        {
            reader.Read();// Skip ahead to next node
            ReadXmlBaseImpl(reader);

            reader.ReadStartElement();
            Width = double.Parse(reader.ReadElementString(nameof(Width)));
            reader.Read();

            Height = double.Parse(reader.ReadElementString(nameof(Height)));
            Points = new List<MarkGeometryPoint>();

            CentrePoint = new MarkGeometryPoint();
            CentrePoint.ReadXml(reader);

            reader.Read();
            reader.ReadEndElement();

            GenerateView();
            Update();
        }

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(GetType().ToString());
            WriteXmlBaseImpl(writer);

            writer.WriteElementString(nameof(Width), Width.ToString());
            writer.WriteElementString(nameof(Height), Height.ToString());

            CentrePoint.WriteXml(writer);

            writer.WriteEndElement();
        }
    }
}
