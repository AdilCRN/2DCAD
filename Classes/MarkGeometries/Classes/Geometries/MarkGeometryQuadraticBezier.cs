using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;

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
            Lines = new List<MarkGeometryLine>();

            GeometricArithmeticModule.LookAheadStepPositionIterationHelper(VertexCount,
                (current, next) =>
                {
                    Lines.Add(new MarkGeometryLine(
                        GeometricArithmeticModule.GetPointAtPosition(StartPoint, EndPoint, ControlPoint, current),
                        GeometricArithmeticModule.GetPointAtPosition(StartPoint, EndPoint, ControlPoint, next)
                    ));

                    return true;
                }
            );
        }

        public override void Update()
        {
            GenerateView();
            base.Update();
        }

        public override void Transform(Matrix<double> transformationMatrixIn)
        {
            StartPoint.Transform(transformationMatrixIn);
            ControlPoint.Transform(transformationMatrixIn);
            EndPoint.Transform(transformationMatrixIn);

            Update();
        }

    }
}
