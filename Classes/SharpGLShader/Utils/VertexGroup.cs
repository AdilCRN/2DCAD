using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGLShader.Utils
{
    public class VertexGroup
    {
        public double[] Color { get; set; }
        public List<double> Vertices { get; set; }

        public VertexGroup()
        {
            Vertices = new List<double>();
        }

        public VertexGroup(double[] color)
        {
            Color = color;
            Vertices = new List<double>();
        }

        public void Add(double x, double y)
        {
            Vertices.Add(x);
            Vertices.Add(y);
        }
    }
}
