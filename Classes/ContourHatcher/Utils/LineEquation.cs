using MSolvLib.MarkGeometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContourHatcher.Utils
{
    public class LineEquation
    {
        private static readonly double tolerance = 0.00001;
        public double Gradient { get; set; }
        public double YIntercept { get; set; }

        public double AlternateX { get; private set; }
        public double AlternateY { get; private set; }

        public LineEquation(MarkGeometryLine line)
        {
            Gradient = line.Gradient;

            AlternateX = line.StartPoint.X;
            AlternateY = line.StartPoint.Y;

            if (Math.Abs(line.StartPoint.X) < tolerance)
                YIntercept = line.StartPoint.Y;
            else
                YIntercept = line.StartPoint.Y - (Gradient * line.StartPoint.X);
        }

        public double CalculateX(double y)
        {
            return (y - YIntercept) / Gradient;
        }

        public double CalculateY(double x)
        {
            return (Gradient * x) + YIntercept;
        }

        public bool PassesThroughRect(MarkGeometryRectangle rect)
        {
            if (
                GeometricArithmeticModule.IsWithin(
                    CalculateY(rect.Extents.MinX), rect.Extents.MinY, rect.Extents.MaxY
                ) ||
                GeometricArithmeticModule.IsWithin(
                    CalculateX(rect.Extents.MinY), rect.Extents.MinX, rect.Extents.MaxX
                ) ||
                GeometricArithmeticModule.IsWithin(
                    CalculateY(rect.Extents.MaxX), rect.Extents.MinY, rect.Extents.MaxY
                ) ||
                GeometricArithmeticModule.IsWithin(
                    CalculateX(rect.Extents.MaxY), rect.Extents.MinX, rect.Extents.MaxX
                )
            )
                return true;

            return false;
        }
    }
}
