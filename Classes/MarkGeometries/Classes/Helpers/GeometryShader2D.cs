using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;

namespace MSolvLib.MarkGeometry.Helpers
{
    public class GeometryShader2D
    {
        public enum OptimisationSetting
        {
            Default = 0,
            Speed = 1,
            HighestQuality = 2
        }

        public float DefaultPointWidth { get; private set; } = 0.2f;
        public float DefaultStrokeWidth { get; private set; } = 0.8f;
        public Pen DefaultStroke { get; private set; } = new Pen(Color.Black, 0.8f);
        public SolidBrush DefaultPointColor { get; private set; } = new SolidBrush(Color.Red);
        public SolidBrush DefaultFillColor { get; private set; } = new SolidBrush(Color.Transparent);
        public SolidBrush DefaultStrokeColor { get; private set; } = new SolidBrush(Color.Black);

        public void UpdateSettings(double pointWidth, double lineWidth, Color point, Color stroke, Color fill)
        {
            DefaultPointWidth = (float)pointWidth;
            DefaultStrokeWidth = (float)lineWidth;
            DefaultPointColor = ToSolidBrush(point);
            DefaultStrokeColor = ToSolidBrush(stroke);
            DefaultFillColor = ToSolidBrush(fill);
            DefaultStroke = new Pen(stroke, DefaultStrokeWidth);
        }

        public void Draw(Bitmap bmp, IMarkGeometry geometry, bool showVertices=false)
        {
            using (var graphics = Graphics.FromImage(bmp))
            {
                Draw(graphics, geometry, showVertices);
            }
        }

        public void Draw(Bitmap bmp, IMarkGeometry geometry, Matrix4x4 transform, bool showVertices = false)
        {
            using (var graphics = Graphics.FromImage(bmp))
            {
                var _geometry = (IMarkGeometry)geometry.Clone();
                _geometry.Transform(transform);

                Draw(graphics, _geometry, showVertices);
            }
        }

