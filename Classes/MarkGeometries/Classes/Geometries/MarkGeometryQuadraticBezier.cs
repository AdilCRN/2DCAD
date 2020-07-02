using System.Numerics;

namespace MSolvLib.MarkGeometry
{
    public class MarkGeometryQuadraticBezier : MarkGeometryPath
    {
        public override string Name => "QuadraticBezier";

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

        public MarkGeometryPoint StartPoint { get; set; } = new MarkGeometryPoint();
        public MarkGeometryPoint EndPoint { get; set; } = new MarkGeometryPoint();
        public MarkGeometryPoint ControlPoint { get; set; } = new MarkGeometryPoint();

        public MarkGeometryQuadraticBezier()
            : base()
        {
            Update();
        }

        public MarkGeometryQuadraticBezier(MarkGeometryPoint startPointIn, MarkGeometryPoint endPointIn, MarkGeometryPoint controlPointIn)
            : base()
        {
            StartPoint = startPointIn;
            EndPoint = endPointIn;
            ControlPoint = controlPointIn;

            Update();
        }

        public void GenerateView()
        {
            for (int i = 0; i < VertexCount; i++)
            {
                Points.Add(GeometricArithmeticModule.GetPointAtPosition(
                    StartPoint, EndPoint, ControlPoint, i / (double)(VertexCount - 1))
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
            ControlPoint.Transform(transformationMatrixIn);
            EndPoint.Transform(transformationMatrixIn);

            Update();
        }

    }
}
