using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STLSlicer.Utils
{
    public class STLFacet
    {
        public STLVertex Vmax { get; set; }
        public STLVertex Vmid { get; set; }
        public STLVertex Vmin { get; set; }
        public STLNormals Normals { get; set; }

        public STLEdge EdgeS1 { get; set; }
        public STLEdge EdgeS2 { get; set; }
        public STLEdge EdgeS3 { get; set; }

        public int Smin { get; set; }
        public int Smid { get; set; }
        public int Smax { get; set; }

        public OrientationType OrientationType { get; set; }

        public STLFacet(double[] normalsIn, double[] v0, double[] v1, double[] v2)
        {
            Normals = new STLNormals(normalsIn);

            // sort input by their z positions
            // but retain their counter-clockwise order using Flags

            if (v0[2] >= v1[2] && v0[2] >= v2[2]) // v0 is greater
            {
                Vmax = new STLVertex(v0)
                {
                    Flag = 0
                };

                if (v1[2] <= v2[2]) // v1 is lesser
                {
                    Vmin = new STLVertex(v1)
                    {
                        Flag = 1
                    };

                    Vmid = new STLVertex(v2)
                    {
                        Flag = 2
                    };

                    OrientationType = OrientationType.TypeC;
                }
                else
                {
                    Vmid = new STLVertex(v1)
                    {
                        Flag = 1
                    };

                    Vmin = new STLVertex(v2)
                    {
                        Flag = 2
                    };

                    OrientationType = OrientationType.TypeB;
                }
            }
            else if (v1[2] >= v0[2] && v1[2] >= v2[2]) // v1 is greater
            {
                Vmax = new STLVertex(v1)
                {
                    Flag = 1
                };

                if (v0[2] <= v2[2]) // v0 is lesser
                {
                    Vmin = new STLVertex(v0)
                    {
                        Flag = 0
                    };

                    Vmid = new STLVertex(v2)
                    {
                        Flag = 2
                    };

                    OrientationType = OrientationType.TypeA;
                }
                else
                {
                    Vmid = new STLVertex(v0)
                    {
                        Flag = 0
                    };

                    Vmin = new STLVertex(v2)
                    {
                        Flag = 2
                    };

                    OrientationType = OrientationType.TypeC;
                }
            }
            else if (v2[2] >= v0[2] && v2[2] >= v1[2]) // v2 is greater
            {
                Vmax = new STLVertex(v2)
                {
                    Flag = 2
                };

                if (v0[2] <= v1[2]) // v0 is lesser
                {
                    Vmin = new STLVertex(v0)
                    {
                        Flag = 0
                    };

                    Vmid = new STLVertex(v1)
                    {
                        Flag = 1
                    };

                    OrientationType = OrientationType.TypeD;
                }
                else
                {
                    Vmid = new STLVertex(v0)
                    {
                        Flag = 0
                    };

                    Vmin = new STLVertex(v1)
                    {
                        Flag = 1
                    };

                    OrientationType = OrientationType.TypeA;
                }
            }

            EdgeS1 = new STLEdge
            {
                Start = Vmin,
                End = Vmax
            };

            EdgeS2 = new STLEdge
            {
                Start = Vmin,
                End = Vmid
            };

            EdgeS3 = new STLEdge
            {
                Start = Vmid,
                End = Vmax
            };
        }

        /// <summary>
        /// </summary>
        /// <param name="minSZ">szmin denotes the z coordinate of the 0th slice which can be designed to coincide to the x-y plane of the system</param>
        public void UpdateSliceNumber(double minSZ, double sliceThickness)
        {
            Smin = (int)Math.Ceiling((Vmin.Z - minSZ) / sliceThickness);
            Smid = (int)Math.Ceiling((Vmid.Z - minSZ) / sliceThickness);
            Smax = (int)Math.Ceiling((Vmax.Z - minSZ) / sliceThickness);
        }
    }
}
