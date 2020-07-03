using MSolvLib.MarkGeometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContourHatcher.Utils
{
    public class IntersectionsBinaryTree
    {
        public IntersectionsBinaryTree LeftNode { get; set; }
        public IntersectionsBinaryTree RightNode { get; set; }
        public MarkGeometryPoint Value { get; set; }
        public MarkGeometryPoint Origin { get; set; }

        public IntersectionsBinaryTree(MarkGeometryPoint origin)
        {
            Value = null;
            LeftNode = null;
            RightNode = null;
            Origin = origin;
        }

        public IntersectionsBinaryTree(MarkGeometryPoint origin, MarkGeometryPoint point)
            : this(origin)
        {
            Insert(point);
        }

        public void Insert(MarkGeometryPoint point)
        {
            if (Value == null)
            {
                Value = point;
            }
            else if (IsLesser(Origin, point, Value))
            {
                if (LeftNode == null)
                    LeftNode = new IntersectionsBinaryTree(Origin, point);
                else
                    LeftNode.Insert(point);
            }
            else
            {
                if (RightNode == null)
                    RightNode = new IntersectionsBinaryTree(Origin, point);
                else
                    RightNode.Insert(point);
            }

        }

        public void InsertRange(IntersectionsBinaryTree tree)
        {
            tree.Traverse(
                (point) =>
                {
                    Insert(point);
                }
            );
        }

        public void InsertRange(IEnumerable<MarkGeometryPoint> points)
        {
            foreach (var point in points)
                Insert(point);
        }

        public List<MarkGeometryPoint> ToList()
        {
            var values = new List<MarkGeometryPoint>();

            Traverse((point) =>
            {
                values.Add(point);
            });

            return values;
        }

        private void Traverse(Action<MarkGeometryPoint> callback)
        {
            if (Value == null)
                return;

            LeftNode?.Traverse(callback);
            callback(Value);
            RightNode?.Traverse(callback);
        }

        public static bool IsLesser(MarkGeometryPoint origin, MarkGeometryPoint p1, MarkGeometryPoint p2)
        {
            return GeometricArithmeticModule.ABSMeasure2D(origin, p1) < GeometricArithmeticModule.ABSMeasure2D(origin, p2);
        }
    }
}
