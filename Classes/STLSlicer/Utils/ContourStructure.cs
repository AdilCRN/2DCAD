using MSolvLib.MarkGeometry;
using SharpGLShader.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STLSlicer.Utils
{
    public class ContourStructure//: IEnumerable<MVertex>
    {
        public LinkedList<IntersectionStructure> IntersectionList { get; set; }

        public ContourStructure(IntersectionStructure intersectionStructureIn)
        {
            IntersectionList = new LinkedList<IntersectionStructure>();
            IntersectionList.AddLast(intersectionStructureIn);
        }

        public IEnumerable<MVertex> ToVertices()
        {
            if (MSTLSlicer.CompareEqual(IntersectionList.First.Value.ForwardEdge, IntersectionList.Last.Value.BackwardEdge))
                return IntersectionList.Select(ill => ill.ForwardEdgeIntersectionPoint).Append(IntersectionList.First.Value.ForwardEdgeIntersectionPoint);

            return IntersectionList.Select(ill => ill.ForwardEdgeIntersectionPoint);
        }
    }
}
