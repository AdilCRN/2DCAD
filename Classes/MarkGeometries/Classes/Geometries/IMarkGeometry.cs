using MathNet.Numerics.LinearAlgebra;
using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using netDxf.Entities;
using System;
using System.Drawing;

namespace MSolvLib.MarkGeometry
{
    public interface IMarkGeometry : ICloneable
    {
        string Name { get; }
        GeometryExtents<double> Extents { get; set; }
        string LayerName { get; set; }

        double Area { get; }
        double Perimeter { get; }

        Color? Fill { get; set; }
        Color? Stroke { get; set; }
        float Transparency { get; set; }

        void Transform(Matrix<double> transformationMatrixIn);

        void Draw2D(IMarkGeometryVisualizer2D view);
        void Draw2D(IMarkGeometryVisualizer2D view, bool shouldShowVertex);
        void SetExtents();
        void Update();
        void SetFill(Color? colorIn);
        void SetStroke(Color? colorIn);

        EntityObject GetAsDXFEntity();
        EntityObject GetAsDXFEntity(string layerName);
    }
}