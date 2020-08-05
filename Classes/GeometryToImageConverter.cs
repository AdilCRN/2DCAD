using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using MSolvLib.MarkGeometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MarkGeometriesLib.Classes
{
    public static class GeometryToImageConverter
    {
        #region Section: Helpers
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

        private static void DrawPath(Image<Gray, Byte> image, MarkGeometryPath path, Gray fillColor, int thickness, bool shouldFill = true, LineType lineType = LineType.FourConnected)
        {
            var points = new List<Point>();

            foreach (var point in (MarkGeometryPoint[])path)
            {
                points.Add(point);
            }

            if (shouldFill && path.IsClosed)
            {
                image.Draw(points.ToArray(), fillColor, -1, lineType);
            }
            else
            {
                image.DrawPolyline(points.ToArray(), false, fillColor, thickness, lineType);
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

        private static Bitmap Rotate4Bpp(Bitmap bitmapIn, double angleDeg, byte bgColor = 8)
        {
            if (bitmapIn.PixelFormat != PixelFormat.Format4bppIndexed)
                return null;

            var rect = new MarkGeometryRectangle(bitmapIn.Width, bitmapIn.Height);
            GeometricArithmeticModule.Rotate(rect, 0, 0, GeometricArithmeticModule.ToRadians(angleDeg));
            var bitmapOut = new Bitmap((int)Math.Ceiling(rect.Extents.Width), (int)Math.Ceiling(rect.Extents.Height), PixelFormat.Format4bppIndexed);

            var bmpInImageData = bitmapIn.LockBits(new Rectangle(0, 0, bitmapIn.Width, bitmapIn.Height), ImageLockMode.ReadOnly, bitmapIn.PixelFormat);
            long bmpInStride = bmpInImageData.Stride;

            var bmpOutImageData = bitmapOut.LockBits(new Rectangle(0, 0, bitmapOut.Width, bitmapOut.Height), ImageLockMode.ReadWrite, bitmapIn.PixelFormat);
            long bmpOutStride = bmpOutImageData.Stride;

            long bmpInWidth = bitmapIn.Width;
            long bmpInHeight = bitmapIn.Height;
            long bmpOutHeight = bitmapOut.Height;
            long bmpOutHalfWidth = bitmapOut.Width / 2;

            long _col, _row, outIndex, inIndex;
            double cxIn = 0.5 * bmpInImageData.Width;
            double cyIn = 0.5 * bmpInImageData.Height;
            double cxOut = 0.5 * bmpOutImageData.Width;
            double cyOut = 0.5 * bmpOutImageData.Height;
            double sa = Math.Sin(GeometricArithmeticModule.ToRadians(-angleDeg));
            double ca = Math.Cos(GeometricArithmeticModule.ToRadians(-angleDeg));

            // mirror 4bpp bg-color accross both halves of the byte;
            bgColor = Math.Min(bgColor, (byte)0x0F);
            bgColor = (byte)((bgColor | 0xF0) & ((bgColor << 4) | 0x0F));

            unsafe
            {
                byte* bmpInPtr = (byte*)bmpInImageData.Scan0.ToPointer();
                byte* bmpOutPtr = (byte*)bmpOutImageData.Scan0.ToPointer();

                #region Section: Linear Version

                for (int row = 0; row < bmpOutHeight; row++)
                {
                    for (int col = 0; col < bmpOutHalfWidth; col++)
                    {
                        // pixel a
                        _col = (long)(((((col * 2) - cxOut) * ca) - ((row - cyOut) * sa)) + cxIn);
                        _row = (long)((((row - cyOut) * ca) + (((col * 2) - cxOut) * sa)) + cyIn);
                        outIndex = (row * bmpOutStride) + col;
                        inIndex = (_row * bmpInStride) + (_col / 2);

                        // check row and column is within in image
                        if (
                            _col < 0 ||
                            _col >= bmpInWidth ||
                            _row < 0 ||
                            _row >= bmpInHeight
                        )
                        {
                            bmpOutPtr[outIndex] = (byte)((bmpOutPtr[outIndex] | 0xF0) & (0x0F | bgColor));
                        }
                        else if (_col % 2 == 0)
                        {
                            // read the high byte
                            bmpOutPtr[outIndex] = (byte)((bmpOutPtr[outIndex] | 0xF0) & (0x0F | bmpInPtr[inIndex]));
                        }
                        else
                        {
                            // read the low byte
                            bmpOutPtr[outIndex] = (byte)((bmpOutPtr[outIndex] | 0xF0) & (0x0F | (bmpInPtr[inIndex] << 4)));
                        }

                        // pixel b
                        _col = (long)((((((col * 2) + 1) - cxOut) * ca) - ((row - cyOut) * sa)) + cxIn);
                        _row = (long)((((row - cyOut) * ca) + ((((col * 2) + 1) - cxOut) * sa)) + cyIn);
                        inIndex = (_row * bmpInStride) + (_col / 2);

                        if (
                            _col < 0 ||
                            _col >= bmpInWidth ||
                            _row < 0 ||
                            _row >= bmpInHeight
                        )
                        {
                            bmpOutPtr[outIndex] = (byte)((bmpOutPtr[outIndex] | 0x0F) & (0xF0 | bgColor));
                        }
                        else if (_col % 2 == 0)
                        {
                            // read the high byte
                            bmpOutPtr[outIndex] = (byte)((bmpOutPtr[outIndex] | 0x0F) & (0xF0 | (bmpInPtr[inIndex] >> 4)));
                        }
                        else
                        {
                            // read the low byte
                            bmpOutPtr[outIndex] = (byte)((bmpOutPtr[outIndex] | 0x0F) & (0xF0 | bmpInPtr[inIndex]));
                        }
                    }
                }

                #endregion

                #region Section: Parallel Version

                //Parallel.For(0, bmpOutHeight, (row) =>
                //        {
                //            Parallel.For(0, bmpOutHalfWidth, (col) =>
                //            {
                //                byte* bmpInPtr = (byte*)bmpInImageData.Scan0.ToPointer();
                //                byte* bmpOutPtr = (byte*)bmpOutImageData.Scan0.ToPointer();
                //        // pixel a
                //        _col = (long)(((((col * 2) - cxOut) * ca) - ((row - cyOut) * sa)) + cxIn);
                //                _row = (long)((((row - cyOut) * ca) + (((col * 2) - cxOut) * sa)) + cyIn);
                //                outIndex = (row * bmpOutStride) + col;
                //                inIndex = (_row * bmpInStride) + (_col / 2);

                //        // check row and column is within in image
                //        if (
                //                    _col < 0 ||
                //                    _col >= bmpInWidth ||
                //                    _row < 0 ||
                //                    _row >= bmpInHeight
                //                )
                //                {
                //                    bmpOutPtr[outIndex] = (byte)((bmpOutPtr[outIndex] | 0xF0) & (0x0F | bgColor));
                //                }
                //                else if (_col % 2 == 0)
                //                {
                //            // read the high byte
                //            bmpOutPtr[outIndex] = (byte)((bmpOutPtr[outIndex] | 0xF0) & (0x0F | bmpInPtr[inIndex]));
                //                }
                //                else
                //                {
                //            // read the low byte
                //            bmpOutPtr[outIndex] = (byte)((bmpOutPtr[outIndex] | 0xF0) & (0x0F | (bmpInPtr[inIndex] << 4)));
                //                }

                //        // pixel b
                //        _col = (long)((((((col * 2) + 1) - cxOut) * ca) - ((row - cyOut) * sa)) + cxIn);
                //                _row = (long)((((row - cyOut) * ca) + ((((col * 2) + 1) - cxOut) * sa)) + cyIn);
                //                inIndex = (_row * bmpInStride) + (_col / 2);

                //                if (
                //                    _col < 0 ||
                //                    _col >= bmpInWidth ||
                //                    _row < 0 ||
                //                    _row >= bmpInHeight
                //                )
                //                {
                //                    bmpOutPtr[outIndex] = (byte)((bmpOutPtr[outIndex] | 0x0F) & (0xF0 | bgColor));
                //                }
                //                else if (_col % 2 == 0)
                //                {
                //            // read the high byte
                //            bmpOutPtr[outIndex] = (byte)((bmpOutPtr[outIndex] | 0x0F) & (0xF0 | (bmpInPtr[inIndex] >> 4)));
                //                }
                //                else
                //                {
                //            // read the low byte
                //            bmpOutPtr[outIndex] = (byte)((bmpOutPtr[outIndex] | 0x0F) & (0xF0 | bmpInPtr[inIndex]));
                //                }
                //            });
                //        }); 

                #endregion
            } 


            bitmapIn.UnlockBits(bmpInImageData);
            bitmapOut.UnlockBits(bmpOutImageData);
            bitmapOut.SetResolution(bitmapIn.HorizontalResolution, bitmapIn.VerticalResolution);

            return bitmapOut;
        }

        private static Bitmap Rotate4Bpp_Parallel(Bitmap bitmapIn, double angleDeg, byte bgColor = 8)
        {
            if (bitmapIn.PixelFormat != PixelFormat.Format4bppIndexed)
                return null;

            var rect = new MarkGeometryRectangle(bitmapIn.Width, bitmapIn.Height);
            GeometricArithmeticModule.Rotate(rect, 0, 0, GeometricArithmeticModule.ToRadians(angleDeg));
            var bitmapOut = new Bitmap((int)Math.Ceiling(rect.Extents.Width), (int)Math.Ceiling(rect.Extents.Height), PixelFormat.Format4bppIndexed);

            var bmpInImageData = bitmapIn.LockBits(new Rectangle(0, 0, bitmapIn.Width, bitmapIn.Height), ImageLockMode.ReadOnly, bitmapIn.PixelFormat);
            long bmpInStride = bmpInImageData.Stride;

            var bmpOutImageData = bitmapOut.LockBits(new Rectangle(0, 0, bitmapOut.Width, bitmapOut.Height), ImageLockMode.ReadWrite, bitmapIn.PixelFormat);
            long bmpOutStride = bmpOutImageData.Stride;

            var bmpInPtr = bmpInImageData.Scan0;
            var bmpOutPtr = bmpOutImageData.Scan0;

            long bmpInWidth = bitmapIn.Width;
            long bmpInHeight = bitmapIn.Height;
            long bmpOutHeight = bitmapOut.Height;
            long bmpOutHalfWidth = bitmapOut.Width / 2;
            int bmpInBytes = (int)(Math.Abs(bmpInStride) * bmpInHeight);
            int bmpOutBytes = (int)(Math.Abs(bmpOutStride) * bmpOutHeight);

            double cxIn = 0.5 * bmpInImageData.Width;
            double cyIn = 0.5 * bmpInImageData.Height;
            double cxOut = 0.5 * bmpOutImageData.Width;
            double cyOut = 0.5 * bmpOutImageData.Height;
            double sa = Math.Sin(GeometricArithmeticModule.ToRadians(-angleDeg));
            double ca = Math.Cos(GeometricArithmeticModule.ToRadians(-angleDeg));

            // mirror 4bpp bg-color accross both halves of the byte;
            bgColor = Math.Min(bgColor, (byte)0x0F);
            bgColor = (byte)((bgColor | 0xF0) & ((bgColor << 4) | 0x0F));

            // create buffers for in and out images
            var bmpInValues = new byte[bmpInBytes];
            var bmpOutValues = new byte[bmpOutBytes];

            // copy bmp in to buffer in
            Marshal.Copy(bmpInPtr, bmpInValues, 0, bmpInBytes);

            #region Section: Parallel Version

            Parallel.For(0, bmpOutHeight, (row) =>
            {
                Parallel.For(0, bmpOutHalfWidth, (col) =>
                {
                    long _col, _row, outIndex, inIndex;

                    // pixel a
                    _col = (long)(((((col * 2) - cxOut) * ca) - ((row - cyOut) * sa)) + cxIn);
                    _row = (long)((((row - cyOut) * ca) + (((col * 2) - cxOut) * sa)) + cyIn);
                    outIndex = (row * bmpOutStride) + col;
                    inIndex = (_row * bmpInStride) + (_col / 2);

                    // check row and column is within in image
                    if (
                        _col < 0 ||
                        _col >= bmpInWidth ||
                        _row < 0 ||
                        _row >= bmpInHeight
                    )
                    {
                        bmpOutValues[outIndex] = (byte)((bmpOutValues[outIndex] | 0xF0) & (0x0F | bgColor));
                    }
                    else if (_col % 2 == 0)
                    {
                        // read the high byte
                        bmpOutValues[outIndex] = (byte)((bmpOutValues[outIndex] | 0xF0) & (0x0F | bmpInValues[inIndex]));
                    }
                    else
                    {
                        // read the low byte
                        bmpOutValues[outIndex] = (byte)((bmpOutValues[outIndex] | 0xF0) & (0x0F | (bmpInValues[inIndex] << 4)));
                    }

                    // pixel b
                    _col = (long)((((((col * 2) + 1) - cxOut) * ca) - ((row - cyOut) * sa)) + cxIn);
                    _row = (long)((((row - cyOut) * ca) + ((((col * 2) + 1) - cxOut) * sa)) + cyIn);
                    inIndex = (_row * bmpInStride) + (_col / 2);

                    if (
                        _col < 0 ||
                        _col >= bmpInWidth ||
                        _row < 0 ||
                        _row >= bmpInHeight
                    )
                    {
                        bmpOutValues[outIndex] = (byte)((bmpOutValues[outIndex] | 0x0F) & (0xF0 | bgColor));
                    }
                    else if (_col % 2 == 0)
                    {
                        // read the high byte
                        bmpOutValues[outIndex] = (byte)((bmpOutValues[outIndex] | 0x0F) & (0xF0 | (bmpInValues[inIndex] >> 4)));
                    }
                    else
                    {
                        // read the low byte
                        bmpOutValues[outIndex] = (byte)((bmpOutValues[outIndex] | 0x0F) & (0xF0 | bmpInValues[inIndex]));
                    }
                });
            });

            #endregion

            // copy out buffer to bmp out
            Marshal.Copy(bmpOutValues, 0, bmpOutPtr, bmpOutBytes);

            // unlock bits
            bitmapIn.UnlockBits(bmpInImageData);
            bitmapOut.UnlockBits(bmpOutImageData);
            bitmapOut.SetResolution(bitmapIn.HorizontalResolution, bitmapIn.VerticalResolution);

            return bitmapOut;
        }

        //private static long GetRotatedIndex(int rowIn, int columnIn, double imgHalfWidth, double imgHalfHeight, double ca, double sa, int imgStrideOut)
        //{
        //    // translate to origin
        //    double px = columnIn - imgHalfWidth;
        //    double py = rowIn - imgHalfHeight;

        //    // rotate
        //    var _x = (px * ca) - (py * sa);
        //    var _y = (py * ca) + (px * sa);

        //    // translate back to centre
        //    px = _x + imgHalfWidth;
        //    py = _y + imgHalfHeight;

        //    return (py * imgStrideOut) + px;
        //}

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

        private static Gray ToGray(Color color)
        {
            return color == Color.White ? new Gray(255) : new Gray(0);
        }

        private static void DrawGeometry(Image<Gray, Byte> image, IMarkGeometry geometry, double scale, double pointSize, double lineWidth, Color bgColor, Color fgColor, bool shouldFill = true, LineType lineType = LineType.FourConnected)
        {
            if (geometry is MarkGeometryPoint point)
            {
                image.Draw(new CircleF(point, (float)(scale * pointSize)), ToGray(fgColor), -1, lineType);
            }
            else if (geometry is MarkGeometryLine line)
            {
                image.Draw(line, line.Fill == null ? ToGray(fgColor) : ToGray((Color)line.Fill), (int)Math.Round(scale * lineWidth), lineType);
            }
            else if (geometry is MarkGeometryCircle circle)
            {
                if (shouldFill)
                    image.Draw(new CircleF(new PointF((float)circle.CentrePoint.X, (float)circle.CentrePoint.Y), (float)(circle.Radius)), ToGray((Color)circle.Fill), -1, lineType);
                else
                    image.Draw(new CircleF(new PointF((float)circle.CentrePoint.X, (float)circle.CentrePoint.Y), (float)(circle.Radius)), ToGray((Color)circle.Fill), (int)Math.Round(scale * lineWidth), lineType);
            }
            else if (geometry is MarkGeometryArc arc)
            {
                DrawPath(image, new MarkGeometryPath(arc), ToGray(fgColor), (int)Math.Round(scale * lineWidth), shouldFill, lineType);
            }
            else if (geometry is MarkGeometryPath path)
            {
                DrawPath(image, path, ToGray((Color)path.Fill), (int)Math.Round(scale * lineWidth), shouldFill, lineType);
            }
        }

        public static Bitmap RotateImage4Bpp(string imagePathIn, double angle, GIColor bgColor = GIColor.White, bool crop = false)
        {
            var __bgColor = new Gray(bgColor == GIColor.Black ? 0 : 255);

            var image = CvInvoke.Imread(imagePathIn, ImreadModes.ReducedGrayscale4).ToImage<Gray, Byte>().Rotate(angle, __bgColor, crop);
            var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format4bppIndexed);

            FastCopy(image, bitmap);

            return bitmap;
        }

        public static (bool Success, int Width, int Height) RotateImage4Bpp(string imagePathIn, string imagePathOut, double angle, double dpiX = 720, double dpiY = 720, GIColor bgColor = GIColor.White, bool crop = false)
        {
            var bmp = RotateImage4Bpp(imagePathIn, angle, bgColor, crop);
            SaveAsBitmap(imagePathOut, bmp, dpiX, dpiY, OptimisationSetting.HighestQuality);
            return (File.Exists(imagePathOut), bmp.Width, bmp.Height);
        }

        public static (bool Success, int Width, int Height) RotateImage4BppV2(string imagePathIn, string imagePathOut, double angle)
        {
            var bmpIn = new Bitmap(imagePathIn);
            var bmpOut = Rotate4Bpp(bmpIn, angle);
            SaveAsBitmap(imagePathOut, bmpOut, bmpOut.HorizontalResolution, bmpOut.VerticalResolution, OptimisationSetting.HighestQuality);
            return (File.Exists(imagePathOut), bmpOut.Width, bmpOut.Height);
        }

        public static (bool Success, int Width, int Height) RotateImage4BppV2_Parallel(string imagePathIn, string imagePathOut, double angle)
        {
            var bmpIn = new Bitmap(imagePathIn);
            var bmpOut = Rotate4Bpp_Parallel(bmpIn, angle);
            SaveAsBitmap(imagePathOut, bmpOut, bmpOut.HorizontalResolution, bmpOut.VerticalResolution, OptimisationSetting.HighestQuality);
            return (File.Exists(imagePathOut), bmpOut.Width, bmpOut.Height);
        }

        #endregion

        /// <summary>
        ///     Convert a DXf file to a picture. Supports version R12, R13 and DXF 2007.
        /// </summary>
        /// <param name="dxfFilePath">The full filepath to the DXF</param>
        /// <param name="imageFilePaths">The full filepath to the output images (will be be created/overwritten)</param>
        /// <param name="layerNames">The names of the layers to draw (leave empty or use null to draw all layers)</param>
        /// <param name="dpiX">The export DPI on the x axis</param>
        /// <param name="dpiY">The export DPI on the y axis</param>
        /// <param name="pixelSize">The size of the pixels</param>
        /// <param name="angle">The angle (in degrees) of the exported image. positive is counter-clockwise. image resizes to fit DXF</param>
        /// <param name="preferredAxis">The reference axis to retain</param> // TODO : Come up with a better description
        /// <param name="pixelFormat">The pixel format (default is 1bpp)</param>
        /// <param name="optimisationSetting">The optimisation setting, use to compress the generated images</param>
        /// <param name="bgColor">The background color</param>
        /// <param name="fgColor">The foreground color</param>
        /// <param name="pointSize">The point size in millimetres</param>
        /// <param name="lineWidth">The line width in millimetres</param>
        /// <param name="shouldFill">Use to toggle filling closed geometries</param>
        /// <param name="shouldCloseGeometries">Use to toggle the post-processing (closing geometries) applied to the geometries</param>
        /// <param name="closureTolerance">Set the tolerance factor used to judge open and closed geometries</param>
        /// <returns>The status (true if successful), and the centre, width and height of the post-processed DXF</returns>
        public static (bool Success, double CentreX, double CentreY, double Width, double Height) To1BppImageComplexRetainBounds(
            string dxfFilePath,
            string[] imageFilePaths,
            string[] layerNames = null,
            double dpiX = 720, double dpiY = 720,
            double pixelSize = 25.4, double angle = 0,
            PixelFormat pixelFormat = PixelFormat.Format1bppIndexed,
            OptimisationSetting optimisationSetting = OptimisationSetting.Speed,
            GIColor bgColor = GIColor.White, GIColor fgColor = GIColor.Black,
            double pointSize = 0.1, double lineWidth = 0.1,
            bool shouldFill = true, bool shouldCloseGeometries = true,
            double closureTolerance = 0.01
        )
        {
            // create buffer to store geometries
            var geometriesIn = new List<IMarkGeometry>();

            if (shouldCloseGeometries)
            {
                List<MarkGeometryPath> openGeometries = new List<MarkGeometryPath>();
                List<IMarkGeometry> closedGeometries = new List<IMarkGeometry>();

                foreach (var kv in GeometricArithmeticModule.ExtractLabelledGeometriesFromDXF(dxfFilePath, layerNames))
                {
                    for (int i = 0; i < kv.Value.Count; i++)
                    {
                        if (kv.Value[i] is MarkGeometryLine line)
                        {
                            openGeometries.Add(new MarkGeometryPath(line));
                        }
                        else if (kv.Value[i] is MarkGeometryArc arc)
                        {
                            if (Math.Abs(arc.Sweep % (2 * Math.PI)) <= 0.0001)
                                closedGeometries.Add(new MarkGeometryPath(arc));
                            else
                                openGeometries.Add(new MarkGeometryPath(arc));
                        }
                        else if (kv.Value[i] is MarkGeometryPath path)
                        {
                            if (path.IsClosed)
                                closedGeometries.Add(path);
                            else
                                openGeometries.Add(path);
                        }
                        else
                        {
                            closedGeometries.Add(kv.Value[i]);
                        }
                    }
                }

                closedGeometries.AddRange(
                    GeometricArithmeticModule.Simplify(openGeometries, closureTolerance)
                );

                geometriesIn = closedGeometries;
            }
            else
            {
                foreach (var kv in GeometricArithmeticModule.ExtractLabelledGeometriesFromDXF(dxfFilePath, layerNames))
                {
                    geometriesIn.AddRange(kv.Value);
                }
            }

            // convert 1DPI color to Color
            var __bgColor = bgColor == GIColor.Black ? Color.Black : Color.White;
            var __fillColor = fgColor == GIColor.Black ? Color.Black : Color.White;

            // create colour tree from geometries
            var geometriesAsTrees = MarkGeometryTree.FromGeometries(
                geometriesIn, __bgColor, __fillColor
            );

            // calculate transforms to be applied to the geometries
            var maxDPI = Math.Max(dpiX, dpiY);
            var scaleFactorX = (dpiX / pixelSize);
            var scaleFactorY = (dpiY / pixelSize);
            var scale = 0.5 * (scaleFactorX + scaleFactorY);
            var preferredScaleFactor = Math.Max(scaleFactorX, scaleFactorY);
            var geometriesAsTreesExtents = GeometricArithmeticModule.CalculateExtents(geometriesAsTrees);

            // create rectangle to represent the preferred size
            var preferredBounds = new MarkGeometryRectangle(
                geometriesAsTreesExtents.Centre,
                geometriesAsTreesExtents.Width,
                geometriesAsTreesExtents.Height
            );

            var transformMatrix = GeometricArithmeticModule.CombineTransformations(
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    -geometriesAsTreesExtents.Centre.X,
                    -geometriesAsTreesExtents.Centre.Y,
                    -geometriesAsTreesExtents.Centre.Z
                ),
                GeometricArithmeticModule.GetScalingTransformationMatrix(
                    scaleFactorX,
                    -scaleFactorY,
                    1
                ),
                GeometricArithmeticModule.GetRotationTransformationMatrix(
                    0,
                    0,
                    GeometricArithmeticModule.ToRadians(
                        -angle
                    )
                ),
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    geometriesAsTreesExtents.Centre.X,
                    geometriesAsTreesExtents.Centre.Y,
                    geometriesAsTreesExtents.Centre.Z
                )
            );

            var preferredTransformMatrix = GeometricArithmeticModule.CombineTransformations(
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    -geometriesAsTreesExtents.Centre.X,
                    -geometriesAsTreesExtents.Centre.Y,
                    -geometriesAsTreesExtents.Centre.Z
                ),
                GeometricArithmeticModule.GetScalingTransformationMatrix(
                    preferredScaleFactor,
                    -preferredScaleFactor,
                    1
                ),
                GeometricArithmeticModule.GetRotationTransformationMatrix(
                    0,
                    0,
                    GeometricArithmeticModule.ToRadians(
                        -angle
                    )
                ),
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    geometriesAsTreesExtents.Centre.X,
                    geometriesAsTreesExtents.Centre.Y,
                    geometriesAsTreesExtents.Centre.Z
                )
            );

            // apply transform matrix to geometries
            for (int i = 0; i < geometriesAsTrees.Count; i++)
                geometriesAsTrees[i].Transform(transformMatrix);

            // apply transform to the preferred boundary
            preferredBounds.Transform(preferredTransformMatrix);

            // align boundary to top left image origin
            preferredBounds.Transform(
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    -preferredBounds.Extents.MinX,
                    -preferredBounds.Extents.MinY,
                    -preferredBounds.Extents.MinZ
                )
            );

            // now we need to align the geometries so that
            // so that they are centred within the boundary
            var geometries = GeometricArithmeticModule.AlignCentreToExtents(geometriesAsTrees, preferredBounds.Extents);

            // calculate image's width
            int imageWidth = (int)Math.Round(preferredBounds.Extents.Width);
            int imageHeight = (int)Math.Round(preferredBounds.Extents.Height);

            var image = new Image<Gray, Byte>(imageWidth, imageHeight);
            var bitmap = new Bitmap(imageWidth, imageHeight, pixelFormat);

            // Draw background
            image.SetValue(ToGray(__bgColor));

            for (int i = 0; i < geometries.Count; i++)
                if (geometries[i] is MarkGeometryTree tree)
                    tree.BeginGetAll((geometry) =>
                    {
                        DrawGeometry(image, geometry, scale, pointSize, lineWidth, __bgColor, __fillColor);
                        return true;
                    });
                else
                    DrawGeometry(image, geometries[i], scale, pointSize, lineWidth, __bgColor, __fillColor);

            FastCopy(image, bitmap);
            for (int i = 0; i < imageFilePaths.Count(); i++)
                SaveAsBitmap(imageFilePaths[i], bitmap, maxDPI, dpiY, optimisationSetting);

            return (
                true,                                // task successful
                preferredBounds.Extents.Centre.X,              // centre of processed DXF; use for alignment
                preferredBounds.Extents.Centre.Y,
                preferredBounds.Extents.Width / preferredScaleFactor,  // size of processed DXF; use for alignment
                preferredBounds.Extents.Height / preferredScaleFactor
            );
        }

        /// <summary>
        ///     Convert a DXf file to a picture. Supports version R12, R13 and DXF 2007.
        /// </summary>
        /// <param name="dxfFilePath">The full filepath to the DXF</param>
        /// <param name="imageFilePath">The full filepath to the output image (will be be created/overwritten)</param>
        /// <param name="layerNames">The names of the layers to draw (leave empty or use null to draw all layers)</param>
        /// <param name="dpiX">The export DPI on the x axis</param>
        /// <param name="dpiY">The export DPI on the y axis</param>
        /// <param name="pixelSize">The size of the pixels</param>
        /// <param name="angle">The angle (in degrees) of the exported image. positive is counter-clockwise. image resizes to fit DXF</param>
        /// <param name="preferredAxis">The reference axis to retain</param> // TODO : Come up with a better description
        /// <param name="pixelFormat">The pixel format (default is 1bpp)</param>
        /// <param name="optimisationSetting">The optimisation setting, use to compress the generated images</param>
        /// <param name="bgColor">The background color</param>
        /// <param name="fgColor">The foreground color</param>
        /// <param name="pointSize">The point size in millimetres</param>
        /// <param name="lineWidth">The line width in millimetres</param>
        /// <param name="shouldFill">Use to toggle filling closed geometries</param>
        /// <param name="shouldCloseGeometries">Use to toggle the post-processing (closing geometries) applied to the geometries</param>
        /// <param name="closureTolerance">Set the tolerance factor used to judge open and closed geometries</param>
        /// <returns>The status (true if successful), and the centre, width and height of the post-processed DXF</returns>
        public static (bool Success, double CentreX, double CentreY, double Width, double Height) To1BppImageComplexRetainBounds(
            string dxfFilePath,
            string imageFilePath,
            string[] layerNames = null,
            double dpiX = 720, double dpiY = 720,
            double pixelSize = 25.4, double angle = 0,
            PixelFormat pixelFormat = PixelFormat.Format1bppIndexed,
            OptimisationSetting optimisationSetting = OptimisationSetting.Speed,
            GIColor bgColor = GIColor.White, GIColor fgColor = GIColor.Black,
            double pointSize = 0.1, double lineWidth = 0.1,
            bool shouldFill = true, bool shouldCloseGeometries = true,
            double closureTolerance = 0.01
        )
        {
            return To1BppImageComplexRetainBounds(
                dxfFilePath,
                new string[] { imageFilePath },
                layerNames,
                dpiX,
                dpiY,
                pixelSize,
                angle,
                pixelFormat,
                optimisationSetting,
                bgColor,
                fgColor,
                pointSize,
                lineWidth,
                shouldFill,
                shouldCloseGeometries,
                closureTolerance
            );
        }

        /// <summary>
        ///     Convert a DXf file to a picture. Supports version R12, R13 and DXF 2007.
        /// </summary>
        /// <param name="dxfFilePath">The full filepath to the DXF</param>
        /// <param name="imageFilePaths">The full filepath to the output images (will be be created/overwritten)</param>
        /// <param name="layerNames">The names of the layers to draw (leave empty or use null to draw all layers)</param>
        /// <param name="dpiX">The export DPI on the x axis</param>
        /// <param name="dpiY">The export DPI on the y axis</param>
        /// <param name="pixelSize">The size of the pixels</param>
        /// <param name="angle">The angle (in degrees) of the exported image. positive is counter-clockwise. image resizes to fit DXF</param>
        /// <param name="scaleX">Scale the DXF on the x-axis (default 1)</param>
        /// <param name="scaleY">Scale the DXF on the y-axis (default 1)</param>
        /// <param name="pixelFormat">The pixel format (default is 1bpp)</param>
        /// <param name="optimisationSetting">The optimisation setting, use to compress the generated images</param>
        /// <param name="bgColor">The background color</param>
        /// <param name="fgColor">The foreground color</param>
        /// <param name="pointSize">The point size in millimetres</param>
        /// <param name="lineWidth">The line width in millimetres</param>
        /// <param name="shouldFill">Use to toggle filling closed geometries</param>
        /// <param name="shouldCloseGeometries">Use to toggle the post-processing (closing geometries) applied to the geometries</param>
        /// <param name="closureTolerance">Set the tolerance factor used to judge open and closed geometries</param>
        /// <returns>The status (true if successful), and the centre, width and height of the post-processed DXF</returns>
        public static (bool Success, double CentreX, double CentreY, double Width, double Height) To1BppImageComplex(
            string dxfFilePath,
            string[] imageFilePaths,
            string[] layerNames = null,
            double dpiX = 720, double dpiY = 720,
            double pixelSize = 25.4, double angle = 0,
            double scaleX = 1, double scaleY = 1,
            PixelFormat pixelFormat = PixelFormat.Format1bppIndexed,
            OptimisationSetting optimisationSetting = OptimisationSetting.Speed,
            GIColor bgColor = GIColor.White, GIColor fgColor = GIColor.Black,
            double pointSize = 0.1, double lineWidth = 0.1,
            bool shouldFill = true, bool shouldCloseGeometries = true,
            double closureTolerance = 0.01
        )
        {
            var geometriesIn = new List<IMarkGeometry>();

            if (shouldCloseGeometries)
            {
                List<MarkGeometryPath> openGeometries = new List<MarkGeometryPath>();
                List<IMarkGeometry> closedGeometries = new List<IMarkGeometry>();

                foreach (var kv in GeometricArithmeticModule.ExtractLabelledGeometriesFromDXF(dxfFilePath, layerNames))
                {
                    for (int i = 0; i < kv.Value.Count; i++)
                    {
                        if (kv.Value[i] is MarkGeometryLine line)
                        {
                            openGeometries.Add(new MarkGeometryPath(line));
                        }
                        else if (kv.Value[i] is MarkGeometryArc arc)
                        {
                            if (Math.Abs(arc.Sweep % (2 * Math.PI)) <= 0.0001)
                                closedGeometries.Add(new MarkGeometryPath(arc));
                            else
                                openGeometries.Add(new MarkGeometryPath(arc));
                        }
                        else if (kv.Value[i] is MarkGeometryPath path)
                        {
                            if (path.IsClosed)
                                closedGeometries.Add(path);
                            else
                                openGeometries.Add(path);
                        }
                        else
                        {
                            closedGeometries.Add(kv.Value[i]);
                        }
                    }
                }

                closedGeometries.AddRange(
                    GeometricArithmeticModule.Simplify(openGeometries, closureTolerance)
                );

                geometriesIn = closedGeometries;
            }
            else
            {
                foreach (var kv in GeometricArithmeticModule.ExtractLabelledGeometriesFromDXF(dxfFilePath, layerNames))
                {
                    geometriesIn.AddRange(kv.Value);
                }
            }

            var geometriesAsTrees = MarkGeometryTree.FromGeometries(
                geometriesIn,
                bgColor == GIColor.Black ? Color.Black : Color.White,
                fgColor == GIColor.Black ? Color.Black : Color.White
            );

            var scaleFactorX = (dpiX / pixelSize) * scaleX;
            var scaleFactorY = (dpiY / pixelSize) * scaleY;
            var scale = 0.5 * (scaleFactorX + scaleFactorY);
            var geometriesAsTreesExtents = GeometricArithmeticModule.CalculateExtents(geometriesAsTrees);

            var transformMatrix = GeometricArithmeticModule.CombineTransformations(
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    -geometriesAsTreesExtents.Centre.X,
                    -geometriesAsTreesExtents.Centre.Y,
                    -geometriesAsTreesExtents.Centre.Z
                ),
                GeometricArithmeticModule.GetScalingTransformationMatrix(
                    scaleFactorX,
                    -scaleFactorY,
                    1
                ),
                GeometricArithmeticModule.GetRotationTransformationMatrix(
                    0,
                    0,
                    GeometricArithmeticModule.ToRadians(
                        angle
                    )
                ),
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    geometriesAsTreesExtents.Centre.X,
                    geometriesAsTreesExtents.Centre.Y,
                    geometriesAsTreesExtents.Centre.Z
                )
            );

            // add boundary to retain geometry's structure
            geometriesAsTrees.Add((IMarkGeometry)geometriesAsTreesExtents.Boundary.Clone());

            // apply transform matrix to geometries
            for (int i = 0; i < geometriesAsTrees.Count; i++)
                geometriesAsTrees[i].Transform(transformMatrix);

            var outputExtents = GeometricArithmeticModule.CalculateExtents(geometriesAsTrees);

            // don't forget to delete the added structure
            geometriesAsTrees.RemoveAt(geometriesAsTrees.Count - 1);

            var geometries = GeometricArithmeticModule.AlignTopLeftToOrigin(geometriesAsTrees);

            var extents = GeometricArithmeticModule.CalculateExtents(geometries);

            int imageWidth = (int)Math.Round(extents.Width);
            int imageHeight = (int)Math.Round(extents.Height);

            var image = new Image<Gray, Byte>(imageWidth, imageHeight);
            var bitmap = new Bitmap(imageWidth, imageHeight, pixelFormat);

            var __bgColor = bgColor == GIColor.Black ? Color.Black : Color.White;
            var __fillColor = fgColor == GIColor.Black ? Color.Black : Color.White;

            // Draw background
            image.SetValue(ToGray(__bgColor));

            for (int i = 0; i < geometries.Count; i++)
            {
                if (geometries[i] is MarkGeometryTree tree)
                {
                    tree.BeginGetAll((geometry) =>
                    {
                        DrawGeometry(image, geometry, scale, pointSize, lineWidth, __bgColor, __fillColor);
                        return true;
                    });
                }
                else
                {
                    DrawGeometry(image, geometries[i], scale, pointSize, lineWidth, __bgColor, __fillColor);
                }
            }

            FastCopy(image, bitmap);
            for (int i = 0; i < imageFilePaths.Count(); i++)
                SaveAsBitmap(imageFilePaths[i], bitmap, dpiX, dpiY, optimisationSetting);

            return (
                true,                                // task successful
                outputExtents.Centre.X,              // centre of processed DXF; use for alignment
                outputExtents.Centre.Y,
                outputExtents.Width / scaleFactorX,  // size of processed DXF; use for alignment
                outputExtents.Height / scaleFactorY
            );
        }

        /// <summary>
        ///     Convert a DXf file to a picture. Supports version R12, R13 and DXF 2007.
        /// </summary>
        /// <param name="geometriesIn">A list of geometries to write to file</param>
        /// <param name="imageFilePaths">The full filepath to the output images (will be be created/overwritten)</param>
        /// <param name="dpiX">The export DPI on the x axis</param>
        /// <param name="dpiY">The export DPI on the y axis</param>
        /// <param name="pixelSize">The size of the pixels</param>
        /// <param name="angle">The angle (in degrees) of the exported image. positive is counter-clockwise. image resizes to fit DXF</param>
        /// <param name="scaleX">Scale the DXF on the x-axis (default 1)</param>
        /// <param name="scaleY">Scale the DXF on the y-axis (default 1)</param>
        /// <param name="pixelFormat">The pixel format (default is 1bpp)</param>
        /// <param name="optimisationSetting">The optimisation setting, use to compress the generated images</param>
        /// <param name="bgColor">The background color</param>
        /// <param name="fgColor">The foreground color</param>
        /// <param name="pointSize">The point size in millimetres</param>
        /// <param name="lineWidth">The line width in millimetres</param>
        /// <param name="shouldFill">Use to toggle filling closed geometries</param>
        /// <param name="shouldCloseGeometries">Use to toggle the post-processing (closing geometries) applied to the geometries</param>
        /// <param name="closureTolerance">Set the tolerance factor used to judge open and closed geometries</param>
        /// <returns>The status (true if successful), and the centre, width and height of the post-processed DXF</returns>
        public static (bool Success, double CentreX, double CentreY, double Width, double Height) To1BppImageComplex(
            List<IMarkGeometry> geometriesIn,
            string[] imageFilePaths,
            double dpiX = 720, double dpiY = 720,
            double pixelSize = 25.4, double angle = 0,
            double scaleX = 1, double scaleY = 1,
            PixelFormat pixelFormat = PixelFormat.Format1bppIndexed,
            OptimisationSetting optimisationSetting = OptimisationSetting.Speed,
            GIColor bgColor = GIColor.White, GIColor fgColor = GIColor.Black,
            double pointSize = 0.1, double lineWidth = 0.1,
            bool shouldFill = true, bool shouldCloseGeometries = true,
            double closureTolerance = 0.01
        )
        {
            if (shouldCloseGeometries)
            {
                List<MarkGeometryPath> openGeometries = new List<MarkGeometryPath>();
                List<IMarkGeometry> closedGeometries = new List<IMarkGeometry>();

                for (int i = 0; i < geometriesIn.Count; i++)
                {
                    if (geometriesIn[i] is MarkGeometryLine line)
                    {
                        openGeometries.Add(new MarkGeometryPath(line));
                    }
                    else if (geometriesIn[i] is MarkGeometryArc arc)
                    {
                        if (Math.Abs(arc.Sweep % (2 * Math.PI)) <= 0.0001)
                            closedGeometries.Add(new MarkGeometryPath(arc));
                        else
                            openGeometries.Add(new MarkGeometryPath(arc));
                    }
                    else if (geometriesIn[i] is MarkGeometryPath path)
                    {
                        if (path.IsClosed)
                            closedGeometries.Add(path);
                        else
                            openGeometries.Add(path);
                    }
                    else
                    {
                        closedGeometries.Add(geometriesIn[i]);
                    }
                }

                closedGeometries.AddRange(
                    GeometricArithmeticModule.Simplify(openGeometries, closureTolerance)
                );

                geometriesIn = closedGeometries;
            }

            var geometriesAsTrees = MarkGeometryTree.FromGeometries(
                geometriesIn,
                bgColor == GIColor.Black ? Color.Black : Color.White,
                fgColor == GIColor.Black ? Color.Black : Color.White
            );

            var scaleFactorX = (dpiX / pixelSize) * scaleX;
            var scaleFactorY = (dpiY / pixelSize) * scaleY;
            var scale = 0.5 * (scaleFactorX + scaleFactorY);
            var geometriesAsTreesExtents = GeometricArithmeticModule.CalculateExtents(geometriesAsTrees);

            var transformMatrix = GeometricArithmeticModule.CombineTransformations(
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    -geometriesAsTreesExtents.Centre.X,
                    -geometriesAsTreesExtents.Centre.Y,
                    -geometriesAsTreesExtents.Centre.Z
                ),
                GeometricArithmeticModule.GetScalingTransformationMatrix(
                    scaleFactorX,
                    -scaleFactorY,
                    1
                ),
                GeometricArithmeticModule.GetRotationTransformationMatrix(
                    0,
                    0,
                    GeometricArithmeticModule.ToRadians(
                        angle
                    )
                ),
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    geometriesAsTreesExtents.Centre.X,
                    geometriesAsTreesExtents.Centre.Y,
                    geometriesAsTreesExtents.Centre.Z
                )
            );

            // add boundary to retain geometry's structure
            geometriesAsTrees.Add((IMarkGeometry)geometriesAsTreesExtents.Boundary.Clone());

            // apply transform matrix to geometries
            for (int i = 0; i < geometriesAsTrees.Count; i++)
                geometriesAsTrees[i].Transform(transformMatrix);

            var outputExtents = GeometricArithmeticModule.CalculateExtents(geometriesAsTrees);

            // don't forget to delete the added structure
            geometriesAsTrees.RemoveAt(geometriesAsTrees.Count - 1);

            var geometries = GeometricArithmeticModule.AlignTopLeftToOrigin(geometriesAsTrees);

            var extents = GeometricArithmeticModule.CalculateExtents(geometries);

            int imageWidth = (int)Math.Round(extents.Width);
            int imageHeight = (int)Math.Round(extents.Height);

            var image = new Image<Gray, Byte>(imageWidth, imageHeight);
            var bitmap = new Bitmap(imageWidth, imageHeight, pixelFormat);

            var __bgColor = bgColor == GIColor.Black ? Color.Black : Color.White;
            var __fillColor = fgColor == GIColor.Black ? Color.Black : Color.White;

            // Draw background
            image.SetValue(ToGray(__bgColor));

            for (int i = 0; i < geometries.Count; i++)
            {
                if (geometries[i] is MarkGeometryTree tree)
                {
                    tree.BeginGetAll((geometry) =>
                    {
                        DrawGeometry(image, geometry, scale, pointSize, lineWidth, __bgColor, __fillColor);
                        return true;
                    });
                }
                else
                {
                    DrawGeometry(image, geometries[i], scale, pointSize, lineWidth, __bgColor, __fillColor);
                }
            }

            FastCopy(image, bitmap);
            for (int i = 0; i < imageFilePaths.Count(); i++)
                SaveAsBitmap(imageFilePaths[i], bitmap, dpiX, dpiY, optimisationSetting);

            return (
                true,                                // task successful
                outputExtents.Centre.X,              // centre of processed DXF; use for alignment
                outputExtents.Centre.Y,
                outputExtents.Width / scaleFactorX,  // size of processed DXF; use for alignment
                outputExtents.Height / scaleFactorY
            );
        }

        /// <summary>
        ///     Convert a DXf file to a picture. Supports version R12, R13 and DXF 2007.
        /// </summary>
        /// <param name="dxfFilePath">The full filepath to the DXF</param>
        /// <param name="imageFilePath">The full filepath to the output image (will be be created/overwritten)</param>
        /// <param name="layerNames">The names of the layers to draw (leave empty or use null to draw all layers)</param>
        /// <param name="dpiX">The export DPI on the x axis</param>
        /// <param name="dpiY">The export DPI on the y axis</param>
        /// <param name="pixelSize">The size of the pixels</param>
        /// <param name="angle">The angle (in degrees) of the exported image. positive is counter-clockwise. image resizes to fit DXF</param>
        /// <param name="scaleX">Scale the DXF on the x-axis (default 1)</param>
        /// <param name="scaleY">Scale the DXF on the y-axis (default 1)</param>
        /// <param name="pixelFormat">The pixel format (default is 1bpp)</param>
        /// <param name="optimisationSetting">The optimisation setting, use to compress the generated images</param>
        /// <param name="bgColor">The background color</param>
        /// <param name="fgColor">The foreground color</param>
        /// <param name="pointSize">The point size in millimetres</param>
        /// <param name="lineWidth">The line width in millimetres</param>
        /// <param name="shouldFill">Use to toggle filling closed geometries</param>
        /// <param name="shouldCloseGeometries">Use to toggle the post-processing (closing geometries) applied to the geometries</param>
        /// <param name="closureTolerance">Set the tolerance factor used to judge open and closed geometries</param>
        /// <returns>The status (true if successful), and the centre, width and height of the post-processed DXF</returns>
        public static (bool Success, double CentreX, double CentreY, double Width, double Height) To1BppImageComplex(
            string dxfFilePath,
            string imageFilePath,
            string[] layerNames = null,
            double dpiX = 120, double dpiY = 120,
            double pixelSize = 96, double angle = 0,
            double scaleX = 1, double scaleY = 1,
            PixelFormat pixelFormat = PixelFormat.Format1bppIndexed,
            OptimisationSetting optimisationSetting = OptimisationSetting.Speed,
            GIColor bgColor = GIColor.White, GIColor fgColor = GIColor.Black,
            double pointSize = 0.2, double lineWidth = 0.2,
            bool shouldFill = true, bool shouldCloseGeometries = true,
            double closureTolerance = 0.01
        )
        {
            return To1BppImageComplex(
                dxfFilePath,
                new string[] { imageFilePath },
                layerNames,
                dpiX,
                dpiY,
                pixelSize,
                angle,
                scaleX,
                scaleY,
                pixelFormat,
                optimisationSetting,
                bgColor,
                fgColor,
                pointSize,
                lineWidth,
                shouldFill,
                shouldCloseGeometries,
                closureTolerance
            );
        }

        public static (Bitmap Bitmap, GeometryExtents<double> Extents, List<string> Labels, int Count) Get1BppImageComplex(string dxfFilePath, string[] layerNames, double dpiX = 120, double dpiY = 120, double pixelSize = 96, double angle = 0, PixelFormat pixelFormat = PixelFormat.Format1bppIndexed, GIColor bgColor = GIColor.White, GIColor fgColor = GIColor.Black, double pointSize = 0.2, double lineWidth = 0.2, bool shouldFill = true, bool shouldCloseGeometries = false, double closureTolerance = 0.0001)
        {
            var geometriesIn = new List<IMarkGeometry>();
            var labels = new List<string>();

            if (shouldCloseGeometries)
            {
                List<MarkGeometryPath> openGeometries = new List<MarkGeometryPath>();
                List<IMarkGeometry> closedGeometries = new List<IMarkGeometry>();

                foreach (var kv in GeometricArithmeticModule.ExtractLabelledGeometriesFromDXF(dxfFilePath, layerNames))
                {
                    for (int i = 0; i < kv.Value.Count; i++)
                    {
                        if (kv.Value[i] is MarkGeometryLine line)
                        {
                            openGeometries.Add(new MarkGeometryPath(line));
                        }
                        else if (kv.Value[i] is MarkGeometryArc arc)
                        {
                            if (Math.Abs(arc.Sweep % (2 * Math.PI)) <= 0.0001)
                                closedGeometries.Add(new MarkGeometryPath(arc));
                            else
                                openGeometries.Add(new MarkGeometryPath(arc));
                        }
                        else if (kv.Value[i] is MarkGeometryPath path)
                        {
                            if (path.IsClosed)
                                closedGeometries.Add(path);
                            else
                                openGeometries.Add(path);
                        }
                        else
                        {
                            closedGeometries.Add(kv.Value[i]);
                        }
                    }
                }

                closedGeometries.AddRange(
                    GeometricArithmeticModule.Simplify(openGeometries, closureTolerance)
                );

                geometriesIn = closedGeometries;
            }
            else
            {
                foreach (var kv in GeometricArithmeticModule.ExtractLabelledGeometriesFromDXF(dxfFilePath, layerNames))
                {
                    labels.Add(kv.Key);
                    geometriesIn.AddRange(kv.Value);
                }
            }

            var geometriesAsTrees = MarkGeometryTree.FromGeometries(geometriesIn, bgColor == GIColor.Black ? Color.Black : Color.White, fgColor == GIColor.Black ? Color.Black : Color.White);

            var scaleFactorX = (dpiX / pixelSize);
            var scaleFactorY = (dpiY / pixelSize);
            var scale = 0.5 * (scaleFactorX + scaleFactorY);
            var geometriesAsTreesExtents = GeometricArithmeticModule.CalculateExtents(geometriesAsTrees);

            var transformMatrix = GeometricArithmeticModule.CombineTransformations(
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    -geometriesAsTreesExtents.Centre.X,
                    -geometriesAsTreesExtents.Centre.Y,
                    -geometriesAsTreesExtents.Centre.Z
                ),
                GeometricArithmeticModule.GetScalingTransformationMatrix(
                    scaleFactorX,
                    -scaleFactorY,
                    1
                ),
                GeometricArithmeticModule.GetRotationTransformationMatrix(
                    0,
                    0,
                    GeometricArithmeticModule.ToRadians(
                        angle
                    )
                ),
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    geometriesAsTreesExtents.Centre.X,
                    geometriesAsTreesExtents.Centre.Y,
                    geometriesAsTreesExtents.Centre.Z
                )
            );

            // apply transform to matrix
            for (int i = 0; i < geometriesAsTrees.Count; i++)
                geometriesAsTrees[i].Transform(transformMatrix);

            var geometries = GeometricArithmeticModule.AlignTopLeftToOrigin(geometriesAsTrees);

            var extents = GeometricArithmeticModule.CalculateExtents(geometries);

            int imageWidth = (int)Math.Round(extents.Width);
            int imageHeight = (int)Math.Round(extents.Height);

            var image = new Image<Gray, Byte>(imageWidth, imageHeight);
            var bitmap = new Bitmap(imageWidth, imageHeight, pixelFormat);

            var __bgColor = bgColor == GIColor.Black ? Color.Black : Color.White;
            var __fillColor = fgColor == GIColor.Black ? Color.Black : Color.White;

            // Draw background
            image.SetValue(ToGray(__bgColor));

            for (int i = 0; i < geometries.Count; i++)
            {
                if (geometries[i] is MarkGeometryTree tree)
                {
                    tree.BeginGetAll((geometry) =>
                    {
                        DrawGeometry(image, geometry, scale, pointSize, lineWidth, __bgColor, __fillColor);
                        return true;
                    });
                }
                else
                {
                    DrawGeometry(image, geometries[i], scale, pointSize, lineWidth, __bgColor, __fillColor);
                }
            }

            FastCopy(image, bitmap);
            return (bitmap, geometriesAsTreesExtents, labels, geometriesIn.Count);
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

            angle = -1 * GeometricArithmeticModule.ToRadians(angle);
            for (int i = 0; i < geometriesIn.Count; i++)
            {
                GeometricArithmeticModule.Rotate(geometriesIn[i], 0, 0, angle, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z);
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
            int pointThickness = (int)(scale * pointSize);

            for (int i = 0; i < geometries.Length; i++)
            {
                if (geometries[i] is MarkGeometryPoint point)
                {
                    image.Draw(new CircleF(point, pointThickness), __fillColor, -1);
                }
                else if (geometries[i] is MarkGeometryLine line)
                {
                    image.Draw(line, __fillColor, thickness);
                }
                else if (geometries[i] is MarkGeometryCircle circle)
                {
                    if (shouldFill)
                        image.Draw(new CircleF(new PointF((float)circle.CentrePoint.X, (float)circle.CentrePoint.Y), (float)(circle.Radius)), __fillColor, -1);
                    else
                        image.Draw(new CircleF(new PointF((float)circle.CentrePoint.X, (float)circle.CentrePoint.Y), (float)(circle.Radius)), __fillColor, thickness);
                }
                else if (geometries[i] is MarkGeometryArc arc)
                {
                    DrawPath(image, new MarkGeometryPath(arc), __fillColor, thickness);
                }
                else if (geometries[i] is MarkGeometryPath path)
                {
                    DrawPath(image, path, __fillColor, thickness, shouldFill);
                }
            }

            FastCopy(image, bitmap);
            return (bitmap, geometriesInExtents, labels, geometriesIn.Count);
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
                    image.Draw(new CircleF(new PointF((float)circle.CentrePoint.X, (float)circle.CentrePoint.Y), (float)(circle.Radius)), __fillColor, -1);
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
    }
}

