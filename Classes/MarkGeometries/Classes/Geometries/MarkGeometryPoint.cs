using MathNet.Numerics.LinearAlgebra;
using netDxf.Entities;
using System;
using System.Xml;
using System.Xml.Serialization;

namespace MSolvLib.MarkGeometry
{
    [Serializable]
    public class MarkGeometryPoint : MarkGeometry
    {
        public override string Name => "Point";
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double Z { get; set; } = 0;

        public override double Area { get; protected set; } = Math.PI * Math.Pow(Double.Epsilon, 2);
        public override double Perimeter { get; protected set; } = 2 * Math.PI * Double.Epsilon;

        [XmlIgnore]
        private double radius = 0;
        public double Radius
        {
            get
            {
                return GeometricArithmeticModule.CalculateRadius(this);
            }
            set
            {
                radius = value;
            }
        }

        [XmlIgnore]
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

        [XmlIgnore]
        public GeometricEulerOrientation Orientation
        {
            get
            {
                return GeometricArithmeticModule.CalculateOrientation(this);
            }
        }

        public MarkGeometryPoint()
            : base()
        {
            Update();
        }

        /// <summary>
        ///     The copy constructor for this class.
        /// </summary>
        /// <param name="input"></param>
        internal MarkGeometryPoint(MarkGeometryPoint input)
            : base(input)
        {
            // force copy

            X = input.X + 0.0;
            Y = input.Y + 0.0;
            Z = input.Z + 0.0;

            Update();
        }

        public MarkGeometryPoint(double X, double Y)
            : base()
        {
            Z = 0.0;
            this.X = X;
            this.Y = Y;

            Update();
        }

        public MarkGeometryPoint(double X, double Y, double Z)
            : base()
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;

            Update();
        }

        public MarkGeometryPoint(netDxf.Vector3 p)
            : base()
        {
            X = p.X;
            Y = p.Y;
            Z = p.Z;

            Update();
        }

        public MarkGeometryPoint(netDxf.Vector2 p)
            : base()
        {
            X = p.X;
            Y = p.Y;
            Z = 0;

            Update();
        }

        public MarkGeometryPoint(Point p)
            : base()
        {
            X = p.Position.X;
            Y = p.Position.Y;
            Z = p.Position.Z;

            Update();
        }

        public MarkGeometryPoint(Matrix<double> matrix)
            : base()
        {
            X = matrix[0, 0];
            Y = matrix[1, 0];
            Z = matrix[2, 0];

            Update();
        }

        public MarkGeometryPoint(LwPolylineVertex lwPolylineVertexIn)
            : base()
        {
            X = lwPolylineVertexIn.Position.X;
            Y = lwPolylineVertexIn.Position.Y;
            Z = 0;

            Update();
        }

        public static explicit operator netDxf.Vector3(MarkGeometryPoint point)
        {
            return new netDxf.Vector3(point.X, point.Y, point.Z);
        }

        public static explicit operator netDxf.Vector2(MarkGeometryPoint point)
        {
            return new netDxf.Vector2(point.X, point.Y);
        }

        public static implicit operator System.Drawing.PointF(MarkGeometryPoint point)
        {
            return new System.Drawing.PointF((float)point.X, (float)point.Y);
        }

        public static implicit operator System.Drawing.Point(MarkGeometryPoint point)
        {
            return new System.Drawing.Point((int)point.X, (int)point.Y);
        }

        public static explicit operator Matrix<double>(MarkGeometryPoint point)
        {
            var matrix = Matrix<double>.Build.Dense(1, 4);
            matrix[0, 0] = point.X;
            matrix[0, 1] = point.Y;
            matrix[0, 2] = point.Z;
            matrix[0, 3] = 1;
            return matrix;
        }

        public static explicit operator Vector<double>(MarkGeometryPoint point)
        {
            var vector = Vector<double>.Build.Dense(3);
            vector[0] = point.X;
            vector[1] = point.Y;
            vector[2] = point.Z;
            return vector;
        }

        public static MarkGeometryPoint operator +(MarkGeometryPoint p1, MarkGeometryPoint p2)
        {
            return new MarkGeometryPoint(
                    p1.X + p2.X,
                    p1.Y + p2.Y,
                    p1.Z + p2.Z
                );
        }

        public static MarkGeometryPoint operator +(MarkGeometryPoint p1, double number)
        {
            return p1 + new MarkGeometryPoint(number, number, number);
        }