        public void Draw(Bitmap bmp, IList<IMarkGeometry> geometries, bool showVertices = false, int startOffset = 0)
        {
            int index = startOffset;
            try
            {
                using (var graphics = Graphics.FromImage(bmp))
                {
                    while (index < geometries.Count)
                    {
                        Draw(graphics, geometries[index++], showVertices);
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                Draw(bmp, geometries, showVertices, index-1);
            }
        }

        public void Draw(Bitmap bmp, IList<IMarkGeometry> geometries, Matrix4x4 transform, bool showVertices = false, int startOffset = 0)
        {
            int index = startOffset;
            try
            {
                using (var graphics = Graphics.FromImage(bmp))
                {
                    while (index < geometries.Count)
                    {
                        geometries[index].Transform(transform);
                        Draw(graphics, geometries[index++], showVertices);
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                Draw(bmp, geometries, transform, showVertices, index-1);
            }
        }

        public void Reset(Bitmap bmp, Color color)
        {
            using (var graphics = Graphics.FromImage(bmp))
            {
                graphics.Clear(color);
            }
        }

        public bool WriteToFile(Bitmap bmp, string fileName, PixelFormat format, float dpiX, float dpiY, OptimisationSetting optimisationSetting)
        {
            EncoderParameters encoderParameters;
            var __clone = bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), format);
            __clone.SetResolution(dpiX, dpiY);

            switch (optimisationSetting)
            {
                case OptimisationSetting.HighestQuality:
                    encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);

                    if (fileName.EndsWith("bmp"))
                    {
                        __clone.Save(fileName, GetEncoderInfo("image/bmp"), encoderParameters);
                    }
                    else if (fileName.EndsWith("tiff") || fileName.EndsWith("tif"))
                    {
                        __clone.Save(fileName, GetEncoderInfo("image/tiff"), encoderParameters);
                    }
                    else if (fileName.EndsWith("jpg") || fileName.EndsWith("jpeg"))
                    {
                        __clone.Save(fileName, GetEncoderInfo("image/jpeg"), encoderParameters);
                    }
                    else if (fileName.EndsWith("png"))
                    {
                        __clone.Save(fileName, GetEncoderInfo("image/png"), encoderParameters);
                    }
                    else if (fileName.EndsWith("gif"))
                    {
                        __clone.Save(fileName, GetEncoderInfo("image/gif"), encoderParameters);
                    }
                    else
                    {
                        return false;
                    }
                    break;

                case OptimisationSetting.Speed:
                case OptimisationSetting.Default:
                default:
                    if (fileName.EndsWith("bmp"))
                    {
                        __clone.Save(fileName, ImageFormat.Bmp);
                    }
                    else if (fileName.EndsWith("tiff") || fileName.EndsWith("tif"))
                    {
                        encoderParameters = new EncoderParameters(2);
                        encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
                        encoderParameters.Param[1] = new EncoderParameter(System.Drawing.Imaging.Encoder.Compression, (long)EncoderValue.CompressionLZW);
                        __clone.Save(fileName, GetEncoderInfo("image/tiff"), encoderParameters);
                    }
                    else if (fileName.EndsWith("jpg") || fileName.EndsWith("jpeg"))
                    {
                        __clone.Save(fileName, ImageFormat.Jpeg);
                    }
                    else if (fileName.EndsWith("png"))
                    {
                        __clone.Save(fileName, ImageFormat.Png);
                    }
                    else if (fileName.EndsWith("gif"))
                    {
                        __clone.Save(fileName, ImageFormat.Gif);
                    }
                    else
                    {
                        return false;
                    }
                    break;
            }

            return true;
        }

        //public BitmapSource ToBitmapSource(Bitmap bmp, System.Windows.Media.PixelFormat pixelFormat)
        //{
        //    var bitmapData = bmp.LockBits(
        //        new Rectangle(0, 0, bmp.Width, bmp.Height),
        //        ImageLockMode.ReadOnly, bmp.PixelFormat
        //    );

        //    var bitmapSource = BitmapSource.Create(
        //        bitmapData.Width, bitmapData.Height,
        //        bmp.HorizontalResolution, bmp.VerticalResolution,
        //        pixelFormat, null,
        //        bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride
        //    );

        //    bmp.UnlockBits(bitmapData);
        //    return bitmapSource;
        //}

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        private void Draw(Graphics graphics, IMarkGeometry geometry, bool showVertices)
        {
            if (geometry is MarkGeometryPoint point)
            {
                var pointRadius = 0.5 * DefaultPointWidth;
                graphics.FillEllipse(
                    point.Fill == null ? DefaultPointColor : ToSolidBrush((Color)point.Fill),
                    (float)(point.X - pointRadius),
                    (float)(point.Y - pointRadius),
                    DefaultPointWidth, DefaultPointWidth
                );
            }
            else if (geometry is MarkGeometryLine line)
            {
                graphics.DrawLine(
                    line.Stroke == null ? DefaultStroke : new Pen((Color)line.Stroke, DefaultStrokeWidth),
                    (float)line.StartPoint.X,
                    (float)line.StartPoint.Y,
                    (float)line.EndPoint.X,
                    (float)line.EndPoint.Y
                );

                if (showVertices)
                {
                    Draw(graphics, line.StartPoint, false);
                    Draw(graphics, line.EndPoint, false);
                }
            }
            else if (geometry is MarkGeometryCircle circle)
            {
                graphics.FillEllipse(
                    circle.Fill == null ? DefaultFillColor : ToSolidBrush((Color)circle.Fill),
                    (float)circle.Extents.MinX,
                    (float)circle.Extents.MinY,
                    (float)(2 * circle.Radius),
                    (float)(2 * circle.Radius)
                );
                graphics.DrawEllipse(
                    circle.Stroke == null ? DefaultStroke : new Pen((Color)circle.Stroke, DefaultStrokeWidth),
                    (float)circle.Extents.MinX,
                    (float)circle.Extents.MinY,
                    (float)(2 * circle.Radius),
                    (float)(2 * circle.Radius)
                );
            }
            else if (geometry is MarkGeometryArc arc)
            {
                Draw(graphics, new MarkGeometryPath(arc), showVertices);

                //Draw(graphics, arc.Extents.Boundary, showVertices);

                //// TODO : Use filled pie for filled arc
                //graphics.DrawArc(
                //    arc.Fill == null ? DefaultStroke : new Pen((Color)arc.Stroke, DefaultStrokeWidth),
                //    (float)arc.Extents.MinX,
                //    (float)arc.Extents.MinY,
                //    (float)(2*arc.Radius),
                //    (float)(2*arc.Radius),
                //    (float)GeometricArithmeticModule.ToDegrees(arc.StartAngle), // convert from radians to degrees
                //    (float)GeometricArithmeticModule.ToDegrees(arc.Angle)
                //);

                //if (showVertices)
                //{
                //    Draw(graphics, arc.StartPoint, false);
                //    Draw(graphics, arc.EndPoint, false);
                //}
            }
            else if (geometry is MarkGeometryPath path)
            {
                var points = ((List<PointF>)path).ToArray();

                if (path.IsClosed)
                    graphics.FillPolygon(
                        path.Fill == null ? DefaultFillColor : ToSolidBrush((Color)path.Fill),
                        points
                    );

                graphics.DrawLines(
                    path.Stroke == null ? DefaultStroke : new Pen((Color)path.Stroke, DefaultStrokeWidth),
                    points
                );

                if (showVertices)
                {
                    for (int i = 0; i < path.Points.Count; i++)
                        Draw(graphics, path.Points[i], false);
                }
            }
            else if (geometry is MarkGeometriesWrapper wrapper)
            {
                int n = GeometricArithmeticModule.Max<int>(new int[] {
                    wrapper.Points.Count,
                    wrapper.Arcs.Count,
                    wrapper.Circles.Count,
                    wrapper.Lines.Count,
                    wrapper.Paths.Count
                });

                for (int i = 0; i < n; i++)
                {
                    if (i < wrapper.Points.Count)
                    {
                        Draw(graphics, wrapper.Points[i], showVertices);
                    }

                    if (i < wrapper.Arcs.Count)
                    {
                        Draw(graphics, wrapper.Arcs[i], showVertices);
                    }

                    if (i < wrapper.Circles.Count)
                    {
                        Draw(graphics, wrapper.Circles[i], showVertices);
                    }

                    if (i < wrapper.Lines.Count)
                    {
                        Draw(graphics, wrapper.Lines[i], showVertices);
                    }

                    if (i < wrapper.Paths.Count)
                    {
                        Draw(graphics, wrapper.Paths[i], showVertices);
                    }
                }
            }
            else if (geometry is MarkGeometryTree tree)
            {
                foreach (var element in tree.Flatten())
                {
                    Draw(graphics, element, showVertices);
                }
            }
        }

        private static SolidBrush ToSolidBrush(Color color)
        {
            return new SolidBrush(color);
        }
    }
}