#region Section: Not Used
//namespace GenericTestProject
//{
//    public static class GeometryToImageConverter
//    {
//        #region Section: Helpers
//        public enum OptimisationSetting
//        {
//            Default = 0,
//            Speed = 1,
//            HighestQuality = 2,
//            Compressed = 3
//        }

//        public enum GIColor
//        {
//            Black = 0,
//            White = 1
//        }

//        private static void DrawPath(Image<Gray, Byte> image, MarkGeometryPath path, Gray fillColor, int thickness, bool shouldFill = true)
//        {
//            var points = new List<Point>();

//            foreach (var point in (MarkGeometryPoint[])path)
//            {
//                points.Add(point);
//            }

//            if (shouldFill && path.IsClosed)
//            {
//                image.Draw(points.ToArray(), fillColor, -1);
//            }
//            else
//            {
//                image.DrawPolyline(points.ToArray(), false, fillColor, thickness);
//            }
//        }

//        private static void FastCopy(Image<Gray, Byte> image, Bitmap bitmapIn)
//        {
//            if (bitmapIn.PixelFormat == PixelFormat.Format1bppIndexed)
//            {
//                FastCopy1Bpp(image, bitmapIn);
//            }
//            else if (bitmapIn.PixelFormat == PixelFormat.Format4bppIndexed)
//            {
//                FastCopy4Bpp(image, bitmapIn);
//            }
//            else
//            {
//                throw new Exception("Pixel Format is not supported");
//            }
//        }

