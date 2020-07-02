using netDxf;
using netDxf.Entities;
using System;
using System.Numerics;

namespace MSolvLib.MarkGeometry
{
    public class MarkGeometryEllipse : MarkGeometryPath
    {
        public override string Name => "Ellipse";
        public int VertexCount { get; set; } = 32;
        public double StartAngle { get; set; } = 0;
        public double EndAngle { get; set; } = 0;
        public double MinorAxis { get; set; } = 0;
        public double MajorAxis { get; set; } = 0;

        public MarkGeometryEllipse(Ellipse ellipse)
            : base()
        {
            CentrePoint = new MarkGeometryPoint(ellipse.Center);
            StartAngle = ellipse.StartAngle;
            EndAngle = ellipse.EndAngle;
            MinorAxis = ellipse.MajorAxis; // AKA the y-axis
            MajorAxis = ellipse.MinorAxis; // AKA. the x-axis

            Update();
        }

        public void GenerateView()
        {
            for (int i = 0; i < VertexCount; i++)
            {
                Points.Add(
                    GeometricArithmeticModule.GetPointAtAngle(
                        CentrePoint,
                        MajorAxis,
                        MinorAxis,
                        GeometricArithmeticModule.Map(i, 0, VertexCount-1, StartAngle, EndAngle)
                    )
                );
            }

            //StartPoint = GeometricArithmeticModule.GetPointAtAngle(CentrePoint, MajorAxis, MinorAxis, StartAngle);
            //EndPoint = GeometricArithmeticModule.GetPointAtAngle(CentrePoint, MajorAxis, MinorAxis, EndAngle);
            IsClosed = Math.Abs(StartAngle - EndAngle) >= (2.0 * Math.PI) || (StartPoint == EndPoint);

            Update();
        }

        public override void Update()
        {
            GenerateView();
            base.Update();
        }

        public override void Transform(Matrix4x4 transformationMatrixIn)
        {
            // transform centre point
            CentrePoint.Transform(transformationMatrixIn);
            StartPoint.Transform(transformationMatrixIn);

            // use updated centre point to calculate major and minor axes
            var res = GeometricArithmeticModule.CalculateMajorMinorAxis(CentrePoint, StartPoint, StartAngle);

            // update details
            MajorAxis = res.MajorAxis;
            MinorAxis = res.MinorAxis;

            Update();
        }

        public override EntityObject GetAsDXFEntity()
        {
            return new Ellipse(
                    (netDxf.Vector3) CentrePoint, MajorAxis, MinorAxis
                );
        }

        public override EntityObject GetAsDXFEntity(string layer)
        {
            return new Ellipse(
                    (netDxf.Vector3) CentrePoint, MajorAxis, MinorAxis
                )
            { Layer = new netDxf.Tables.Layer(layer) };
        }
    }
}
