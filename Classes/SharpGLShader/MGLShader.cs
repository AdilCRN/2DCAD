using MathNet.Numerics.Distributions;
using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using MSolvLib.MarkGeometry;
using SharpGL;
using SharpGLShader.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGLShader
{
    public class MGLShader
    {
        #region Section: Static Properties

        public static double[] Red = new double[] { 1.0, 0.0, 0.0 };
        public static double[] Green = new double[] { 0.0, 1.0, 0.0 };
        public static double[] Blue = new double[] { 0.0, 0.0, 1.0 };
        public static double[] White = new double[] { 1.0, 1.0, 1.0 };
        public static double[] Yellow = new double[] { 1.0, 1.0, 0.0 };
        public static double[] Cyan = new double[] { 0.0, 1.0, 1.0 };
        public static double[] Violet = new double[] { 1.0, 0.0, 1.0 };

        #endregion

        #region Section: Protected Properties

        // for CAD view
        protected MSize2D _windowSize;
        protected double _aspectRatio;
        protected bool _renderOnNextPass;
        protected bool _isUpdatingMouse = false;

        // GL buffers
        protected List<VertexGroup> _lines;
        protected List<VertexGroup> _points;
        protected List<VertexGroup> _openPolylines;
        protected List<VertexGroup> _closedPolylines;

        // zoom, pan, scale
        protected double _zoom = 0;
        protected double _scale = 1;
        protected double _zoomFactor = 0.2;
        protected MVertex _cadOffset = new MVertex();
        protected MVertex _panOffset = new MVertex();

        // total viewport size is 2.0 (i.e. -1.0 to 1.0)
        // reserve 0.075 for padding
        protected const double _reservedViewPortSize = 2.0 - 0.075;

        protected double _trueScale => _scale * _zoom;
        protected MVertex _trueOffset => new MVertex(
            (_trueScale * _cadOffset.X) + _panOffset.X,
            (_trueScale * _cadOffset.Y) + _panOffset.Y
        );

        #endregion

        #region Section: Public Properties

        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }

        public double Width
        {
            get
            {
                return MaxX - MinX;
            }
        }

        public double Height
        {
            get
            {
                return MaxY - MinY;
            }
        }

        public MVertex Centre
        {
            get
            {
                return new MVertex(
                    MinX + (0.5 * Width),
                    MinY + (0.5 * Height)
                );
            }
        }

        public int Count { get; set; } = 0;

        public MVertex Mouse { get; set; }

        public float DefaultPointSize { get; set; } = 0.3f;
        public float DefaultLineWidth { get; set; } = 1f;
        public double[] DefaultPointColor { get; set; } = Red;
        public double[] DefaultLineColor { get; set; } = Green;

        #endregion

        #region Section: Constructor

        public MGLShader()
            : this(200, 200)
        {
        }

        public MGLShader(MSize2D windowSize)
            : this(windowSize.Width, windowSize.Height)
        {
        }

        public MGLShader(double width, double height)
        {
            // update window size
            _windowSize = new MSize2D(width, height);
            _aspectRatio = _windowSize.Width / _windowSize.Height;

            Reset();
        }

        #endregion

        #region Section: Resetting View

        public virtual void Reset()
        {
            // reset extents
            MinX = double.MaxValue;
            MaxX = double.MinValue;
            MinY = double.MaxValue;
            MaxY = double.MinValue;

            // update counter
            Count = 0;

            // reset buffers with defaults
            _lines = new List<VertexGroup>() { new VertexGroup(DefaultLineColor) };
            _points = new List<VertexGroup>() { new VertexGroup(DefaultPointColor) };
            _openPolylines = new List<VertexGroup>();
            _closedPolylines = new List<VertexGroup>();

            // reset mouse position
            Mouse = new MVertex();

            ResetView();
        }

        public virtual void ResetView()
        {
            _zoom = 1;

            _panOffset.X = 0;
            _panOffset.Y = 0;

            _cadOffset.X = -Centre.X;
            _cadOffset.Y = -Centre.Y;

            Render();
        }

        #endregion

        #region Section: Adding Geometries

        public virtual void AddDefault(IList<IMarkGeometry> geometriesIn)
        {
            if (geometriesIn == null)
                return;

            for (int i = 0; i < geometriesIn.Count; i++)
                AddDefault(geometriesIn[i]);
        }

        public virtual void AddDefault(IList<IMarkGeometry> geometriesIn, double[] color)
        {
            if (geometriesIn == null || color == null || color.Length < 3)
            {
                AddDefault(geometriesIn);
                return;
            }

            if (geometriesIn.All(x => x is MarkGeometryPoint))
            {
                var currentExtents = GeometryExtents<double>.Combine(
                    new GeometryExtents<double>()
                    {
                        MinX = MinX,
                        MaxX = MaxX,
                        MinY = MinY,
                        MaxY = MaxY
                    },
                    GeometricArithmeticModule.CalculateExtents(
                        geometriesIn
                    )
                );

                // Update extents
                MinX = currentExtents.MinX;
                MaxX = currentExtents.MaxX;
                MinY = currentExtents.MinY;
                MaxY = currentExtents.MaxY;

                // Update Counter
                Count += geometriesIn.Count;

                var vtx = new VertexGroup()
                {
                    Color = color
                };

                for (int i = 0; i < geometriesIn.Count; i++)
                    vtx.Vertices.AddRange(ToDouble(geometriesIn[i] as MarkGeometryPoint));

                _points.Add(vtx);
                Update();
            }
            else if (geometriesIn.All(x => x is MarkGeometryLine))
            {
                var currentExtents = GeometryExtents<double>.Combine(
                    new GeometryExtents<double>()
                    {
                        MinX = MinX,
                        MaxX = MaxX,
                        MinY = MinY,
                        MaxY = MaxY
                    },
                    GeometricArithmeticModule.CalculateExtents(
                        geometriesIn
                    )
                );

                // Update extents
                MinX = currentExtents.MinX;
                MaxX = currentExtents.MaxX;
                MinY = currentExtents.MinY;
                MaxY = currentExtents.MaxY;

                // Update Counter
                Count += geometriesIn.Count;

                var vtx = new VertexGroup()
                {
                    Color = color
                };

                for (int i = 0; i < geometriesIn.Count; i++)
                    vtx.Vertices.AddRange(ToDouble(geometriesIn[i] as MarkGeometryLine));

                _lines.Add(vtx);
                Update();
            }
            else
            {
                for (int i = 0; i < geometriesIn.Count; i++)
                    AddDefault(geometriesIn[i], color);
            }
        }

        public virtual void AddDefault(IMarkGeometry geometryIn)
        {
            if (geometryIn == null)
                return;

            // Update extents
            MaxX = Math.Max(geometryIn.Extents.MaxX, MaxX);
            MaxY = Math.Max(geometryIn.Extents.MaxY, MaxY);
            MinX = Math.Min(geometryIn.Extents.MinX, MinX);
            MinY = Math.Min(geometryIn.Extents.MinY, MinY);

            // Update Counter
            Count += 1;

            if (geometryIn is MarkGeometryPoint point)
            {
                // add to default
                _points[0].Add(point.X, point.Y);
            }
            else if (geometryIn is MarkGeometryLine line)
            {
                // add to default
                _lines[0].Add(line.StartPoint.X, line.StartPoint.Y);
                _lines[0].Add(line.EndPoint.X, line.EndPoint.Y);
            }
            else if (geometryIn is MarkGeometryCircle circle)
            {
                var vtx = new VertexGroup(DefaultLineColor);
                for (int i = 0; i <= circle.VertexCount; i++)
                    vtx.Add(
                        (circle.CentrePoint.X + (circle.Radius * Math.Cos(i * Math.PI * 2 / circle.VertexCount))), (circle.CentrePoint.Y + (circle.Radius * Math.Sin(i * Math.PI * 2 / circle.VertexCount)))
                    );
                _closedPolylines.Add(vtx);
            }
            else if (geometryIn is MarkGeometryArc arc)
            {
                var arcPath = new MarkGeometryPath(arc);
                var vtx = new VertexGroup(DefaultLineColor);

                // add points
                for (int i = 0; i < arcPath.Points.Count; i++)
                    vtx.Add(arcPath.Points[i].X, arcPath.Points[i].Y);

                if (arcPath.IsClosed)
                    _closedPolylines.Add(vtx);
                else
                    _openPolylines.Add(vtx);
            }
            else if (geometryIn is MarkGeometryPath path)
            {
                var vtx = new VertexGroup(DefaultLineColor);

                // add points
                for (int i = 0; i < path.Points.Count; i++)
                    vtx.Add(path.Points[i].X, path.Points[i].Y);

                if (path.IsClosed)
                    _closedPolylines.Add(vtx);
                else
                    _openPolylines.Add(vtx);
            }
            else if (geometryIn is IMarkGeometryWrapper wrapper)
            {
                wrapper.BeginGetAll((geometry) =>
                {
                    AddDefault(geometry);
                    return true;
                });
            }

            Update();
        }

        public virtual void AddDefault(IMarkGeometry geometryIn, double[] color)
        {
            if (geometryIn == null || color == null || color.Length < 3)
            {
                AddDefault(geometryIn);
                return;
            }

            // Update extents
            MaxX = Math.Max(geometryIn.Extents.MaxX, MaxX);
            MaxY = Math.Max(geometryIn.Extents.MaxY, MaxY);
            MinX = Math.Min(geometryIn.Extents.MinX, MinX);
            MinY = Math.Min(geometryIn.Extents.MinY, MinY);

            // Update Counter
            Count += 1;

            if (geometryIn is MarkGeometryPoint point)
            {
                _points.Add(new VertexGroup()
                {
                    Color = color,
                    Vertices = new List<double>() { point.X, point.Y }
                });
            }
            else if (geometryIn is MarkGeometryLine line)
            {
                // add to default
                _lines.Add(new VertexGroup()
                {
                    Color = color,
                    Vertices = new List<double>() {
                        line.StartPoint.X, line.StartPoint.Y,
                        line.EndPoint.X, line.EndPoint.Y
                    }
                });
            }
            else if (geometryIn is MarkGeometryCircle circle)
            {
                var vtx = new VertexGroup(color);
                for (int i = 0; i <= circle.VertexCount; i++)
                    vtx.Add(
                        (circle.CentrePoint.X + (circle.Radius * Math.Cos(i * Math.PI * 2 / circle.VertexCount))), (circle.CentrePoint.Y + (circle.Radius * Math.Sin(i * Math.PI * 2 / circle.VertexCount)))
                    );
                _closedPolylines.Add(vtx);
            }
            else if (geometryIn is MarkGeometryArc arc)
            {
                var arcPath = new MarkGeometryPath(arc);
                var vtx = new VertexGroup(color);

                // add points
                for (int i = 0; i < arcPath.Points.Count; i++)
                    vtx.Add(arcPath.Points[i].X, arcPath.Points[i].Y);

                if (arcPath.IsClosed)
                    _closedPolylines.Add(vtx);
                else
                    _openPolylines.Add(vtx);
            }
            else if (geometryIn is MarkGeometryPath path)
            {
                var vtx = new VertexGroup(color);

                // add points
                for (int i = 0; i < path.Points.Count; i++)
                    vtx.Add(path.Points[i].X, path.Points[i].Y);

                if (path.IsClosed)
                    _closedPolylines.Add(vtx);
                else
                    _openPolylines.Add(vtx);
            }
            else if (geometryIn is IMarkGeometryWrapper wrapper)
            {
                wrapper.BeginGetAll((geometry) =>
                {
                    AddDefault(geometry, color);
                    return true;
                });
            }

            Update();
        }

        #endregion

        #region Section: Rendering

        protected virtual void Update()
        {
            // total viewport size is 2.0 (i.e. -1.0 to 1.0)
            // reserve 0.15 for padding
            // update CAD offsets and scale
            _cadOffset.X = -Centre.X;
            _cadOffset.Y = -Centre.Y;

            double size = Math.Max(Width, Height);
            _scale = _reservedViewPortSize / size;

            _zoomFactor = 0.1 * size;
        }

        public virtual void Render()
        {
            _renderOnNextPass = true;
        }

        public virtual void Render(OpenGL gl)
        {
            if (!_renderOnNextPass)
                return;

            try
            {
                //  clear the color buffer.
                gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

                // reset the modelview matrix
                gl.LoadIdentity();

                // apply CAD offset
                gl.Translate(_trueScale * _cadOffset.X, _trueScale * _cadOffset.Y, 0);

                // apply view scale about offset
                gl.Scale(_trueScale, _trueScale, 1);

                // apply PAN offset
                gl.Translate(_panOffset.X / _trueScale, _panOffset.Y / _trueScale, 0);

                // set sizes
                gl.PointSize((float)(_trueScale * DefaultPointSize));
                gl.LineWidth((float)(_trueScale * DefaultLineWidth));

                // draw contents in buffer
                Draw(gl);

                // render
                gl.Flush();
            }
            finally
            {
                _renderOnNextPass = false;
            }
        }

        protected void Draw(OpenGL gl)
        {
            // draw contents in buffer
            Draw(gl, OpenGL.GL_POINTS, _points);
            for (int i = 0; i < _openPolylines.Count; i++)
                Draw(gl, OpenGL.GL_LINE_STRIP, _openPolylines[i]?.Vertices, _openPolylines[i]?.Color);
            for (int i = 0; i < _closedPolylines.Count; i++)
                Draw(gl, OpenGL.GL_LINE_LOOP, _closedPolylines[i]?.Vertices, _closedPolylines[i]?.Color);
            Draw(gl, OpenGL.GL_LINES, _lines);
        }

        protected void Draw(OpenGL gl, double[] color)
        {
            // draw contents in buffer
            Draw(gl, OpenGL.GL_POINTS, _points, color);
            for (int i = 0; i < _openPolylines.Count; i++)
                Draw(gl, OpenGL.GL_LINE_STRIP, _openPolylines[i]?.Vertices, _openPolylines[i]?.Color == null ? color : _openPolylines[i].Color);
            for (int i = 0; i < _closedPolylines.Count; i++)
                Draw(gl, OpenGL.GL_LINE_LOOP, _closedPolylines[i]?.Vertices, _closedPolylines[i]?.Color == null ? color : _closedPolylines[i].Color);
            Draw(gl, OpenGL.GL_LINES, _lines, color);
        }

        protected static void Draw(OpenGL gl, uint mode, IList<VertexGroup> vertexGroups)
        {
            if (vertexGroups == null)
                return;

            gl.Begin(mode);
            for (int i = 0; i < vertexGroups.Count; i++)
            {
                if (vertexGroups[i] == null)
                    continue;

                if (vertexGroups[i].Color != null)
                {
                    if (vertexGroups[i].Color.Length >= 3)
                        gl.Color(vertexGroups[i].Color[0], vertexGroups[i].Color[1], vertexGroups[i].Color[2]);
                    else if (vertexGroups[i].Color.Length >= 4)
                        gl.Color(vertexGroups[i].Color[0], vertexGroups[i].Color[1], vertexGroups[i].Color[2], vertexGroups[i].Color[3]);
                }

                for (int j = 0; j < vertexGroups[i].Vertices.Count; j += 2)
                    gl.Vertex(vertexGroups[i].Vertices[j], vertexGroups[i].Vertices[j + 1]);
            }
            gl.End();
        }

        protected static void DrawArray(OpenGL gl, uint mode, double[] points, double[] color)
        {
            if (color != null)
            {
                if (color.Length >= 3)
                    gl.Color(color[0], color[1], color[2]);
                else if (color.Length >= 4)
                    gl.Color(color[0], color[1], color[2], color[3]);
            }

            gl.VertexPointer(2, 0, points);
            gl.EnableClientState(OpenGL.GL_VERTEX_ARRAY);
            gl.DrawArrays(mode, 0, points.Length);
        }

        protected static void Draw(OpenGL gl, uint mode, IList<VertexGroup> vertexGroups, double[] color)
        {
            if (vertexGroups == null)
                return;

            gl.Begin(mode);
            for (int i = 0; i < vertexGroups.Count; i++)
            {
                if (vertexGroups[i].Color != null)
                {
                    if (vertexGroups[i].Color.Length >= 3)
                        gl.Color(vertexGroups[i].Color[0], vertexGroups[i].Color[1], vertexGroups[i].Color[2]);
                    else if (vertexGroups[i].Color.Length >= 4)
                        gl.Color(vertexGroups[i].Color[0], vertexGroups[i].Color[1], vertexGroups[i].Color[2], vertexGroups[i].Color[3]);
                }
                else if (color != null)
                {
                    if (color.Length >= 3)
                        gl.Color(color[0], color[1], color[2]);
                    else if (color.Length >= 4)
                        gl.Color(color[0], color[1], color[2], color[3]);
                }

                for (int j = 0; j < vertexGroups[i].Vertices.Count; j += 2)
                    gl.Vertex(vertexGroups[i].Vertices[j], vertexGroups[i].Vertices[j + 1]);
            }
            gl.End();
        }

        protected static void Draw(OpenGL gl, uint mode, IList<double> vertices, double[] color)
        {
            if (vertices == null)
                return;

            gl.Begin(mode);

            if (color != null)
            {
                if (color.Length >= 3)
                    gl.Color(color[0], color[1], color[2]);
                else if (color.Length >= 4)
                    gl.Color(color[0], color[1], color[2], color[3]);
            }

            for (int i = 0; i < vertices.Count; i += 2)
            {
                gl.Vertex(vertices[i], vertices[i + 1]);
            }

            gl.End();
        }

        #endregion

        #region Section: Callbacks and Interrupts

        public virtual void OnInitialised(OpenGL gl)
        {
            //  enable the OpenGL depth testing functionality.
            gl.Enable(OpenGL.GL_DEPTH_TEST);

            // set clear color to black and opaque
            gl.ClearColor(0, 0, 0, 1.0f);

            ResetView();
        }

        public virtual void OnResize(OpenGL gl, double windowWidth, double windowHeight)
        {
            _windowSize.Width = windowWidth;
            _windowSize.Height = windowHeight == 0 ? 1 : windowHeight;
            _aspectRatio = _windowSize.Width / _windowSize.Height;

            // set viewport to cover the new window
            gl.Viewport(0, 0, (int)Math.Round(_windowSize.Width), (int)Math.Round(_windowSize.Height));

            // set aspect ratio to match the viewport
            // Load and clear the projection matrix.
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            gl.LoadIdentity(); // reset the projection matrix
            if (_windowSize.Width >= _windowSize.Height)
            {
                // aspect >= 1, set the height from -1 to 1, with larger width
                gl.Ortho2D(-1.0 * _aspectRatio, 1.0 * _aspectRatio, -1.0, 1.0);
            }
            else
            {
                // aspect < 1, set the width to -1 to 1, with larger height
                gl.Ortho2D(-1.0, 1.0, -1.0 / _aspectRatio, 1.0 / _aspectRatio);
            }

            // Load the modelview.
            gl.MatrixMode(OpenGL.GL_MODELVIEW);

            // render
            Render();
        }


        #endregion

        #region Section: Mouse, Pan, Zoom and Focus

        public virtual void ZoomToFit(MVertex centrePoint, double width, double height)
        {
            double newScale = _reservedViewPortSize / Math.Max(width, height);

            _panOffset.X = -newScale * (centrePoint.X + _cadOffset.X);
            _panOffset.Y = -newScale * (centrePoint.Y + _cadOffset.Y);
            _zoom = newScale / _scale;

            Render();
        }

        public virtual void Pan(double xDelta, double yDelta)
        {
            _panOffset.X += xDelta;
            _panOffset.Y += yDelta;
            Render();
        }

        public virtual void Zoom(double delta = 0)
        {
            double tmpZoom = _zoom;
            _zoom = GeometricArithmeticModule.Constrain(
                _zoom + ((delta > 0 ? _zoomFactor : -_zoomFactor) * _trueScale),
                0.001,
                1000
            );

            double dZoom = _zoom - tmpZoom;

            _panOffset.X -= (Mouse.X + _cadOffset.X) * _scale * dZoom;
            _panOffset.Y -= (Mouse.Y + _cadOffset.Y) * _scale * dZoom;

            Render();
        }

        public virtual void UpdateMouse(double xNorm, double yNorm)
        {
            if (_isUpdatingMouse)
                return;

            try
            {
                _isUpdatingMouse = true;

                double antiScale = 1.0 / _trueScale;

                if (_aspectRatio >= 1)
                {
                    Mouse.X = antiScale * (MapF(xNorm, 0, _aspectRatio, -_aspectRatio, _aspectRatio) - _trueOffset.X);
                    Mouse.Y = antiScale * (MapF(yNorm, 0, 1, 1, -1) - _trueOffset.Y);
                }
                else
                {
                    double invAspectRation = 1.0 / _aspectRatio;
                    Mouse.X = antiScale * (MapF(xNorm, 0, 1, -1, 1) - _trueOffset.X);
                    Mouse.Y = antiScale * (MapF(yNorm, 0, invAspectRation, invAspectRation, -invAspectRation) - _trueOffset.Y);
                }
            }
            finally
            {
                _isUpdatingMouse = false;
            }
        }

        #endregion

        #region Section: Static Helpers

        protected static double[] ToDouble(MarkGeometryPoint point)
        {
            return new double[] { point.X, point.Y };
        }

        protected static double[] ToDouble(MarkGeometryLine line)
        {
            return new double[] { line.StartPoint.X, line.StartPoint.Y, line.EndPoint.X, line.EndPoint.Y };
        }

        protected static List<MVertex> ToVertexes(MarkGeometryCircle circle)
        {
            var vtx = new List<MVertex>(circle.VertexCount + 1);
            for (int i = 0; i <= circle.VertexCount; i++)
                vtx.Add(new MVertex(
                    (circle.CentrePoint.X + (circle.Radius * Math.Cos(i * Math.PI * 2 / circle.VertexCount))), (circle.CentrePoint.Y + (circle.Radius * Math.Sin(i * Math.PI * 2 / circle.VertexCount)))
                ));
            return vtx;
        }

        protected static double MapF(double val, double inMin, double inMax, double outMin, double outMax)
        {
            return (((val - inMin) / (inMax - inMin)) * (outMax - outMin)) + outMin;
        }

        #endregion
    }
}