//        private static void FastCopy1Bpp(Image<Gray, Byte> image, Bitmap bitmapIn)
//        {
//            var imgMIpl = image.MIplImage;
//            long imgStride = imgMIpl.WidthStep;

//            var bmpImageData = bitmapIn.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, bitmapIn.PixelFormat);
//            var bmpStride = bmpImageData.Stride;

//            long pxIndex = 0;
//            int _bmpSW = bitmapIn.Width / 8;

//            unsafe
//            {
//                byte* bmpPtr = (byte*)bmpImageData.Scan0.ToPointer();
//                byte* imgPtr = (byte*)imgMIpl.ImageData.ToPointer();

//                for (long row = 0; row < bitmapIn.Height; row++)
//                {
//                    for (long col = 0; col <= _bmpSW; col++)
//                    {
//                        pxIndex = (row * imgStride) + (col * 8);
//                        bmpPtr[(row * bmpStride) + col] = compress(imgPtr[pxIndex], imgPtr[pxIndex + 1], imgPtr[pxIndex + 2], imgPtr[pxIndex + 3], imgPtr[pxIndex + 4], imgPtr[pxIndex + 5], imgPtr[pxIndex + 6], imgPtr[pxIndex + 7]);
//                    }
//                }
//            }

//            bitmapIn.UnlockBits(bmpImageData);
//        }

