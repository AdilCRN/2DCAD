using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STLSlicer.Utils
{
    public class STLContour
    {
        public STLContourType ContourType { get; set; }
        public List<STLVertex> Vertices { get; set; }
    }
}
