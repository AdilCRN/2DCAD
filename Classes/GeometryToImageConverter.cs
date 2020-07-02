using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using MSolvLib.MarkGeometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace MarkGeometriesLib.Classes
{
    public static class GeometryToImageConverter
    {
        public enum OptimisationSetting
        {
            Default = 0,
            Speed = 1,
            HighestQuality = 2,
            Compressed = 3
        }

        public enum GIColor
        {
            Black = 0,
            White = 1
        }

        private static void DrawPath(Image<Gray, Byte> image, MarkGeometryPath path, Gray fillColor, int thickness, bool shouldFill=true)
        {
            var points = new List<Point>();

            foreach (var point in (MarkGeometryPoint[])path)
            {
                points.Add(point);
            }

            if (shouldFill && path.IsClosed)
            {
                image.Draw(points.ToArray(), fillColor, -1);
            }
            else
            {
                image.DrawPolyline(points.ToArray(), false, fillColor, thickness, LineType.Filled);
            }
        }

        private static void FastCopy(Image<Gray, Byte> image, Bitmap bitmapIn)
        {
            if (bitmapIn.PixelFormat == PixelFormat.Format1bppIndexed)
            {
                FastCopy1Bpp(image, bitmapIn);
            }
            else if (bitmapIn.PixelFormat == PixelFormat.Format4bppIndexed)
            {
                FastCopy4Bpp(image, bitmapIn);
            }
            else
            {
                throw new Exception("Pixel Format is not supported");
            }
        }

        private static void FastCopy1Bpp(Image<Gray, Byte> image, Bitmap bitmapIn)
        {
            var imgMIpl = image.MIplImage;
            long imgStride = imgMIpl.WidthStep;

            var bmpImageData = bitmapIn.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, bitmapIn.PixelFormat);
            var bmpStride = bmpImageData.Stride;

            long pxIndex = 0;
            int _bmpSW = bitmapIn.Width / 8;

            unsafe
            {
                byte* bmpPtr = (byte*)bmpImageData.Scan0.ToPointer();
                byte* imgPtr = (byte*)imgMIpl.ImageData.ToPointer();

                for (long row = 0; row < bitmapIn.Height; row++)
                {
                    for (long col = 0; col <= _bmpSW; col++)
                    {
                        pxIndex = (row * imgStride) + (col * 8);
                        bmpPtr[(row * bmpStride) + col] = compress(imgPtr[pxIndex], imgPtr[pxIndex + 1], imgPtr[pxIndex + 2], imgPtr[pxIndex + 3], imgPtr[pxIndex + 4], imgPtr[pxIndex + 5], imgPtr[pxIndex + 6], imgPtr[pxIndex + 7]);
                    }
                }
            }

            bitmapIn.UnlockBits(bmpImageData);
        }

        private static void FastCopy4Bpp(Image<Gray, Byte> image, Bitmap bitmapIn)
        {
            var imgMIpl = image.MIplImage;
            long imgStride = imgMIpl.WidthStep;

            var bmpImageData = bitmapIn.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, bitmapIn.PixelFormat);
            var bmpStride = bmpImageData.Stride;

            long pxIndex = 0;
            int _bmpSW = bitmapIn.Width / 2;

            unsafe
            {
                byte* bmpPtr = (byte*)bmpImageData.Scan0.ToPointer();
                byte* imgPtr = (byte*)imgMIpl.ImageData.ToPointer();

                for (long row = 0; row < bitmapIn.Height; row++)
                {
                    for (long col = 0; col <= _bmpSW; col++)
                    {
                        pxIndex = (row * imgStride) + (col * 2);
                        bmpPtr[(row * bmpStride) + col] = compress(imgPtr[pxIndex], imgPtr[pxIndex + 1]);
                    }
                }
            }

            bitmapIn.UnlockBits(bmpImageData);
        }

        private static byte compress(int c1, int c2)
        {
            const byte mask = 0x0f;
            return (byte)(((c1 & mask) << 4) | (c2 & mask));
        }

        private static byte compress(int c1, int c2, int c3, int c4, int c5, int c6, int c7, int c8)
        {
            const byte mask = 0x01;
            return (byte)(((c1 & mask) << 7) | ((c2 & mask) << 6) | ((c3 & mask) << 5) | ((c4 & mask) << 4) | ((c5 & mask) << 3) | ((c6 & mask) << 2) | ((c7 & mask) << 1) | (c8 & mask));
        }

        public static bool SaveAsBitmap(string fileName, Bitmap bitmapIn, double dpiX, double dpiY, OptimisationSetting optimisationIn, long compressionQuality = 100L)
        {
            EncoderParameters encoderParameters;
            bitmapIn.SetResolution((float)dpiX, (float)dpiY);

            switch (optimisationIn)
            {
                case OptimisationSetting.HighestQuality:
                    encoderParameters = new EncoderParameters(2);
                    encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, compressionQuality);
                    encoderParameters.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionNone);

                    if (fileName.EndsWith("bmp"))
                    {
                        bitmapIn.Save(fileName, GetEncoderInfo("image/bmp"), encoderParameters);
                    }
                    else if (fileName.EndsWith("tiff") || fileName.EndsWith("tif"))
                    {
                        bitmapIn.Save(fileName, GetEncoderInfo("image/tiff"), encoderParameters);
                    }
                    else if (fileName.EndsWith("jpg") || fileName.EndsWith("jpeg"))
                    {
                        bitmapIn.Save(fileName, GetEncoderInfo("image/jpeg"), encoderParameters);
                    }
                    else if (fileName.EndsWith("png"))
                    {
                        bitmapIn.Save(fileName, GetEncoderInfo("image/png"), encoderParameters);
                    }
                    else if (fileName.EndsWith("gif"))
                    {
                        bitmapIn.Save(fileName, GetEncoderInfo("image/gif"), encoderParameters);
                    }
                    else
                    {
                        return false;
                    }
                    break;

                case OptimisationSetting.Compressed:
                case OptimisationSetting.Speed:
                case OptimisationSetting.Default:
                default:
                    if (fileName.EndsWith("bmp"))
                    {
                        bitmapIn.Save(fileName, ImageFormat.Bmp);
                    }
                    else if (fileName.EndsWith("tiff") || fileName.EndsWith("tif"))
                    {
                        encoderParameters = new EncoderParameters(2);
                        encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, compressionQuality);
                        encoderParameters.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);
                        bitmapIn.Save(fileName, GetEncoderInfo("image/tiff"), encoderParameters);
                    }
                    else if (fileName.EndsWith("jpg") || fileName.EndsWith("jpeg"))
                    {
                        bitmapIn.Save(fileName, ImageFormat.Jpeg);
                    }
                    else if (fileName.EndsWith("png"))
                    {
                        bitmapIn.Save(fileName, ImageFormat.Png);
                    }
                    else if (fileName.EndsWith("gif"))
                    {
                        bitmapIn.Save(fileName, ImageFormat.Gif);
                    }
                    else
                    {
                        return false;
                    }
                    break;
            }

            return true;
        }

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

        public static (bool success, int width, int height) To1BppImage(string dxfFilePath, string imageFilePath, double dpiX = 120, double dpiY = 120, double pixelSize = 96, double angle = 0, PixelFormat pixelFormat = PixelFormat.Format1bppIndexed, OptimisationSetting optimisationSetting = OptimisationSetting.Speed, GIColor bgColor = GIColor.White, GIColor fgColor = GIColor.Black, double pointSize = 0.2, double lineWidth = 0.2)
        {
            var geometriesIn = GeometricArithmeticModule.ExtractGeometriesFromDXF(dxfFilePath);
            return To1BppImage(geometriesIn, imageFilePath, dpiX, dpiY, pixelSize, angle, pixelFormat, optimisationSetting, bgColor, fgColor, pointSize, lineWidth);
        }

        public static (bool success, int width, int height) To1BppImage(List<IMarkGeometry> geometriesIn, string imageFilePath, double dpiX = 120, double dpiY = 120, double pixelSize = 96, double angle = 0, PixelFormat pixelFormat = PixelFormat.Format1bppIndexed, OptimisationSetting optimisationSetting = OptimisationSetting.HighestQuality, GIColor bgColor = GIColor.White, GIColor fgColor = GIColor.Black, double pointSize = 0.2, double lineWidth = 0.2)
        {
            var scaleFactorX = (dpiX / pixelSize);
            var scaleFactorY = (dpiY / pixelSize);
            var scale = 0.5 * (scaleFactorX + scaleFactorY);
            var geometriesInExtents = GeometricArithmeticModule.CalculateExtents(geometriesIn.ToArray());

            angle = GeometricArithmeticModule.ToRadians(angle);
            foreach (var geometry in geometriesIn)
            {
                GeometricArithmeticModule.Rotate(geometry, 0, 0, angle, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z);
            }

            var geometries = GeometricArithmeticModule.AlignTopLeftToOrigin(geometriesIn.ConvertAll(g => GeometricArithmeticModule.Scale(((IMarkGeometry)g.Clone()), scaleFactorX, -scaleFactorY, 1, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z)).ToArray());
            var extents = GeometricArithmeticModule.CalculateExtents(geometries);

            int imageWidth = (int)Math.Ceiling(extents.Width);
            int imageHeight = (int)Math.Ceiling(extents.Height);

            var image = new Image<Gray, Byte>(imageWidth, imageHeight);
            var bitmap = new Bitmap(imageWidth, imageHeight, pixelFormat);

            var __bgColor = new Gray(bgColor == GIColor.Black ? 0 : 255);
            var __fillColor = new Gray(fgColor == GIColor.Black ? 0 : 255);

            // Draw background
            image.SetValue(__bgColor);

            int thickness = (int)(scale * lineWidth);
            thickness = thickness <= 0 ? 2 : thickness;

            int pointThickness = (int)(scale * pointSize);
            pointThickness = pointThickness <= 0 ? 2 : pointThickness;

            foreach (var geometry in geometries)
            {
                if (geometry is MarkGeometryPoint point)
                {
                    image.Draw(new CircleF(point, pointThickness), __fillColor, -1);
                }
                else if (geometry is MarkGeometryLine line)
                {
                    image.Draw(line, __fillColor, thickness);
                }
                else if (geometry is MarkGeometryCircle circle)
                {
                    image.Draw(new CircleF(new PointF((float)circle.CentrePoint.X, (float)circle.CentrePoint.Y), (float)(circle.Radius)), __fillColor, -1, LineType.Filled);
                }
                else if (geometry is MarkGeometryArc arc)
                {
                    DrawPath(image, new MarkGeometryPath(arc), __fillColor, thickness);
                }
                else if (geometry is MarkGeometryPath path)
                {
                    DrawPath(image, path, __fillColor, thickness);
                }
            }

            #region Section: Save as Uncompressed - takes a longer time
            //var tiffWriter = new TiffWriter<Gray, Byte>(outImageName);
            //tiffWriter.WriteImage(image); 
            #endregion

            #region Section: Save as Losslessly compressed - takes a shorter time

            //image.Save(outImageName);

            #endregion

            FastCopy(image, bitmap);
            SaveAsBitmap(imageFilePath, bitmap, dpiX, dpiY, optimisationSetting);

            return (true, imageWidth, imageHeight);
        }

        public static (bool success, int width, int height) To1BppImageComplex(string dxfFilePath, string imageFilePath, double dpiX = 120, double dpiY = 120, double pixelSize = 96, double angle = 0, PixelFormat pixelFormat = PixelFormat.Format1bppIndexed, OptimisationSetting optimisationSetting = OptimisationSetting.Speed, GIColor bgColor = GIColor.White, GIColor fgColor = GIColor.Black, double pointSize = 0.2, double lineWidth = 0.2)
        {
            var geometriesIn = GeometricArithmeticModule.ExtractGeometriesFromDXF(dxfFilePath);

            var scaleFactorX = (dpiX / pixelSize);
            var scaleFactorY = (dpiY / pixelSize);
            var scale = 0.5 * (scaleFactorX + scaleFactorY);
            var geometriesInExtents = GeometricArithmeticModule.CalculateExtents(geometriesIn);

            angle = GeometricArithmeticModule.ToRadians(angle);
            foreach (var geometry in geometriesIn)
            {
                GeometricArithmeticModule.Rotate(geometry, 0, 0, angle, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z);
            }

            var geometries = GeometricArithmeticModule.AlignTopLeftToOrigin(geometriesIn.ConvertAll(g => GeometricArithmeticModule.Scale(((IMarkGeometry)g.Clone()), scaleFactorX, scaleFactorY, 1, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z)).ToArray());
            var extents = GeometricArithmeticModule.CalculateExtents(geometries);

            int imageWidth = (int)Math.Ceiling(extents.Width);
            int imageHeight = (int)Math.Ceiling(extents.Height);

            var image = new Image<Gray, Byte>(imageWidth, imageHeight);
            var bitmap = new Bitmap(imageWidth, imageHeight, pixelFormat);

            var __bgColor = bgColor == GIColor.Black ? Color.Black : Color.White;
            var __fillColor = fgColor == GIColor.Black ? Color.Black : Color.White;

            // Draw background
            image.SetValue(ToGray(__bgColor));

            var items = MarkGeometryTree.FromGeometries(new List<IMarkGeometry>(geometries), bgColor == GIColor.Black ? Color.Black : Color.White, fgColor == GIColor.Black ? Color.Black : Color.White);

            foreach(var item in items)
            {
                if (item is MarkGeometryTree tree)
                {
                    tree.BeginGetAll((geometry) =>
                    {
                        DrawGeometry(image, geometry, scale, pointSize, lineWidth, __bgColor, __fillColor);
                        return true;
                    });
                }
                else
                {
                    DrawGeometry(image, item, scale, pointSize, lineWidth, __bgColor, __fillColor);
                }
            }

            #region Section: Save as Uncompressed - takes a longer time
            //var tiffWriter = new TiffWriter<Gray, Byte>(outImageName);
            //tiffWriter.WriteImage(image); 
            #endregion

            #region Section: Save as Losslessly compressed - takes a shorter time

            //image.Save(outImageName);

            #endregion

            FastCopy(image, bitmap);
            SaveAsBitmap(imageFilePath, bitmap, dpiX, dpiY, optimisationSetting);

            return (true, imageWidth, imageHeight);
        }

        public static (Bitmap Bitmap, GeometryExtents<double> Extents, List<string> Labels, int Count) Get1BppImageComplex(string dxfFilePath, string[] layerNames, double dpiX = 120, double dpiY = 120, double pixelSize = 96, double angle = 0, PixelFormat pixelFormat = PixelFormat.Format1bppIndexed, GIColor bgColor = GIColor.White, GIColor fgColor = GIColor.Black, double pointSize = 0.2, double lineWidth = 0.2, bool shouldFill=true, bool shouldCloseGeometries = false, double closureTolerance = 0.0001)
        {
            var geometriesIn = new List<IMarkGeometry>();
            var labels = new List<string>();

            foreach(var kv in GeometricArithmeticModule.ExtractLabelledGeometriesFromDXF(dxfFilePath, layerNames))
            {
                labels.Add(kv.Key);
                geometriesIn.AddRange(kv.Value);
            }

            if (shouldCloseGeometries)
            {
                var lines = geometriesIn.Where(g => g is MarkGeometryLine).Select(g => (MarkGeometryLine)g).ToList();
                var (paths, unsedLines) = GeometricArithmeticModule.GeneratePathsFromLineSequence(
                    lines,
                    closureTolerance
                );

                geometriesIn.RemoveAll(x => lines.Contains(x));
                geometriesIn.AddRange(paths);
                geometriesIn.AddRange(unsedLines);
            }

            var scaleFactorX = (dpiX / pixelSize);
            var scaleFactorY = (dpiY / pixelSize);
            var scale = 0.5 * (scaleFactorX + scaleFactorY);
            var geometriesInExtents = GeometricArithmeticModule.CalculateExtents(geometriesIn);

            angle = GeometricArithmeticModule.ToRadians(angle);
            foreach (var geometry in geometriesIn)
            {
                GeometricArithmeticModule.Rotate(geometry, 0, 0, angle, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z);
            }

            var geometries = GeometricArithmeticModule.AlignTopLeftToOrigin(geometriesIn.ConvertAll(g => GeometricArithmeticModule.Scale(((IMarkGeometry)g.Clone()), scaleFactorX, scaleFactorY, 1, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z)).ToArray());
            var extents = GeometricArithmeticModule.CalculateExtents(geometries);

            int imageWidth = (int)Math.Ceiling(extents.Width);
            int imageHeight = (int)Math.Ceiling(extents.Height);

            var image = new Image<Gray, Byte>(imageWidth, imageHeight);
            var bitmap = new Bitmap(imageWidth, imageHeight, pixelFormat);

            var __bgColor = bgColor == GIColor.Black ? Color.Black : Color.White;
            var __fillColor = fgColor == GIColor.Black ? Color.Black : Color.White;

            // Draw background
            image.SetValue(ToGray(__bgColor));

            var items = MarkGeometryTree.FromGeometries(new List<IMarkGeometry>(geometries), bgColor == GIColor.Black ? Color.Black : Color.White, fgColor == GIColor.Black ? Color.Black : Color.White);

            foreach (var item in items)
            {
                if (item is MarkGeometryTree tree)
                {
                    tree.BeginGetAll((geometry) =>
                    {
                        DrawGeometry(image, geometry, scale, pointSize, lineWidth, __bgColor, __fillColor, shouldFill);
                        return true;
                    });
                }
                else
                {
                    DrawGeometry(image, item, scale, pointSize, lineWidth, __bgColor, __fillColor, shouldFill);
                }
            }

            FastCopy(image, bitmap);
            return (bitmap, geometriesInExtents, labels, geometriesIn.Count);
        }

        public static (Bitmap Bitmap, GeometryExtents<double> Extents, List<string> Labels, int Count) Get1BppImage(string dxfFilePath, string[] layerNames, double dpiX = 120, double dpiY = 120, double pixelSize = 96, double angle = 0, PixelFormat pixelFormat = PixelFormat.Format1bppIndexed, GIColor bgColor = GIColor.White, GIColor fgColor = GIColor.Black, double pointSize = 0.2, double lineWidth = 0.2, bool shouldFill = true, bool shouldCloseGeometries = false, double closureTolerance = 0.0001)
        {
            var geometriesIn = new List<IMarkGeometry>();
            var labels = new List<string>();

            foreach (var kv in GeometricArithmeticModule.ExtractLabelledGeometriesFromDXF(dxfFilePath, layerNames))
            {
                labels.Add(kv.Key);
                geometriesIn.AddRange(kv.Value);
            }

            if (shouldCloseGeometries)
            {
                var lines = geometriesIn.Where(g => g is MarkGeometryLine).Select(g => (MarkGeometryLine)g).ToList();
                var (paths, unsedLines) = GeometricArithmeticModule.GeneratePathsFromLineSequence(
                    lines,
                    closureTolerance
                );

                geometriesIn.RemoveAll(x => lines.Contains(x));
                geometriesIn.AddRange(paths);
                geometriesIn.AddRange(unsedLines);
            }

            var scaleFactorX = (dpiX / pixelSize);
            var scaleFactorY = (dpiY / pixelSize);
            var scale = 0.5 * (scaleFactorX + scaleFactorY);
            var geometriesInExtents = GeometricArithmeticModule.CalculateExtents(geometriesIn);

            angle = GeometricArithmeticModule.ToRadians(angle);
            foreach (var geometry in geometriesIn)
            {
                GeometricArithmeticModule.Rotate(geometry, 0, 0, angle, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z);
            }

            var geometries = GeometricArithmeticModule.AlignTopLeftToOrigin(geometriesIn.ConvertAll(g => GeometricArithmeticModule.Scale(((IMarkGeometry)g.Clone()), scaleFactorX, scaleFactorY, 1, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z)).ToArray());
            var extents = GeometricArithmeticModule.CalculateExtents(geometries);

            int imageWidth = (int)Math.Ceiling(extents.Width);
            int imageHeight = (int)Math.Ceiling(extents.Height);

            var image = new Image<Gray, Byte>(imageWidth, imageHeight);
            var bitmap = new Bitmap(imageWidth, imageHeight, pixelFormat);

            var __bgColor = new Gray(bgColor == GIColor.Black ? 0 : 255);
            var __fillColor = new Gray(fgColor == GIColor.Black ? 0 : 255);

            // Draw background
            image.SetValue(__bgColor);

            int thickness = (int)(scale * lineWidth);
            int pointThickness = (int)(scale * pointSize);

            foreach (var geometry in geometries)
            {
                if (geometry is MarkGeometryPoint point)
                {
                    image.Draw(new CircleF(point, pointThickness), __fillColor, -1);
                }
                else if (geometry is MarkGeometryLine line)
                {
                    image.Draw(line, __fillColor, thickness);
                }
                else if (geometry is MarkGeometryCircle circle)
                {
                    if (shouldFill)
                        image.Draw(new CircleF(new PointF((float)circle.CentrePoint.X, (float)circle.CentrePoint.Y), (float)(circle.Radius)), __fillColor, -1, LineType.Filled);
                    else
                        image.Draw(new CircleF(new PointF((float)circle.CentrePoint.X, (float)circle.CentrePoint.Y), (float)(circle.Radius)), __fillColor, -1, LineType.AntiAlias);
                }
                else if (geometry is MarkGeometryArc arc)
                {
                    DrawPath(image, new MarkGeometryPath(arc), __fillColor, thickness);
                }
                else if (geometry is MarkGeometryPath path)
                {
                    DrawPath(image, path, __fillColor, thickness, shouldFill);
                }
            }

            FastCopy(image, bitmap);
            return (bitmap, geometriesInExtents, labels, geometriesIn.Count);
        }


        public static (bool success, int width, int height) To1BppImageComplex(string dxfFilePath, string[] imageFilePaths, double dpiX = 120, double dpiY = 120, double pixelSize = 96, double angle = 0, PixelFormat pixelFormat = PixelFormat.Format1bppIndexed, OptimisationSetting optimisationSetting = OptimisationSetting.Speed, GIColor bgColor = GIColor.White, GIColor fgColor = GIColor.Black, double pointSize = 0.2, double lineWidth = 0.2)
        {
            var geometriesIn = GeometricArithmeticModule.ExtractGeometriesFromDXF(dxfFilePath);

            var scaleFactorX = (dpiX / pixelSize);
            var scaleFactorY = (dpiY / pixelSize);
            var scale = 0.5 * (scaleFactorX + scaleFactorY);
            var geometriesInExtents = GeometricArithmeticModule.CalculateExtents(geometriesIn);

            angle = GeometricArithmeticModule.ToRadians(angle);
            foreach (var geometry in geometriesIn)
            {
                GeometricArithmeticModule.Rotate(geometry, 0, 0, angle, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z);
            }

            var geometries = GeometricArithmeticModule.AlignTopLeftToOrigin(geometriesIn.ConvertAll(g => GeometricArithmeticModule.Scale(((IMarkGeometry)g.Clone()), scaleFactorX, scaleFactorY, 1, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z)).ToArray());
            var extents = GeometricArithmeticModule.CalculateExtents(geometries);

            int imageWidth = (int)Math.Ceiling(extents.Width);
            int imageHeight = (int)Math.Ceiling(extents.Height);

            var image = new Image<Gray, Byte>(imageWidth, imageHeight);
            var bitmap = new Bitmap(imageWidth, imageHeight, pixelFormat);

            var __bgColor = bgColor == GIColor.Black ? Color.Black : Color.White;
            var __fillColor = fgColor == GIColor.Black ? Color.Black : Color.White;

            // Draw background
            image.SetValue(ToGray(__bgColor));

            var items = MarkGeometryTree.FromGeometries(new List<IMarkGeometry>(geometries), bgColor == GIColor.Black ? Color.Black : Color.White, fgColor == GIColor.Black ? Color.Black : Color.White);

            foreach (var item in items)
            {
                if (item is MarkGeometryTree tree)
                {
                    tree.BeginGetAll((geometry) =>
                    {
                        DrawGeometry(image, geometry, scale, pointSize, lineWidth, __bgColor, __fillColor);
                        return true;
                    });
                }
                else
                {
                    DrawGeometry(image, item, scale, pointSize, lineWidth, __bgColor, __fillColor);
                }
            }

            #region Section: Save as Uncompressed - takes a longer time
            //var tiffWriter = new TiffWriter<Gray, Byte>(outImageName);
            //tiffWriter.WriteImage(image); 
            #endregion

            #region Section: Save as Losslessly compressed - takes a shorter time

            //image.Save(outImageName);

            #endregion

            FastCopy(image, bitmap);
            foreach(var imageFilePath in imageFilePaths)
            {
                SaveAsBitmap(imageFilePath, bitmap, dpiX, dpiY, optimisationSetting);
            }

            return (true, imageWidth, imageHeight);
        }

        private static Gray ToGray(Color color)
        {
            return color == Color.White ? new Gray(255) : new Gray(0);
        }

        private static void DrawGeometry(Image<Gray, Byte> image, IMarkGeometry geometry, double scale, double pointSize, double lineWidth, Color bgColor, Color fgColor, bool shouldFill=true)
        {
            if (geometry is MarkGeometryPoint point)
            {
                image.Draw(new CircleF(point, (float)(scale * pointSize)), ToGray(fgColor), -1);
            }
            else if (geometry is MarkGeometryLine line)
            {
                image.Draw(line, ToGray(fgColor), (int)(scale * lineWidth));
            }
            else if (geometry is MarkGeometryCircle circle)
            {
                if (shouldFill)
                    image.Draw(new CircleF(new PointF((float)circle.CentrePoint.X, (float)circle.CentrePoint.Y), (float)(circle.Radius)), ToGray((Color)circle.Fill), -1, LineType.Filled);
                else
                    image.Draw(new CircleF(new PointF((float)circle.CentrePoint.X, (float)circle.CentrePoint.Y), (float)(circle.Radius)), ToGray((Color)circle.Fill), -1, LineType.AntiAlias);
            }
            else if (geometry is MarkGeometryArc arc)
            {
                DrawPath(image, new MarkGeometryPath(arc), ToGray(fgColor), (int)(scale * lineWidth));
            }
            else if (geometry is MarkGeometryPath path)
            {
                DrawPath(image, path, ToGray((Color)path.Fill), (int)(scale * lineWidth), shouldFill);
            }
        }
    }
}
