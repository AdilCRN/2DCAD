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
            var polyline = spline.ToPolyline(spline.ControlPoints.Count * 5);
            Points.AddRange(
                polyline.Vertexes.Select(v => new MarkGeometryPoint(v.Position))
            );

            if (
                polyline.IsClosed &&
                GeometricArithmeticModule.Compare(StartPoint, EndPoint, ClosureTolerance) != 0)
            {
                Points.Add((MarkGeometryPoint)StartPoint.Clone());
            }
            // END Remove

            Update();
        }
    }
}
