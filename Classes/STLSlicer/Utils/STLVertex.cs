using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STLSlicer.Utils
{
    public class STLVertex
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public int Flag { get; set; }

        public STLVertex(double[] positions)
        {
            X = positions[0];
            Y = positions[1];
            Z = positions[2];
        }
    }
}