//        private static void FastCopy4Bpp(Image<Gray, Byte> image, Bitmap bitmapIn)
//        {
//            var imgMIpl = image.MIplImage;
//            long imgStride = imgMIpl.WidthStep;

//            var bmpImageData = bitmapIn.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, bitmapIn.PixelFormat);
//            var bmpStride = bmpImageData.Stride;

//            long pxIndex = 0;
//            int _bmpSW = bitmapIn.Width / 2;

//            unsafe
//            {
//                byte* bmpPtr = (byte*)bmpImageData.Scan0.ToPointer();
//                byte* imgPtr = (byte*)imgMIpl.ImageData.ToPointer();

//                for (long row = 0; row < bitmapIn.Height; row++)
//                {
//                    for (long col = 0; col <= _bmpSW; col++)
//                    {
//                        pxIndex = (row * imgStride) + (col * 2);
//                        bmpPtr[(row * bmpStride) + col] = compress(imgPtr[pxIndex], imgPtr[pxIndex + 1]);
//                    }
//                }
//            }

//            bitmapIn.UnlockBits(bmpImageData);
//        }

//        private static byte compress(int c1, int c2)
//        {
//            const byte mask = 0x0f;
//            return (byte)(((c1 & mask) << 4) | (c2 & mask));
//        }

