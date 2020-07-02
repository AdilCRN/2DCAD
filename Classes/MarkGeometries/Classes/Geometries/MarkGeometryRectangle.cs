using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using System.Xml.Serialization;
using MathNet.Numerics.LinearAlgebra;

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

        public MarkGeometryRectangle()
            : base()
        {

        }

        public MarkGeometryRectangle(double width, double height)
            : base()
        {
            Lines = new List<MarkGeometryLine>();
            CentrePoint = new MarkGeometryPoint();
            Width = width;
            Height = height;

            GenerateView();
            Update();
        }

        public MarkGeometryRectangle(MarkGeometryPoint centrePoint, double width, double height)
            : base()
        {
            Lines = new List<MarkGeometryLine>();
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
            Lines = rect.Lines;
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
            Lines = new List<MarkGeometryLine>();
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
            Lines = new List<MarkGeometryLine>();
            Lines.AddRange(GeometricArithmeticModule.ToLines(TopLeftPoint, TopRightPoint, BottomRightPoint, BottomLeftPoint, TopLeftPoint));
        }

        public override object Clone()
        {
            return new MarkGeometryRectangle(this);
        }

        public override void Update()
        {
            Area = Height * Width;
            Perimeter = 2 * (Height + Width);

            foreach (var line in Lines)
            {
                line.Update();
            }

            base.SetExtents();
        }

        public override void Transform(Matrix<double> transformationMatrixIn)
        {
            TopLeftPoint.Transform(transformationMatrixIn);
            TopRightPoint.Transform(transformationMatrixIn);
            BottomLeftPoint.Transform(transformationMatrixIn);
            BottomRightPoint.Transform(transformationMatrixIn);

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
            Lines = new List<MarkGeometryLine>();

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
