using MSolvLib.Classes.DXFTiling;
using netDxf;
using netDxf.Entities;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace MSolvLib.MarkGeometry
{
    public class MarkGeometrySpline : MarkGeometryPath
    {
        /// <summary>
        /// Number of points per unit distance
        /// </summary>
        public double Density { get; set; } = 0.1;

        public List<MarkGeometryPoint> FitPoints { get; set; } = new List<MarkGeometryPoint>();
        public List<MarkGeometryPoint> ControlPoints { get; set; } = new List<MarkGeometryPoint>();
        public List<double> Knots { get; set; } = new List<double>();
        public List<double> Weights { get; set; } = new List<double>();
        public bool IsPeriodic { get; set; } = false;
        public int Degree { get; set; } = 0;

        public override string Name => "Spline";

        public MarkGeometrySpline(
            int flag,
            int degree,
            List<double> knotsIn, 
            List<MarkGeometryPoint> controlPointsIn, 
            List<MarkGeometryPoint> fitPointsIn
        )
        {
            Degree = degree;
            IsClosed = (flag == 1);
            IsPeriodic = (flag == 2);
            Knots.AddRange(knotsIn);
            ControlPoints.AddRange(controlPointsIn);
            FitPoints.AddRange(fitPointsIn);

            // generate points
            Points.AddRange(ApproximatePoints(3 * ControlPoints.Count));
            Update();
        }

        public MarkGeometrySpline(Spline spline)
            : base()
        {
            FitPoints = spline.FitPoints.ConvertAll(x => new MarkGeometryPoint(x));
            ControlPoints = new List<SplineVertex>(spline.ControlPoints).ConvertAll(x => new MarkGeometryPoint(x.Position)); ;
            Knots = spline.Knots.ToList();
            IsPeriodic = spline.IsPeriodic;
            IsClosed = spline.IsClosed;
            Degree = spline.Degree;

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

        /// <summary>
        /// Converts the spline in a list of vertexes.
        /// Nurbs evaluator provided by mikau16 based on Michael V. implementation, roughly follows the notation of http://cs.mtu.edu/~shene/PUBLICATIONS/2004/NURBS.pdf
        /// </summary>
        /// <param name="howmany">Number of vertexes generated.</param>
        /// <returns>A list vertexes that represents the spline.</returns>
        public List<MarkGeometryPoint> ApproximatePoints(int howmany)
        {
            if (ControlPoints.Count <= 0)
                return null;

            double uStart, uEnd;
            var points = new List<MarkGeometryPoint>();

            if (!IsClosed)
            {
                howmany -= 1;
                uStart = Knots[0];
                uEnd = Knots[Knots.Count - 1];
            }
            else if (IsPeriodic)
            {
                uStart = Knots[Degree];
                uEnd = Knots[Knots.Count - Degree - 1];
            }
            else
            {
                uStart = Knots[0];
                uEnd = Knots[Knots.Count - 1];
            }

            var delta = (uEnd - uStart) / howmany;

            for (int i = 0; i < howmany; i++)
            {
                points.Add(C(uStart + (delta * i)));
            }

            if (!IsClosed)
                points.Add(ControlPoints[ControlPoints.Count - 1]);

            return points;
        }

        /// <summary>
        /// Adapted from CSharp's netdxf library.
        /// </summary>
        /// <param name="position">A double representing a position on the spline</param>
        /// <returns>A point at the specified location on the Spline</returns>
        public MarkGeometryPoint C(double position)
        {
            double denominatorSum = 0.0;
            var _point = new MarkGeometryPoint();

            for (int i = 0; i < ControlPoints.Count; i++)
            {
                // optional use n * controlPoint's weight - in this case it's 1.0
                double n = N(i, Degree, position);
                denominatorSum += n;
                _point += n * ControlPoints[i];
            }

            // avoid possible divided by zero error, this should never happen
            if (Math.Abs(denominatorSum) < double.Epsilon)
                return new MarkGeometryPoint();

            return (1.0 / denominatorSum) * _point;
        }


        /// <summary>
        /// Adapted from CSharp's netdxf library.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="degree"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private double N(int i, int degree, double position)
        {
            if (degree <= 0)
            {
                if (Knots[i] <= position && position < Knots[i + 1])
                    return 1;
                return 0.0;
            }

            double leftCoefficient = 0.0;
            if (!(Math.Abs(Knots[i + degree] - Knots[i]) < double.Epsilon))
                leftCoefficient = (position - Knots[i]) / (Knots[i + degree] - Knots[i]);

            double rightCoefficient = 0.0; // article contains error here, denominator is Knots[i + p + 1] - Knots[i + 1]
            if (!(Math.Abs(Knots[i + degree + 1] - Knots[i + 1]) < double.Epsilon))
                rightCoefficient = (Knots[i + degree + 1] - position) / (Knots[i + degree + 1] - Knots[i + 1]);

            return leftCoefficient * N(i, degree - 1, position) + rightCoefficient * N(i + 1, degree - 1, position);
        }

        public override void Transform(Matrix4x4 transformationMatrixIn)
        {
            Parallel.For(0, ControlPoints.Count, (i)=> {
                ControlPoints[i].Transform(transformationMatrixIn);
            });

            Parallel.For(0, FitPoints.Count, (i) => {
                FitPoints[i].Transform(transformationMatrixIn);
            });

            base.Transform(transformationMatrixIn); 
        }

        public override void Update()
        {
            // Perimeter is calculated within SetExtents
            SetExtents();

            if (Points.Count < 2)
            {
                Area = 0; // same as point
                return;
            }
            else if (Points.Count < 4)
            {
                Area = Extents.Hypotenuse * Double.Epsilon; // same as line
                return;
            }

            // calculating area of an irregular polygon
            double A = 0;
            double B = 0;

            for (var i = 0; i < Points.Count - 1; i++)
            {
                A += Points[i].X * Points[i + 1].Y;
                B += Points[i].Y * Points[i + 1].X;
            }

            if (!IsClosed)
            {
                A += EndPoint.X * StartPoint.Y;
                B += EndPoint.Y * StartPoint.X;
            }

            Area = Math.Abs((A - B) / 2.0);
        }
    }
}
