using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;

namespace MSolvLib.MarkGeometry
{

    public class MarkGeometryCircle : MarkGeometry
    {
        public override string Name => "Circle";
        public MarkGeometryPoint StartPoint { get; set; }
        public MarkGeometryPoint EndPoint { get; set; }

        public double Radius { get; set; } = 0;
        public override double Area { get; protected set; } = 0;
        public override double Perimeter { get; protected set; } = 0;
        public MarkGeometryPoint CentrePoint { get; set; }

        private int _vertexCount = 32;

        public int VertexCount
        {
            get { return _vertexCount; }
            set
            { 
                if (value >= 8)
                {
                    _vertexCount = value;
                }
            }
        }


        public MarkGeometryCircle()
            : base()
        {
            Radius = 0;
            CentrePoint = new MarkGeometryPoint();

            Update();
        }

        public MarkGeometryCircle(netDxf.Entities.Circle circle)
            : base()
        {
            Radius = circle.Radius;
            CentrePoint = new MarkGeometryPoint(circle.Center);

            Update();
        }

        public MarkGeometryCircle(double radius)
            : base()
        {
            Radius = radius;
            CentrePoint = new MarkGeometryPoint();

            Update();
        }

        public MarkGeometryCircle(MarkGeometryPoint centre)
            : base()
        {
            Radius = 0;
            CentrePoint = centre;

            Update();
        }

        public MarkGeometryCircle(MarkGeometryPoint centre, double radius)
            : base()
        {
            Radius = radius;
            CentrePoint = centre;

            Update();
        }

        /// <summary>
        ///     Constructor using variables explicitly
        /// </summary>
        /// <param name="x">The position of the circle on the x axis</param>
        /// <param name="y">The position of the circle on the y axis</param>
        /// <param name="z">The position of the circle on the z axis</param>
        /// <param name="radius">The radius of the circle</param>
        public MarkGeometryCircle(double x, double y, double z, double radius)
            : base()
        {
            Radius = radius;
            CentrePoint = new MarkGeometryPoint(x, y, z);

            Update();
        }

        /// <summary>
        ///     The copy constructor
        /// </summary>
        /// <param name="input"></param>
        internal MarkGeometryCircle(MarkGeometryCircle input)
            : base(input)
        {
            Radius = input.Radius + 0;
            CentrePoint = (MarkGeometryPoint) input.CentrePoint.Clone();
            VertexCount = input.VertexCount + 0;

            Update();
        }

        public static explicit operator MarkGeometryPoint[](MarkGeometryCircle circle)
        {
            List<MarkGeometryPoint> points = new List<MarkGeometryPoint>();

            for (int i = 0; i < circle.VertexCount; i++)
            {
                points.Add(
                    GeometricArithmeticModule.GetPointAtPosition(circle, (double)i / (double)circle.VertexCount)
                );
            }

            return points.ToArray();
        }

        public static explicit operator MarkGeometryLine[](MarkGeometryCircle circle)
        {
            return GeometricArithmeticModule.SplitGeometry(circle, circle.VertexCount).ToArray();
        }

        public override void Update()
        {
            SetExtents();
            Area = Math.PI * Math.Pow(Radius, 2);
            Perimeter = 2 * Math.PI * Radius;
        }

        public override object Clone()
        {
            return new MarkGeometryCircle(this);
        }

        public override void SetExtents()
        {
            Extents.MinX = CentrePoint.X - Radius;
            Extents.MaxX = CentrePoint.X + Radius;

            Extents.MinY = CentrePoint.Y - Radius;
            Extents.MaxY = CentrePoint.Y + Radius;

            Extents.MinZ = CentrePoint.Z - Radius;
            Extents.MaxZ = CentrePoint.Z + Radius;

            // determine start and end points
            StartPoint = GeometricArithmeticModule.GetPointAtAngle(CentrePoint, Radius, 0);
            EndPoint = GeometricArithmeticModule.GetPointAtAngle(CentrePoint, Radius, 2*Math.PI);
        }

        public override void Transform(Matrix<double> transformationMatrixIn)
        {
            CentrePoint.Transform(transformationMatrixIn);
            StartPoint.Transform(transformationMatrixIn);

            Radius = GeometricArithmeticModule.ABSMeasure(StartPoint, CentrePoint);
            Update();
        }

        public override netDxf.Entities.EntityObject GetAsDXFEntity()
        {
            return new netDxf.Entities.Circle(
                    CentrePoint.GetAsDXFVector(), Radius
                );
        }

        public override netDxf.Entities.EntityObject GetAsDXFEntity(string layer)
        {
            return new netDxf.Entities.Circle(
                    CentrePoint.GetAsDXFVector(), Radius
                ){ Layer = new netDxf.Tables.Layer(layer) };
        }

        public override void Draw2D(IMarkGeometryVisualizer2D view, bool shouldShowVertex)
        {
            view.Draw2D(this, shouldShowVertex);
        }
    }
}
