using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGLShader.Utils
{
    [Serializable]
    public class MVertex
    {
        public double X { get; set; }
        public double Y { get; set; }

        public MVertex()
        {
            X = 0;
            Y = 0;
        }

        public MVertex(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