//        private static byte compress(int c1, int c2, int c3, int c4, int c5, int c6, int c7, int c8)
//        {
//            const byte mask = 0x01;
//            return (byte)(((c1 & mask) << 7) | ((c2 & mask) << 6) | ((c3 & mask) << 5) | ((c4 & mask) << 4) | ((c5 & mask) << 3) | ((c6 & mask) << 2) | ((c7 & mask) << 1) | (c8 & mask));
//        }

//        public static bool SaveAsBitmap(string fileName, Bitmap bitmapIn, double dpiX, double dpiY, OptimisationSetting optimisationIn, long compressionQuality = 100L)
//        {
//            EncoderParameters encoderParameters;
//            bitmapIn.SetResolution((float)dpiX, (float)dpiY);

//            switch (optimisationIn)
//            {
//                case OptimisationSetting.HighestQuality:
//                    encoderParameters = new EncoderParameters(2);
//                    encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, compressionQuality);
//                    encoderParameters.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionNone);

//                    if (fileName.EndsWith("bmp"))
//                    {
//                        bitmapIn.Save(fileName, GetEncoderInfo("image/bmp"), encoderParameters);
//                    }
//                    else if (fileName.EndsWith("tiff") || fileName.EndsWith("tif"))
//                    {
//                        bitmapIn.Save(fileName, GetEncoderInfo("image/tiff"), encoderParameters);
//                    }
//                    else if (fileName.EndsWith("jpg") || fileName.EndsWith("jpeg"))
//                    {
//                        bitmapIn.Save(fileName, GetEncoderInfo("image/jpeg"), encoderParameters);
//                    }
//                    else if (fileName.EndsWith("png"))
//                    {
//                        bitmapIn.Save(fileName, GetEncoderInfo("image/png"), encoderParameters);
//                    }
//                    else if (fileName.EndsWith("gif"))
//                    {
//                        bitmapIn.Save(fileName, GetEncoderInfo("image/gif"), encoderParameters);
//                    }
//                    else
//                    {
//                        return false;
//                    }
//                    break;

