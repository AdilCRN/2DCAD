using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SharpGLShader.Utils
{
    [Serializable]
    public class MDirectionalVertex : MVertex
    {
        public double AngleDeg { get; set; }

        [XmlIgnore]
        public double AngleRad
        {
            get
            {
                return AngleDeg / 180 * Math.PI;
            }
        }

        public MDirectionalVertex()
            : base()
        {
            AngleDeg = 0;
        }

        public MDirectionalVertex(double x, double y, double angle)
            : base(x, y)
        {
            AngleDeg = angle;
        }
    }
}
