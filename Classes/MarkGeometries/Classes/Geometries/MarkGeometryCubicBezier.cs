using System.Numerics;

namespace MSolvLib.MarkGeometry
{
    public class MarkGeometryCubicBezier : MarkGeometryPath
    {
        public override string Name => "CubicBezier";

        private int _vertexCount = 8;

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

        public new MarkGeometryPoint StartPoint { get; set; }
        public new MarkGeometryPoint EndPoint { get; set; }

        public MarkGeometryPoint ControlPoint_1 { get; set; } = new MarkGeometryPoint();
        public MarkGeometryPoint ControlPoint_2 { get; set; } = new MarkGeometryPoint();

        public MarkGeometryCubicBezier()
            : base()
        {
            Update();
        }

        public MarkGeometryCubicBezier(MarkGeometryPoint startPointIn, MarkGeometryPoint endPointIn, MarkGeometryPoint controlPoint_1In, MarkGeometryPoint controlPoint_2In)
            : base()
        {
            StartPoint = startPointIn;
            EndPoint = endPointIn;
            ControlPoint_1 = controlPoint_1In;
            ControlPoint_2 = controlPoint_2In;

            Update();
        }

        public void GenerateView()
        {
            for(int i=0; i<VertexCount; i++)
            {
                Points.Add(GeometricArithmeticModule.GetPointAtPosition(
                    StartPoint, EndPoint, ControlPoint_1, ControlPoint_2, i / (double)(VertexCount-1))
                );
            }
        }

        public override void Update()
        {
            GenerateView();
            base.Update();
        }

        public override void Transform(Matrix4x4 transformationMatrixIn)
        {
            StartPoint.Transform(transformationMatrixIn);
            ControlPoint_1.Transform(transformationMatrixIn);
            ControlPoint_2.Transform(transformationMatrixIn);
            EndPoint.Transform(transformationMatrixIn);

            Update();
        }
    }
}