//                case OptimisationSetting.Compressed:
//                case OptimisationSetting.Speed:
//                case OptimisationSetting.Default:
//                default:
//                    if (fileName.EndsWith("bmp"))
//                    {
//                        bitmapIn.Save(fileName, ImageFormat.Bmp);
//                    }
//                    else if (fileName.EndsWith("tiff") || fileName.EndsWith("tif"))
//                    {
//                        encoderParameters = new EncoderParameters(2);
//                        encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, compressionQuality);
//                        encoderParameters.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);
//                        bitmapIn.Save(fileName, GetEncoderInfo("image/tiff"), encoderParameters);
//                    }
//                    else if (fileName.EndsWith("jpg") || fileName.EndsWith("jpeg"))
//                    {
//                        bitmapIn.Save(fileName, ImageFormat.Jpeg);
//                    }
//                    else if (fileName.EndsWith("png"))
//                    {
//                        bitmapIn.Save(fileName, ImageFormat.Png);
//                    }
//                    else if (fileName.EndsWith("gif"))
//                    {
//                        bitmapIn.Save(fileName, ImageFormat.Gif);
//                    }
//                    else
//                    {
//                        return false;
//                    }
//                    break;
//            }

//            return true;
//        }

//        private static ImageCodecInfo GetEncoderInfo(String mimeType)
//        {
//            int j;
//            ImageCodecInfo[] encoders;
//            encoders = ImageCodecInfo.GetImageEncoders();
//            for (j = 0; j < encoders.Length; ++j)
//            {
//                if (encoders[j].MimeType == mimeType)
//                    return encoders[j];
//            }
//            return null;
//        }


