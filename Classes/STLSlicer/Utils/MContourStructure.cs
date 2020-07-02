using MSolvLib.MarkGeometry;
using SharpGLShader.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STLSlicer.Utils
{
    public class MContourStructure
    {
        public MLinkedList<IntersectionStructure> IntersectionList { get; set; }

        public MContourStructure(IntersectionStructure intersectionStructureIn)
        {
            IntersectionList = new MLinkedList<IntersectionStructure>();
            IntersectionList.AddLast(intersectionStructureIn);
        }

        public IEnumerable<MVertex> ToVertices()
        {
            // close contour if closed, i.e. first edge touches last edge
            if (MSTLSlicer.CompareEqual(IntersectionList.First.Value.ForwardEdge, IntersectionList.Last.Value.BackwardEdge))
                return IntersectionList.Select(ill => ill.ForwardEdgeIntersectionPoint).Append(IntersectionList.First.Value.ForwardEdgeIntersectionPoint);

            return IntersectionList.Select(ill => ill.ForwardEdgeIntersectionPoint);
        }

        public List<MarkGeometryPoint> ToPoints(double deviationToleranceDeg = 1)
        {
            if (IntersectionList.Count <= 0)
                return new List<MarkGeometryPoint>();
            else if (IntersectionList.Count <= 1)
                return new List<MarkGeometryPoint> { ToPoint(IntersectionList.First.Value.ForwardEdgeIntersectionPoint) };

            MVertex previousPoint = null;
            var lastEntry = IntersectionList.First;
            var points = new List<MarkGeometryPoint> { ToPoint(lastEntry.Value.ForwardEdgeIntersectionPoint) };
            var current = lastEntry.Next;

            double referenceAngle = GetAngle(
                lastEntry.Value.ForwardEdgeIntersectionPoint,
                current.Value.ForwardEdgeIntersectionPoint
            );

            deviationToleranceDeg = GeometricArithmeticModule.ToRadians(deviationToleranceDeg);

            while (current != null)
            {
                var angle = GetAngle(
                    lastEntry.Value.ForwardEdgeIntersectionPoint,
                    current.Value.ForwardEdgeIntersectionPoint
                );

                if (Math.Abs(referenceAngle - angle) > deviationToleranceDeg)
                {
                    lastEntry = current.Previous;
                    points.Add(ToPoint(lastEntry.Value.ForwardEdgeIntersectionPoint));

                    referenceAngle = GetAngle(
                        lastEntry.Value.ForwardEdgeIntersectionPoint,
                        current.Value.ForwardEdgeIntersectionPoint
                    );
                }

                previousPoint = current.Value.ForwardEdgeIntersectionPoint;
                current = current.Next;
            }

            if (previousPoint != null)
                points.Add(ToPoint(previousPoint));

            // close contour if closed, i.e. first edge touches last edge
            if (MSTLSlicer.CompareEqual(IntersectionList.First.Value.ForwardEdge, IntersectionList.Last.Value.BackwardEdge))
                points.Add(new MarkGeometryPoint(IntersectionList.First.Value.ForwardEdgeIntersectionPoint.X, IntersectionList.First.Value.ForwardEdgeIntersectionPoint.Y));

            return points;
        }

        private MarkGeometryPoint ToPoint(MVertex vIn)
        {
            return new MarkGeometryPoint(vIn.X, vIn.Y);
        }

        private double GetAngle(MVertex v1, MVertex v2)
        {
            return Math.Atan2(
                v2.Y - v1.Y,
                v2.X - v1.X
            );
        }
    }
}
