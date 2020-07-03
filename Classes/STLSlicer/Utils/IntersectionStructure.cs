using SharpGLShader.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STLSlicer.Utils
{
    public class IntersectionStructure
    {
        public double SlicePosition { get; set; }
        public MVertex ForwardEdgeIntersectionPoint { get; set; }
        public MVertex BackwardEdgeIntersectionPoint => MSTLSlicer.GetIntersection(BackwardEdge, SlicePosition);

        public STLEdge ForwardEdge { get; set; } // pointer to e1 (forward) and e2 (backward) edges
        public STLEdge BackwardEdge { get; set; } // pointer to e1 (forward) and e2 (backward) edges

        public static LinkedList<IntersectionStructure> Create(MVertex intersectionPoint, STLEdge forwardEdge, STLEdge backwardEdge)
        {
            var iis = new LinkedList<IntersectionStructure>();
            iis.AddLast(
                new IntersectionStructure()
                {
                    ForwardEdgeIntersectionPoint = intersectionPoint,
                    ForwardEdge = forwardEdge,
                    BackwardEdge = backwardEdge
                }
            );

            return iis;
        }

        public static LinkedList<IntersectionStructure> Create(IntersectionStructure intersection)
        {
            return Create(
                intersection.ForwardEdgeIntersectionPoint,
                intersection.ForwardEdge,
                intersection.BackwardEdge
            );
        }
    }
}