//        private static Gray ToGray(Color color)
//        {
//            return color == Color.White ? new Gray(255) : new Gray(0);
//        }

//        private static void DrawGeometry(Image<Gray, Byte> image, IMarkGeometry geometry, double scale, double pointSize, double lineWidth, Color bgColor, Color fgColor, bool shouldFill = true)
//        {
//            if (geometry is MarkGeometryPoint point)
//            {
//                image.Draw(new CircleF(point, (float)(scale * pointSize)), ToGray(fgColor), -1);
//            }
//            else if (geometry is MarkGeometryLine line)
//            {
//                image.Draw(line, ToGray(fgColor), (int)(scale * lineWidth));
//            }
//            else if (geometry is MarkGeometryCircle circle)
//            {
//                if (shouldFill)
//                    image.Draw(new CircleF(new PointF((float)circle.CentrePoint.X, (float)circle.CentrePoint.Y), (float)(circle.Radius)), ToGray((Color)circle.Fill), -1, LineType.Filled);
//                else
//                    image.Draw(new CircleF(new PointF((float)circle.CentrePoint.X, (float)circle.CentrePoint.Y), (float)(circle.Radius)), ToGray((Color)circle.Fill), (int)(scale * lineWidth), LineType.AntiAlias);
//            }
//            else if (geometry is MarkGeometryArc arc)
//            {
//                DrawPath(image, new MarkGeometryPath(arc), ToGray(fgColor), (int)(scale * lineWidth));
//            }
//            else if (geometry is MarkGeometryPath path)
//            {
//                DrawPath(image, path, ToGray((Color)path.Fill), (int)(scale * lineWidth), shouldFill);
//            }
//        } 
//        #endregion



//        public static (bool success, int width, int height) To1BppImageComplex(string dxfFilePath, string imageFilePath, double dpiX = 120, double dpiY = 120, double pixelSize = 96, double angle = 0, PixelFormat pixelFormat = PixelFormat.Format1bppIndexed, OptimisationSetting optimisationSetting = OptimisationSetting.Speed, GIColor bgColor = GIColor.White, GIColor fgColor = GIColor.Black, double pointSize = 0.2, double lineWidth = 0.2)
//        {
//            var geometriesIn = GeometricArithmeticModule.ExtractGeometriesFromDXF(dxfFilePath);

//            var scaleFactorX = (dpiX / pixelSize);
//            var scaleFactorY = (dpiY / pixelSize);
//            var scale = 0.5 * (scaleFactorX + scaleFactorY);
//            var geometriesInExtents = GeometricArithmeticModule.CalculateExtents(geometriesIn);

//            angle = GeometricArithmeticModule.ToRadians(angle);

//            for (int i = 0; i < geometriesIn.Count; i++)
//            {
//                GeometricArithmeticModule.Rotate(geometriesIn[i], 0, 0, angle, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z);
//            }

//            var geometries = GeometricArithmeticModule.AlignTopLeftToOrigin(geometriesIn.ConvertAll(g => GeometricArithmeticModule.Scale(((IMarkGeometry)g.Clone()), scaleFactorX, scaleFactorY, 1, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z)).ToArray());
//            var extents = GeometricArithmeticModule.CalculateExtents(geometries);

//            int imageWidth = (int)Math.Ceiling(extents.Width);
//            int imageHeight = (int)Math.Ceiling(extents.Height);

//            var image = new Image<Gray, Byte>(imageWidth, imageHeight);
//            var bitmap = new Bitmap(imageWidth, imageHeight, pixelFormat);

//            var __bgColor = bgColor == GIColor.Black ? Color.Black : Color.White;
//            var __fillColor = fgColor == GIColor.Black ? Color.Black : Color.White;

//            // Draw background
//            image.SetValue(ToGray(__bgColor));

//            var items = MarkGeometryTree.FromGeometries(new List<IMarkGeometry>(geometries), bgColor == GIColor.Black ? Color.Black : Color.White, fgColor == GIColor.Black ? Color.Black : Color.White);

//            for (int i = 0; i < items.Count; i++)
//            {
//                if (items[i] is MarkGeometryTree tree)
//                {
//                    tree.BeginGetAll((geometry) =>
//                    {
//                        DrawGeometry(image, geometry, scale, pointSize, lineWidth, __bgColor, __fillColor);
//                        return true;
//                    });
//                }
//                else
//                {
//                    DrawGeometry(image, items[i], scale, pointSize, lineWidth, __bgColor, __fillColor);
//                }
//            }

//            #region Section: Save as Uncompressed - takes a longer time
//            //var tiffWriter = new TiffWriter<Gray, Byte>(outImageName);
//            //tiffWriter.WriteImage(image); 
//            #endregion

