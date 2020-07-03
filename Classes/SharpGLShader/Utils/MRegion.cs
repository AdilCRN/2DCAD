using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGLShader.Utils
{
    public class MRegion
    {
        public MVertex StartVertex { get; set; }
        public MVertex EndVertex { get; set; }
        public MRegion(MVertex start, MVertex end)
        {
            StartVertex = start;
            EndVertex = end;
        }
    }

}
