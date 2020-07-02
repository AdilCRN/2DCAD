using MathNet.Numerics.LinearAlgebra;
using netDxf.Entities;
using System.Collections.Generic;
using System.Linq;

namespace MSolvLib.MarkGeometry
{
    public class MarkGeometrySpline : MarkGeometryPath
    {
        // Number of points per unit distance
        public double Density { get; set; } = 0.1;

        public List<MarkGeometryPoint> FitPoints { get; set; } = new List<MarkGeometryPoint>();
        public List<MarkGeometryPoint> ControlPoints { get; set; } = new List<MarkGeometryPoint>();
        public List<double> Knots { get; set; } = new List<double>();
        public List<double> Weights { get; set; } = new List<double>();
        public bool IsPeriodic { get; set; } = false;

        public MarkGeometryPoint StartPoint => GeometricArithmeticModule.GetPointAtPosition(this, 0);
        public MarkGeometryPoint EndPoint => GeometricArithmeticModule.GetPointAtPosition(this, 1.0);

        public override string Name => "Spline";

        public MarkGeometrySpline(List<double> knotsIn, List<MarkGeometryPoint> controlPointsIn, List<MarkGeometryPoint> fitPointsIn)
        {
            Knots = knotsIn;
            FitPoints = fitPointsIn;
            ControlPoints = controlPointsIn;
        }

        public MarkGeometrySpline(Spline spline)
            : base()
        {
            FitPoints = spline.FitPoints.ConvertAll(x => new MarkGeometryPoint(x));
            ControlPoints = new List<SplineVertex>(spline.ControlPoints).ConvertAll(x => new MarkGeometryPoint(x.Position)); ;
            Knots = spline.Knots.ToList();
            IsPeriodic = spline.IsPeriodic;

            // TODO : Remove
            Lines = new MarkGeometryPath(spline.ToPolyline(spline.ControlPoints.Count * 5)).Lines;

            Update();
        }


        public void GenerateView()
        {
            //List<MarkGeometryPoint> Points = new List<MarkGeometryPoint>();

            //for (int i=0; i<FitPoints.Count-1; i++)
            //{
            //    var StartPoint = FitPoints[i];
            //    var EndPoint = FitPoints[i + 1];

            //    int numberOfInterpolationPoints = (int) Math.Floor(Density * GeometricArithmeticModule.ABSMeasure(EndPoint, StartPoint));

            //    for (int j=0; j<numberOfInterpolationPoints; j++)
            //    {
            //        double x = 0;
            //        double y = 0;
            //        double z = 0;

            //        Points.Add(new MarkGeometryPoint(x, y, z));
            //    }
            //}


            //StartPoint = GeometricArithmeticModule.GetPointAtAngle(CentrePoint, MajorAxis, MinorAxis, StartAngle);
            //EndPoint = GeometricArithmeticModule.GetPointAtAngle(CentrePoint, MajorAxis, MinorAxis, EndAngle);
            //IsClosed = Math.Abs(StartAngle - EndAngle) >= 360 || (StartPoint == EndPoint);
        }

        public override void Update()
        {
            GenerateView();
            base.Update();
        }

        public override void Transform(Matrix<double> transformationMatrixIn)
        {
            base.Transform(transformationMatrixIn);
        }

        //public override EntityObject GetAsDXFEntity()
        //{
        //    return new Spline()
        //}

        //public override EntityObject GetAsDXFEntity(string layer)
        //{
        //    return new Spline(
        //            (Vector3)CentrePoint, MajorAxis, MinorAxis
        //        )
        //    { Layer = new netDxf.Tables.Layer(layer) };
        //}
    }
}