//            #region Section: Save as Losslessly compressed - takes a shorter time

//            //image.Save(outImageName);

//            #endregion

//            FastCopy(image, bitmap);
//            SaveAsBitmap(imageFilePath, bitmap, dpiX, dpiY, optimisationSetting);

//            return (true, imageWidth, imageHeight);
//        }



//        public static (bool success, int width, int height) To1BppImageComplex(string dxfFilePath, string[] imageFilePaths, double dpiX = 120, double dpiY = 120, double pixelSize = 96, double angle = 0, PixelFormat pixelFormat = PixelFormat.Format1bppIndexed, OptimisationSetting optimisationSetting = OptimisationSetting.Speed, GIColor bgColor = GIColor.White, GIColor fgColor = GIColor.Black, double pointSize = 0.2, double lineWidth = 0.2)
//        {
//            var geometriesIn = GeometricArithmeticModule.ExtractGeometriesFromDXF(dxfFilePath);

//            var scaleFactorX = (dpiX / pixelSize);
//            var scaleFactorY = (dpiY / pixelSize);
//            var scale = 0.5 * (scaleFactorX + scaleFactorY);
//            var geometriesInExtents = GeometricArithmeticModule.CalculateExtents(geometriesIn);

//            angle = GeometricArithmeticModule.ToRadians(angle);
//            foreach (var geometry in geometriesIn)
//            {
//                GeometricArithmeticModule.Rotate(geometry, 0, 0, angle, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z);
//            }

//            var geometries = GeometricArithmeticModule.AlignTopLeftToOrigin(geometriesIn.ConvertAll(g => GeometricArithmeticModule.Scale(((IMarkGeometry)g.Clone()), scaleFactorX, scaleFactorY, 1, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z)).ToArray());
//            var extents = GeometricArithmeticModule.CalculateExtents(geometries);

//            int imageWidth = (int)Math.Ceiling(extents.Width);
//            int imageHeight = (int)Math.Ceiling(extents.Height);

//            var image = new Image<Gray, Byte>(imageWidth, imageHeight);
//            var bitmap = new Bitmap(imageWidth, imageHeight, pixelFormat);

//            var __bgColor = bgColor == GIColor.Black ? Color.Black : Color.White;
//            var __fillColor = fgColor == GIColor.Black ? Color.Black : Color.White;

//            // Draw background
//            image.SetValue(ToGray(__bgColor));

//            var items = MarkGeometryTree.FromGeometries(new List<IMarkGeometry>(geometries), bgColor == GIColor.Black ? Color.Black : Color.White, fgColor == GIColor.Black ? Color.Black : Color.White);

//            foreach (var item in items)
//            {
//                if (item is MarkGeometryTree tree)
//                {
//                    tree.BeginGetAll((geometry) =>
//                    {
//                        DrawGeometry(image, geometry, scale, pointSize, lineWidth, __bgColor, __fillColor);
//                        return true;
//                    });
//                }
//                else
//                {
//                    DrawGeometry(image, item, scale, pointSize, lineWidth, __bgColor, __fillColor);
//                }
//            }

//            #region Section: Save as Uncompressed - takes a longer time
//            //var tiffWriter = new TiffWriter<Gray, Byte>(outImageName);
//            //tiffWriter.WriteImage(image); 
//            #endregion

//            #region Section: Save as Losslessly compressed - takes a shorter time

//            //image.Save(outImageName);

//            #endregion

//            FastCopy(image, bitmap);
//            foreach(var imageFilePath in imageFilePaths)
//            {
//                SaveAsBitmap(imageFilePath, bitmap, dpiX, dpiY, optimisationSetting);
//            }

//            return (true, imageWidth, imageHeight);
//        }

//        #region Section: Obsolete
//        //public static (Bitmap Bitmap, GeometryExtents<double> Extents, List<string> Labels, int Count) Get1BppImageComplex(string dxfFilePath, string[] layerNames, double dpiX = 120, double dpiY = 120, double pixelSize = 96, double angle = 0, PixelFormat pixelFormat = PixelFormat.Format1bppIndexed, GIColor bgColor = GIColor.White, GIColor fgColor = GIColor.Black, double pointSize = 0.2, double lineWidth = 0.2, bool shouldFill=true, bool shouldCloseGeometries = false, double closureTolerance = 0.0001)
//        //{
//        //    var geometriesIn = new List<IMarkGeometry>();
//        //    var labels = new List<string>();

//        //    if (shouldCloseGeometries)
//        //    {
//        //        List<MarkGeometryPath> openGeometries = new List<MarkGeometryPath>();
//        //        List<IMarkGeometry> closedGeometries = new List<IMarkGeometry>();

//        //        foreach (var kv in GeometricArithmeticModule.ExtractLabelledGeometriesFromDXF(dxfFilePath, layerNames))
//        //        {
//        //            for (int i = 0; i < kv.Value.Count; i++)
//        //            {
//        //                if (kv.Value[i] is MarkGeometryLine line)
//        //                {
//        //                    openGeometries.Add(new MarkGeometryPath(line));
//        //                }
//        //                else if (kv.Value[i] is MarkGeometryArc arc)
//        //                {
//        //                    if (Math.Abs(arc.Sweep % (2 * Math.PI)) <= 0.0001)
//        //                        closedGeometries.Add(new MarkGeometryPath(arc));
//        //                    else
//        //                        openGeometries.Add(new MarkGeometryPath(arc));
//        //                }
//        //                else if (kv.Value[i] is MarkGeometryPath path)
//        //                {
//        //                    if (path.IsClosed)
//        //                        closedGeometries.Add(path);
//        //                    else
//        //                        openGeometries.Add(path);
//        //                }
//        //                else
//        //                {
//        //                    closedGeometries.Add(kv.Value[i]);
//        //                }
//        //            }
//        //        }

//        //        closedGeometries.AddRange(
//        //            GeometricArithmeticModule.Simplify(openGeometries, closureTolerance)
//        //        );

//        //        geometriesIn = closedGeometries;

//        //        //if (shouldCloseGeometries)
//        //        //{
//        //        //    var lines = geometriesIn.Where(g => g is MarkGeometryLine).Select(g => (MarkGeometryLine)g).ToList();
//        //        //    var (paths, unsedLines) = GeometricArithmeticModule.GeneratePathsFromLineSequence(
//        //        //        lines,
//        //        //        closureTolerance
//        //        //    );

//        //        //    geometriesIn.RemoveAll(x => lines.Contains(x));
//        //        //    geometriesIn.AddRange(paths);
//        //        //    geometriesIn.AddRange(unsedLines);
//        //        //}
//        //    }
//        //    else
//        //    {
//        //        foreach (var kv in GeometricArithmeticModule.ExtractLabelledGeometriesFromDXF(dxfFilePath, layerNames))
//        //        {
//        //            labels.Add(kv.Key);
//        //            geometriesIn.AddRange(kv.Value);
//        //        }
//        //    }

//        //    var scaleFactorX = (dpiX / pixelSize);
//        //    var scaleFactorY = (dpiY / pixelSize);
//        //    var scale = 0.5 * (scaleFactorX + scaleFactorY);
//        //    var geometriesInExtents = GeometricArithmeticModule.CalculateExtents(geometriesIn);

//        //    angle = -1 * GeometricArithmeticModule.ToRadians(angle);

//        //    for (int i = 0; i < geometriesIn.Count; i++)
//        //    {
//        //        GeometricArithmeticModule.Rotate(geometriesIn[i], 0, 0, angle, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z);
//        //    }

//        //    var geometries = GeometricArithmeticModule.AlignTopLeftToOrigin(geometriesIn.ConvertAll(g => GeometricArithmeticModule.Scale(((IMarkGeometry)g.Clone()), scaleFactorX, -scaleFactorY, 1, geometriesInExtents.Centre.X, geometriesInExtents.Centre.Y, geometriesInExtents.Centre.Z)).ToArray());
//        //    var extents = GeometricArithmeticModule.CalculateExtents(geometries);

//        //    int imageWidth = (int)Math.Ceiling(extents.Width);
//        //    int imageHeight = (int)Math.Ceiling(extents.Height);

//        //    var image = new Image<Gray, Byte>(imageWidth, imageHeight);
//        //    var bitmap = new Bitmap(imageWidth, imageHeight, pixelFormat);

//        //    var __bgColor = bgColor == GIColor.Black ? Color.Black : Color.White;
//        //    var __fillColor = fgColor == GIColor.Black ? Color.Black : Color.White;

//        //    // Draw background
//        //    image.SetValue(ToGray(__bgColor));

//        //    var items = MarkGeometryTree.FromGeometries(new List<IMarkGeometry>(geometries), bgColor == GIColor.Black ? Color.Black : Color.White, fgColor == GIColor.Black ? Color.Black : Color.White);

//        //    for (int i = 0; i < items.Count; i++)
//        //    {
//        //        if (items[i] is MarkGeometryTree tree)
//        //        {
//        //            tree.BeginGetAll((geometry) =>
//        //            {
//        //                DrawGeometry(image, geometry, scale, pointSize, lineWidth, __bgColor, __fillColor);
//        //                return true;
//        //            });
//        //        }
//        //        else
//        //        {
//        //            DrawGeometry(image, items[i], scale, pointSize, lineWidth, __bgColor, __fillColor);
//        //        }
//        //    }

//        //    FastCopy(image, bitmap);
//        //    return (bitmap, geometriesInExtents, labels, geometriesIn.Count);
//        //} 
//        #endregion
//    }
//}
#endregion