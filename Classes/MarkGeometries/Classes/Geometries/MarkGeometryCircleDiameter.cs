using MSolvLib.MarkGeometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MarkGeometriesLib.Classes.MarkGeometries.Classes.Geometries
{
    public class MarkGeometryCircleDiameter : MarkGeometry
    {
        public override string Name => "CircleDiameterP1";
        public MarkGeometryPoint StartPoint { get; set; }
        public MarkGeometryPoint EndPoint { get; set; }

        public double DiameterP1 { get; set; } = 0;
        public override double Area { get; protected set; } = 0;
        public override double Perimeter { get; protected set; } = 0;
        public MarkGeometryPoint DiameterP2 { get; set; }

        private int _vertexCount = 32;

        public int VertexCount
        {
            get { return _vertexCount; }
            set
            {
                if (value >= 8)
                {
                    _vertexCount = value;
                }
            }
        }


        public MarkGeometryCircleDiameter()
            : base()
        {
            DiameterP1 = 0;
            DiameterP2 = new MarkGeometryPoint();

            Update();
        }


        public MarkGeometryCircleDiameter(double diameterP1)
            : base()
        {
            DiameterP1 = diameterP1;
            DiameterP2 = new MarkGeometryPoint();
            Update();
        }

        public MarkGeometryCircleDiameter(MarkGeometryPoint diameterP2)
            : base()
        {
            DiameterP1 = 0;
            DiameterP2 = diameterP2;

            Update();
        }

        public MarkGeometryCircleDiameter(MarkGeometryPoint diameterP2, double diameterP1)
            : base()
        {
            DiameterP1 = diameterP1;
            DiameterP2 = diameterP2;

            Update();
        }

        /// <summary>
        ///     Constructor using variables explicitly
        /// </summary>
        /// <param name="x">The position of the circle on the x axis</param>
        /// <param name="y">The position of the circle on the y axis</param>
        /// <param name="z">The position of the circle on the z axis</param>
        /// <param name="diameterP1">The diameterP1 of the circle</param>
        public MarkGeometryCircleDiameter(double x, double y, double z, double diameterP1)
            : base()
        {
            DiameterP1 = diameterP1;
            DiameterP2 = new MarkGeometryPoint(x, y, z);

            Update();
        }

        /// <summary>
        ///     The copy constructor
        /// </summary>
        /// <param name="input"></param>
        internal MarkGeometryCircleDiameter(MarkGeometryCircleDiameter input)
            : base(input)
        {
            DiameterP1 = input.DiameterP1 + 0;
            DiameterP2 = (MarkGeometryPoint)input.DiameterP2.Clone();
            VertexCount = input.VertexCount + 0;

            Update();
        }

        public static explicit operator MarkGeometryPoint[](MarkGeometryCircleDiameter circle)
        {
            List<MarkGeometryPoint> points = new List<MarkGeometryPoint>();

            for (int i = 0; i <= circle.VertexCount; i++)
            {
                points.Add(
                    GeometricArithmeticModule.GetPointAtPosition(circle, (double)i / (double)circle.VertexCount)
                );
            }

            return points.ToArray();
        }

        public static explicit operator MarkGeometryLine[](MarkGeometryCircleDiameter circle)
        {
            return GeometricArithmeticModule.SplitGeometry(circle, circle.VertexCount).ToArray();
        }

        public override void Update()
        {
            SetExtents();
            Area = Math.PI * Math.Pow(DiameterP1/2, 2);
            Perimeter = 2 * Math.PI * (DiameterP1/2);
        }

        public override object Clone()
        {
            return new MarkGeometryCircleDiameter(this);
        }

        public override void SetExtents()
        {
            Extents.MinX = DiameterP2.X - DiameterP1;
            Extents.MaxX = DiameterP2.X + DiameterP1;

            Extents.MinY = DiameterP2.Y - DiameterP1;
            Extents.MaxY = DiameterP2.Y + DiameterP1;

            Extents.MinZ = DiameterP2.Z - DiameterP1;
            Extents.MaxZ = DiameterP2.Z + DiameterP1;

            // determine start and end points
            StartPoint = GeometricArithmeticModule.GetPointAtAngle(DiameterP2, DiameterP1, 0);
            EndPoint = GeometricArithmeticModule.GetPointAtAngle(DiameterP2, DiameterP1, 2 * Math.PI);
        }

        public override void Transform(Matrix4x4 transformationMatrixIn)
        {
            DiameterP2.Transform(transformationMatrixIn);
            StartPoint.Transform(transformationMatrixIn);

            DiameterP1 = GeometricArithmeticModule.ABSMeasure(StartPoint, EndPoint);
            Update();
        }

        public override string ToString()
        {
            return $"{{'DiameterP2': {DiameterP2}, 'DiameterP1': {DiameterP1}}}";
        }

        public override netDxf.Entities.EntityObject GetAsDXFEntity()
        {
            return new netDxf.Entities.Circle(
                    DiameterP2.GetAsDXFVector(), DiameterP1
                );
        }

        public override netDxf.Entities.EntityObject GetAsDXFEntity(string layer)
        {
            return new netDxf.Entities.Circle(
                    DiameterP2.GetAsDXFVector(), DiameterP1
                )
            { Layer = new netDxf.Tables.Layer(layer) };
        }

        public override void Draw2D(IMarkGeometryVisualizer2D view, bool shouldShowVertex)
        {
            view.Draw2D(this, shouldShowVertex);
        }
    }
}

    

    

