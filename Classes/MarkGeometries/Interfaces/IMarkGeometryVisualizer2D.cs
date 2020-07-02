using System.Collections.Generic;
using System.Drawing;

namespace MSolvLib.MarkGeometry
{
    public interface IMarkGeometryVisualizer2D
    {
        // drawing interfaces
        void Draw2D(MarkGeometryArc arc, bool shouldShowVertex);
        void Draw2D(MarkGeometryCircle circle, bool shouldShowVertex);
        void Draw2D(MarkGeometryLine line, bool shouldShowVertex);
        void Draw2D(MarkGeometryPath path, bool shouldShowVertex);
        void Draw2D(List<MarkGeometryLine> lines, bool shouldShowVertex);
        void Draw2D(MarkGeometryPoint point);
        //void SetZoom(double xAxis, double yAxis);
        //void SetCanvas(Canvas canvas);
        //void SetOffset(double dx, double dy, double dz);
        //void SetOffset(MarkGeometryPoint offset);
        //MarkGeometryPoint GetOffset();
        //void AddOffset(double dx, double dy, double dz);
        //void AddOffset(MarkGeometryPoint offset);
        //void PushOffset();
        //void PopOffset();
        void Clear();

        // color interfaces
        void SetPointSize(double pointSize);
        void SetStrokeWidth(double width);
        void SetPointColor(Color color);
        void SetStrokeColor(Color color);
        void SetFillColor(Color color);
    }
}