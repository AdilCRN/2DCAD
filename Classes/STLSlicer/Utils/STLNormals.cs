using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STLSlicer.Utils
{
    public class STLNormals
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public STLNormals(double[] normalsIn)
        {
            X = normalsIn[0];
            Y = normalsIn[1];
            Z = normalsIn[2];
        }
    }
}