        public static MarkGeometryPoint operator -(MarkGeometryPoint p1, MarkGeometryPoint p2)
        {
            return new MarkGeometryPoint(
                    p1.X - p2.X,
                    p1.Y - p2.Y,
                    p1.Z - p2.Z
                );
        }

        public static MarkGeometryPoint operator -(MarkGeometryPoint p1, double number)
        {
            return p1 - new MarkGeometryPoint(number, number, number);
        }

        public static MarkGeometryPoint operator *(MarkGeometryPoint p1, MarkGeometryPoint p2)
        {
            return new MarkGeometryPoint(
                    p1.X * p2.X,
                    p1.Y * p2.Y,
                    p1.Z * p2.Z
                );
        }

        public static MarkGeometryPoint operator *(MarkGeometryPoint p1, double number)
        {
            return p1 * new MarkGeometryPoint(number, number, number);
        }

        public static MarkGeometryPoint operator /(MarkGeometryPoint p1, MarkGeometryPoint p2)
        {
            return new MarkGeometryPoint(
                    p1.X / p2.X,
                    p1.Y / p2.Y,
                    p1.Z / p2.Z
                );
        }

        public static MarkGeometryPoint operator /(MarkGeometryPoint p1, double number)
        {
            return p1 / new MarkGeometryPoint(number, number, number);
        }

        public static bool operator <(MarkGeometryPoint p1, MarkGeometryPoint p2)
        {
            return GeometricArithmeticModule.Measure(p1, p2) < 0;
        }

        public static bool operator <=(MarkGeometryPoint p1, MarkGeometryPoint p2)
        {
            return GeometricArithmeticModule.Measure(p1, p2) <= 0;
        }

        public static bool operator >(MarkGeometryPoint p1, MarkGeometryPoint p2)
        {
            return GeometricArithmeticModule.Measure(p1, p2) > 0;
        }

        public static bool operator >=(MarkGeometryPoint p1, MarkGeometryPoint p2)
        {
            return GeometricArithmeticModule.Measure(p1, p2) >= 0;
        }

        //public static bool operator ==(MarkGeometryPoint p1, MarkGeometryPoint p2)
        //{
        //    return GeometricArithmeticModule.Measure(p1, p2) == 0;
        //}

        //public static bool operator !=(MarkGeometryPoint p1, MarkGeometryPoint p2)
        //{
        //    return GeometricArithmeticModule.Measure(p1, p2) != 0;
        //}

        public override void Update()
        {
            SetExtents();
        }

        public override object Clone()
        {
            return new MarkGeometryPoint(this);
        }

        public override void SetExtents()
        {
            Extents.MaxX = X; Extents.MinX = X;
            Extents.MaxY = Y; Extents.MinY = Y;
            Extents.MaxZ = Z; Extents.MinZ = Z;
        }

        public override void Transform(Matrix<double> transformationMatrixIn)
        {
            var result = (Matrix<double>)this * transformationMatrixIn;

            X = result[0, 0];
            Y = result[0, 1];
            Z = result[0, 2];

            Update();
        }

        public override EntityObject GetAsDXFEntity()
        {
            return new Point (
                    X, Y, Z
                );
        }

        public override EntityObject GetAsDXFEntity(string layer)
        {
            return new Point(X, Y, Z) { Layer = new netDxf.Tables.Layer(layer) };
        }

        public netDxf.Vector3 GetAsDXFVector()
        {
            return new netDxf.Vector3(
                    X, Y, Z
                );
        }

        public override void Draw2D(IMarkGeometryVisualizer2D view, bool shouldShowVertex)
        {
            view.Draw2D(this);
        }

        public override string ToString()
        {
            return $"{{X:{X}, Y:{Y}, Z:{Z}}}";
        }

        public override void ReadXml(XmlReader reader)
        {
            reader.Read();// Skip ahead to next node
            ReadXmlBaseImpl(reader);

            X = double.Parse(reader.GetAttribute(nameof(X)));
            Y = double.Parse(reader.GetAttribute(nameof(Y)));
            Z = double.Parse(reader.GetAttribute(nameof(Z)));
        }

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(GetType().ToString());
            WriteXmlBaseImpl(writer);

            writer.WriteAttributeString(nameof(X), X.ToString());
            writer.WriteAttributeString(nameof(Y), Y.ToString());
            writer.WriteAttributeString(nameof(Z), Z.ToString());
            writer.WriteEndElement();
        }
    }
}
