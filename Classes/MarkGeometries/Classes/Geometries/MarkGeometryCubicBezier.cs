using MathNet.Numerics.LinearAlgebra;
using System.Collections.Generic;

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

        public MarkGeometryPoint StartPoint { get; set; } = new MarkGeometryPoint();
        public MarkGeometryPoint EndPoint { get; set; } = new MarkGeometryPoint();
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
            Lines = new List<MarkGeometryLine>();

            GeometricArithmeticModule.LookAheadStepPositionIterationHelper(VertexCount,
                (current, next) =>
                {
                    Lines.Add(new MarkGeometryLine(
                        GeometricArithmeticModule.GetPointAtPosition(StartPoint, EndPoint, ControlPoint_1, ControlPoint_2, current),
                        GeometricArithmeticModule.GetPointAtPosition(StartPoint, EndPoint, ControlPoint_1, ControlPoint_2, next)
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
            ControlPoint_1.Transform(transformationMatrixIn);
            ControlPoint_2.Transform(transformationMatrixIn);
            EndPoint.Transform(transformationMatrixIn);

            Update();
        }
    }
}
