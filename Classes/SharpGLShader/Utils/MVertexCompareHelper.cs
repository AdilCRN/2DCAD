using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGLShader.Utils
{
    public class MVertexCompareHelper : IComparer<MVertex>
    {
        public MVertex Origin { get; set; }

        public MVertexCompareHelper(MVertex origin)
        {
            Origin = origin;
        }

        public int Compare(MVertex v1, MVertex v2)
        {
            double delta = Measure(Origin, v1) - Measure(Origin, v2);
            
            if (Math.Abs(delta) < 0.0001)
            {
                return 0;
            }
            
            return delta < 0 ? -1 : 1;
        }

        public double Measure(MVertex v1, MVertex v2)
        {
            return Math.Sqrt(Math.Pow(v2.X - v1.X, 2) + Math.Pow(v2.Y - v1.Y, 2));
        }
    }
}
