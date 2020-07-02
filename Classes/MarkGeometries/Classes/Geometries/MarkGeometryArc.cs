using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MSolvLib.MarkGeometry
{
    public class MarkGeometryArc : MarkGeometry
    {
        public override string Name => "Arc";
        public MarkGeometryPoint CentrePoint { get; set; }
        public double Radius { get; set; } = 0;
        public double StartAngle { get; set; } = 0;
        public double EndAngle { get; set; } = 0;
        public double Sweep { get; set; } = 0;

        public override double Area { get; protected set; } = 0;
        public override double Perimeter { get; protected set; } = 0;

        public MarkGeometryPoint StartPoint { get; set; }
        public MarkGeometryPoint EndPoint { get; set; }

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

        public MarkGeometryArc()
            : base()
        {
            Radius = 0;
            StartAngle = 0;
            EndAngle = 0;
            CentrePoint = new MarkGeometryPoint();

            StartPoint = new MarkGeometryPoint();
            EndPoint = new MarkGeometryPoint();

            Update();
        }

        public MarkGeometryArc(Arc arc)
            : base()
        {
            Radius = arc.Radius;
            StartAngle = arc.StartAngle * Math.PI / 180.0;
            EndAngle = arc.EndAngle * Math.PI / 180.0;
            CentrePoint = new MarkGeometryPoint(arc.Center);

            Update();
        }

        internal MarkGeometryArc(MarkGeometryArc input)
            : base(input)
        {
            Radius = input.Radius + 0;
            StartAngle = input.StartAngle + 0;
            EndAngle = input.EndAngle + 0;

            CentrePoint = (MarkGeometryPoint) input.CentrePoint.Clone();
            StartPoint = (MarkGeometryPoint) input.StartPoint.Clone();
            EndPoint = (MarkGeometryPoint) input.EndPoint.Clone();
            VertexCount = input.VertexCount + 0;

            Update();
        }
        public MarkGeometryArc(MarkGeometryPoint center, double radius, double startAngle, double endAngle)
            : base()
        {
            Radius = radius;
            StartAngle = startAngle;
            EndAngle = endAngle;
            CentrePoint = center;

            StartPoint = new MarkGeometryPoint();
            EndPoint = new MarkGeometryPoint();

            Update();
        }

        public MarkGeometryArc(MarkGeometryPoint center, double radius, double endAngle)
            : base()
        {
            Radius = radius;
            StartAngle = 0;
            EndAngle = endAngle;
            CentrePoint = center;
            
            Update();
        }

        public MarkGeometryArc(double x, double y, double radius, double startAngle, double endAngle)
            : base()
        {
            Radius = radius;
            StartAngle = startAngle;
            EndAngle = endAngle;
            CentrePoint = new MarkGeometryPoint(x, y);

            Update();
        }

        public MarkGeometryArc(double x, double y, double z, double radius, double startAngle, double endAngle)
            : base()
        {
            Radius = radius;
            StartAngle = startAngle;
            EndAngle = endAngle;
            CentrePoint = new MarkGeometryPoint(x, y, z);

            Update();
        }

        public static explicit operator MarkGeometryPoint[](MarkGeometryArc arc)
        {
            List<MarkGeometryPoint> points = new List<MarkGeometryPoint>();

            for (int i = 0; i <= arc.VertexCount; i++)
            {
                points.Add(
                    GeometricArithmeticModule.GetPointAtPosition(arc, (double)i / (double)arc.VertexCount)
                );
            }

            return points.ToArray();
        }

        public static explicit operator MarkGeometryLine[](MarkGeometryArc arc)
        {
            return GeometricArithmeticModule.SplitGeometry(arc, arc.VertexCount).ToArray();
        }

        public override object Clone()
        {
            return new MarkGeometryArc(this);
        }

        public override void SetExtents()
        {
            if (EndAngle < StartAngle)
                Sweep = (EndAngle + (2 * Math.PI)) - StartAngle;
            else
                Sweep = EndAngle - StartAngle;

            Area = 0.5 * Sweep * Math.Pow(Radius, 2);
            Perimeter = Sweep * Radius;

            // determine start and end points
            StartPoint = GeometricArithmeticModule.GetPointAtPosition(this, 0.0);
            EndPoint = GeometricArithmeticModule.GetPointAtPosition(this, 1.0); ;
            Extents = GeometricArithmeticModule.CalculateExtents((MarkGeometryPoint[]) this);
        }

        public override void Transform(Matrix4x4 transformationMatrixIn)
        {
            CentrePoint.Transform(transformationMatrixIn);
            StartPoint.Transform(transformationMatrixIn);
            
            Radius = GeometricArithmeticModule.ABSMeasure(StartPoint, CentrePoint);

            Update();
        }

        public override void Update()
        {
            SetExtents();
        }

        public override string ToString()
        {
            return $"{{'CentrePoint': {CentrePoint}, 'Radius': {Radius}, 'StartAngle': {StartAngle / Math.PI * 180}, 'EndAngle': {EndAngle / Math.PI * 180}}}";
        }

        public MarkGeometryPoint GetMidpoint()
        {
            return GeometricArithmeticModule.GetPointAtPosition(this, 0.5);
        }

        public override EntityObject GetAsDXFEntity()
        {
            return new Arc(
                    CentrePoint.GetAsDXFVector(),
                    Radius,
                    StartAngle / Math.PI * 180,
                    EndAngle / Math.PI * 180
                );
        }
        
        public override EntityObject GetAsDXFEntity(string layer)
        {
            return new Arc(
                    CentrePoint.GetAsDXFVector(),
                    Radius,
                    StartAngle / Math.PI * 180,
                    EndAngle / Math.PI * 180
                )
            { Layer = new netDxf.Tables.Layer(layer) };
        }

        public override void Draw2D(IMarkGeometryVisualizer2D view, bool shouldShowVertex)
        {
            view.Draw2D(this, shouldShowVertex);
        }
    }
}
