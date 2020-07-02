using MathNet.Numerics.LinearAlgebra;
using MSolvLib.Classes;
using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using netDxf;
using netDxf.Header;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace MSolvLib.MarkGeometry
{
    /// <summary>
    ///     This describes the euler orientation of the a geometric object
    ///     Given a flat grid, X+ travels towards you, Y travels perpendicular
    ///     to X and sideways parallel to you (right is the positive direction),
    ///     and Z+ travel up and out of the flat grid.
    /// </summary>
    public struct GeometricEulerOrientation
    {
        public double Roll;
        public double Pitch;
        public double Yaw;

        public GeometricEulerOrientation(double roll, double pitch, double yaw)
        {
            Roll = roll;
            Pitch = pitch;
            Yaw = yaw;
        }
    }

    public static class GeometricArithmeticModule
    {
        #region Section: Translation Strings
        
        private static string InvalidFileNameMsg = "Given file path is missing or invalid";
        private static string DXFVersionNotSupported = "The version of DXF provided is not supported use, please use AutoCad2000 and higher";

        #endregion

        private static MatrixBuilder<double> _MB = Matrix<double>.Build;

        /// <summary>
        ///     This constrains a number to the  range specified
        /// </summary>
        /// <param name="number">The number to constrain</param>
        /// <param name="minRange">The minimum permissible number</param>
        /// <param name="maxRange">The maximum permissible number</param>
        /// <returns>Returns the number if its within the specified boundaries, else returns the boundary</returns>
        public static double Constrain(double number, double minRange, double maxRange)
        {
            return Math.Max(Math.Min(number, maxRange), minRange);
        }

        /// <summary>
        ///     Maps/scales a given number within the specified constraints
        /// </summary>
        /// <param name="number">The number to map/scale</param>
        /// <param name="inputMinimum">The minimum possible input number</param>
        /// <param name="inputMaximum">The maximum possible input number</param>
        /// <param name="outputMinimum">The minimum possible output number</param>
        /// <param name="outputMaximum">The maximum possible output number</param>
        /// <returns>The number map/scaled based on the specified constraints</returns>
        public static double Map(double number, double inputMinimum, double inputMaximum, double outputMinimum, double outputMaximum)
        {
            return (number - inputMinimum) * (outputMaximum - outputMinimum) / (inputMaximum - inputMinimum) + outputMinimum;
        }

        /// <summary>
        ///     Calculates the euclidean distance between two points using the
        ///     pythagorean theorem.
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        /// <returns>A double representing the distance between the given points</returns>
        public static double ABSMeasure(MarkGeometryPoint p1, MarkGeometryPoint p2)
        {
            return Math.Sqrt(
                    Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2) + Math.Pow(p2.Z - p1.Z, 2)
                );
        }

        /// <summary>
        ///     Calculates the euclidean distance between two points using the
        ///     pythagorean theorem.
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        /// <returns>A double representing the distance between the given points</returns>
        public static double ABSMeasure2D(MarkGeometryPoint p1, MarkGeometryPoint p2)
        {
            return Math.Sqrt(
                    Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2)
                );
        }

        public static double ABSMeasure2D(MarkGeometryLine line)
        {
            return ABSMeasure2D(line.StartPoint, line.EndPoint);
        }

        /// <summary>
        ///     Calculates the euclidean *displacement* (i.e signed distance) between two points using the
        ///     pythagorean theorem.
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        /// <returns>A double representing the distance between the given points</returns>
        public static double Measure(MarkGeometryPoint p1, MarkGeometryPoint p2)
        {
            // TODO : please review/optimize this solution

            MarkGeometryPoint origin = new MarkGeometryPoint(0, 0, 0);
            double sign = (ABSMeasure(origin, p2) > ABSMeasure(origin, p1)) ? 1.0 : -1.0;

            return sign * Math.Sqrt(
                    Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2) + Math.Pow(p2.Z - p1.Z, 2)
                );
        }

        /// <summary>
        ///     Calculates the euclidean *displacement* (i.e signed distance) between two points using the
        ///     pythagorean theorem.
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        /// <returns>A double representing the distance between the given points</returns>
        public static double Measure2D(MarkGeometryPoint p1, MarkGeometryPoint p2)
        {
            // TODO : please review/optimize this solution

            MarkGeometryPoint origin = new MarkGeometryPoint(0, 0, 0);
            double sign = (ABSMeasure2D(origin, p2) > ABSMeasure2D(origin, p1)) ? 1.0 : -1.0;

            return sign * Math.Sqrt(
                    Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2)
                );
        }

        public static double Measure(MarkGeometryLine line)
        {
            return Measure(line.StartPoint, line.EndPoint);
        }

        public static double Measure2D(MarkGeometryLine line)
        {
            return Measure2D(line.StartPoint, line.EndPoint);
        }

        /// <summary>
        ///     1. Returns 0.0 for points
        ///     2. Returns the length of a line
        ///     3. Returns the perimeter of a circle
        ///     4. Returns the length of an arc
        ///     5. Returns the perimeter of a path
        /// </summary>
        /// <param name="geometry">The geometry to measure</param>
        /// <returns>Returns the appropraite perimeter for the given geometry</returns>
        public static double CalculatePerimeter(IMarkGeometry geometry)
        {
            if (geometry is MarkGeometryPoint point)
            {
                return 0.0;
            }
            else if (geometry is MarkGeometryLine line)
            {
                return ABSMeasure(line.StartPoint, line.EndPoint);
            }
            else if (geometry is MarkGeometryCircle circle)
            {
                return 2 * Math.PI * circle.Radius;
            }
            else if (geometry is MarkGeometryArc arc)
            {
                // N.B In radians [θ/360 * 2πr] => [θ/2π * 2πr] => [θr]
                return arc.Radius * (arc.EndAngle - arc.StartAngle);
            }
            else if (geometry is MarkGeometryPath path)
            {
                double length = 0;

                foreach (MarkGeometryLine ln in path.Lines)
                {
                    length += CalculatePerimeter(ln);
                }

                return length;
            }

            throw new NotSupportedException($"Geometry of type '{geometry.GetType().Name}' is not supported with this function");
        }


        /// <summary>
        ///     1. Calculates the radius distance of a point relative to the origin 0,0,0
        ///     2. OR: Returns half the total length of a line
        ///     3. OR: Returns the radius of circles and arcs
        ///     4. OR: Returns half the length of the geometry's extent's hypotenuse
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns>Returns the radius of the given geometry</returns>
        public static double CalculateRadius(IMarkGeometry geometry)
        {
            if (geometry is MarkGeometryPoint point)
            {
                return Math.Sqrt(
                    Math.Pow(point.X, 2) + Math.Pow(point.Y, 2) + Math.Pow(point.Z, 2)
                );
            }
            else if (geometry is MarkGeometryLine line)
            {
                return 0.5 * line.Length;
            }
            else if (geometry is MarkGeometryArc arc)
            {
                return arc.Radius;
            }
            else if (geometry is MarkGeometryCircle circle)
            {
                return circle.Radius;
            }
            else if (geometry is MarkGeometryPath path)
            {
                return 0.5 * path.Extents.Hypotenuse;
            }
            else if (geometry is MarkGeometriesWrapper wrapper)
            {
                return 0.5 * wrapper.Extents.Hypotenuse;
            }

            throw new NotSupportedException($"Geometry of type '{geometry.GetType().Name}' is not supported with this function");
        }

        /// <summary>
        ///     Calculates the euler orientation of a given vector
        /// </summary>
        /// <param name="point">The vector relative to the origin 0,0,0</param>
        /// <returns>The RPY orientation</returns>
        public static GeometricEulerOrientation CalculateOrientation(MarkGeometryPoint point)
        {
            // TODO : please review/optimize this equation

            GeometricEulerOrientation orientation = new GeometricEulerOrientation(
                    roll: Math.Atan2(point.Z, point.Y),
                    pitch: Math.Atan2(point.Z, point.X),
                    yaw: Math.Atan2(point.Y, point.X)
                );

            return orientation;
        }

        /// <summary>
        ///     Calculate the euler orientation of the given point about the reference origin
        /// </summary>
        /// <param name="p">The point</param>
        /// <param name="origin">The reference origin</param>
        /// <returns>Returns the euler orientation of the given point about the reference origin</returns>
        public static GeometricEulerOrientation CalculateOrientation(MarkGeometryPoint p, MarkGeometryPoint origin)
        {
            double dx = p.X - origin.X;
            double dy = p.Y - origin.Y;
            double dz = p.Z - origin.Z;

            return CalculateOrientation(new MarkGeometryPoint(dx, dy, dz));
        }

        /// <summary>
        ///     1. Returns the euler orientation of the given geometry.
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <returns>Returns the appropraite euler orientation of the given geometry.</returns>
        public static GeometricEulerOrientation CalculateOrientation(IMarkGeometry geometry)
        {
            if (geometry is MarkGeometryPoint point)
            {
                return CalculateOrientation(point);
            }
            else if (geometry is MarkGeometryLine line)
            {
                return CalculateOrientation(line.EndPoint, line.StartPoint);
            }
            else if (geometry is MarkGeometryCircle circle)
            {
                return CalculateOrientation(circle.StartPoint, circle.CentrePoint);
            }
            else if (geometry is MarkGeometryArc arc)
            {
                return CalculateOrientation(arc.StartPoint, arc.CentrePoint);
            }
            else if (geometry is MarkGeometryEllipse ellipse)
            {
                return CalculateOrientation(ellipse.StartPoint, ellipse.CentrePoint);
            }
            else if (geometry is MarkGeometryRectangle rectangle)
            {
                return CalculateOrientation(rectangle.TopLeftPoint, rectangle.TopRightPoint);
            }
            else if (geometry is MarkGeometryCubicBezier cubicBezier)
            {
                return CalculateOrientation(cubicBezier.StartPoint, cubicBezier.EndPoint);
            }
            else if (geometry is MarkGeometryQuadraticBezier quadraticBezier)
            {
                return CalculateOrientation(quadraticBezier.StartPoint, quadraticBezier.EndPoint);
            }
            else if (geometry is MarkGeometrySpline spline)
            {
                return CalculateOrientation(spline.StartPoint, spline.EndPoint);
            }
            else if (geometry is MarkGeometryPath path)
            {
                return CalculateOrientation(new MarkGeometryPoint());
            }
            else if (geometry is MarkGeometriesWrapper wrapper)
            {
                return CalculateOrientation(new MarkGeometryPoint());
            }

            throw new NotSupportedException($"Geometry of type '{geometry.GetType().Name}' is not supported with this function");
        }

        ///// <summary>
        /////     Calculates the 2D gradient of a line
        ///// </summary>
        ///// <param name="line">The line</param>
        ///// <returns>returns the 2D gradient of a line</returns>
        //public static double CalculateGradient2D(MarkGeometryLine line)
        //{
        //    return Math.Tan(line.Orientation.Yaw);
        //}

        /// <summary>
        ///     Generates an identity 3D transformation matrix
        /// </summary>
        /// <returns>Returns a 3D identity transformation matrix</returns>
        public static Matrix<double> GetDefaultTransformationMatrix()
        {
            return _MB.DenseIdentity(4);
        }

        ///// <summary>
        /////     Generates a matrix from the given values.
        ///// </summary>
        ///// <param name="m00">Value at row 0, column 0</param>
        ///// <param name="m01">Value at row 0, column 1</param>
        ///// <param name="m10">Value at row 1, column 0</param>
        ///// <param name="m11">Value at row 1, column 1</param>
        ///// <returns>Returns the matrix generated from the given values</returns>
        //public static Matrix<double> GetMatrix(double m00, double m01, double m10, double m11)
        //{
        //    Matrix<double> m = _MB.DenseIdentity(2);
        //    m[0, 0] = m00;
        //    m[0, 1] = m01;
        //    m[1, 0] = m10;
        //    m[1, 1] = m11;
        //    return m;
        //}

        /// <summary>
        ///     Returns the translation transformation matrix for the given parameters.
        /// </summary>
        /// <param name="tx">The magnitude of transformation in the X</param>
        /// <param name="ty">The magnitude of transformation in the Y</param>
        /// <param name="tz">The magnitude of transformation in the Z</param>
        /// <returns>Returns the translation transformation matrix for the given parameters.</returns>
        public static Matrix<double> GetTranslationTransformationMatrix(double tx, double ty, double tz=0)
        {
            //return new Matrix3D(
            //    1, 0, 0, 0,
            //    0, 1, 0, 0,
            //    0, 0, 1, 0,
            //    tx, ty, tz, 1
            //);

            Matrix<double> m = _MB.DenseIdentity(4);
            m[3, 0] = tx;
            m[3, 1] = ty;
            m[3, 2] = tz;
            return m;
        }

        /// <summary>
        ///     Returns the scaling transformation matrix for the given parameters.
        /// </summary>
        /// <param name="sx">The magnitude of transformation in the X</param>
        /// <param name="sy">The magnitude of transformation in the Y</param>
        /// <param name="sz">The magnitude of transformation in the Z</param>
        /// <returns>Returns the scaling transformation matrix for the given parameters.</returns>
        public static Matrix<double> GetScalingTransformationMatrix(double sx, double sy, double sz = 1)
        {
            //return new Matrix3D(
            //    sx, 0, 0, 0,
            //    0, sy, 0, 0,
            //    0, 0, sz, 0,
            //    0, 0, 0, 1
            //);

            Matrix<double> m = _MB.DenseIdentity(4);
            m[0, 0] = sx;
            m[1, 1] = sy;
            m[2, 2] = sz;
            return m;
        }

        /// <summary>
        ///     Returns the shearing transformation matrix for the given parameters.
        /// </summary>
        /// <param name="sh_xy">The magnitude of transformation on the XY plane</param>
        /// <param name="sh_xz">The magnitude of transformation on the XZ plane</param>
        /// <param name="sh_yx">The magnitude of transformation on the YX plane</param>
        /// <param name="sh_yz">The magnitude of transformation on the YZ plane</param>
        /// <param name="sh_zx">The magnitude of transformation on the ZX plane</param>
        /// <param name="sh_zy">The magnitude of transformation on the ZY plane</param>
        /// <returns>Returns the shearing transformation matrix for the given parameters.</returns>
        public static Matrix<double> GetShearingTransformationMatrix(
            double sh_xy,
            double sh_xz,
            double sh_yx,
            double sh_yz,
            double sh_zx,
            double sh_zy
        )
        {
            //return new Matrix3D(
            //    1, sh_yx, sh_zx, 0,
            //    sh_xy, 1, sh_zy, 0,
            //    sh_xz, sh_yz, 1, 0,
            //    0, 0, 0, 1
            //);

            Matrix<double> m = _MB.DenseIdentity(4);
            m[0, 1] = sh_yx;
            m[0, 2] = sh_zx;
            m[1, 0] = sh_xy;
            m[1, 2] = sh_zy;
            m[2, 0] = sh_xz;
            m[2, 1] = sh_yz;
            return m;
        }

        /// <summary>
        /// Returns the 3D rotation transformation matrix for the given parameters.
        /// </summary>
        /// <param name="rxRad">The magnitude of transformation on the X axis</param>
        /// <param name="ryRad">The magnitude of transformation on the Y axis</param>
        /// <param name="rzRad">The magnitude of transformation on the Z axis</param>
        /// <returns>Returns the rotational transformation matrix for the given parameters.</returns>
        public static Matrix<double> GetRotationTransformationMatrix(double rxRad, double ryRad, double rzRad)
        {
            var sx = Math.Sin(rxRad);
            var cx = Math.Cos(rxRad);

            var sy = Math.Sin(ryRad);
            var cy = Math.Cos(ryRad);

            var sz = Math.Sin(rzRad);
            var cz = Math.Cos(rzRad);

            //var rx = new Matrix3D(
            //    1, 0, 0, 0,
            //    0, cx, -sx, 0,
            //    0, sx, cx, 0,
            //    0, 0, 0, 1
            //);

            //var ry = new Matrix3D(
            //    cy, 0, sy, 0,
            //    0, 1, 0, 0,
            //    -sy, 0, cy, 0,
            //    0, 0, 0, 1
            //);

            //var rz = new Matrix3D(
            //    cz, -sz, 0, 0,
            //    sz, cz, 0, 0,
            //    0, 0, 1, 0,
            //    0, 0, 0, 1
            //);

            Matrix<double> rx = _MB.DenseIdentity(4);
            Matrix<double> ry = _MB.DenseIdentity(4);
            Matrix<double> rz = _MB.DenseIdentity(4);

            rx[1, 1] = cx; rx[1, 2] = -sx;
            rx[2, 1] = sx; rx[2, 2] = cx;

            ry[0, 0] = cy; ry[0, 2] = sy;
            ry[2, 0] = -sy; ry[2, 2] = cy;

            rz[0, 0] = cz; rz[0, 1] = -sz;
            rz[1, 0] = sz; rz[1, 1] = cz;

            return CombineTransformations(rx, ry, rz);
        }

        /// <summary>
        ///     Combines a given list of transformation matrixes.
        /// </summary>
        /// <param name="transformations">The ordered list of transformation matrixes.</param>
        /// <returns>Returns a 3D transformation matrix deveried from the given parameters</returns>
        public static Matrix<double> CombineTransformations(params Matrix<double>[] transformations)
        {
            // TODO : Review combining matrices
            // https://www.mauriciopoppe.com/notes/computer-graphics/transformation-matrices/combining-transformations/

            if (transformations.Length >= 2)
            {
                Matrix<double> result = transformations[0];

                for (int i = 1; i < transformations.Length; i++)
                {
                    result = result * transformations[i];
                }

                return result;
            }

            try
            {
                return transformations[0];
            }
            catch (Exception)
            {
                return GetDefaultTransformationMatrix();
            }
        }

        /// <summary>
        ///     Translates the given geometry by the specifed parameters
        /// </summary>
        /// <param name="geometry">The geometry to translate</param>
        /// <param name="dx">The magnitude of translation on the X axis</param>
        /// <param name="dy">The magnitude of translation on the Y axis</param>
        /// <param name="dz">The magnitude of translation on the Z axis</param>
        /// <returns>Return the geometry translate by the specified magnitudes</returns>
        public static IMarkGeometry Translate(IMarkGeometry geometry, double dx, double dy, double dz = 0)
        {
            geometry.Transform(GetTranslationTransformationMatrix(dx, dy, dz));
            return geometry;
        }

        /// <summary>
        ///     Translates the given geometries by the specifed parameters
        /// </summary>
        /// <param name="geometries">The geometries to translate</param>
        /// <param name="dx">The magnitude of translation on the X axis</param>
        /// <param name="dy">The magnitude of translation on the Y axis</param>
        /// <param name="dz">The magnitude of translation on the Z axis</param>
        /// <returns>Return the geometries translate by the specified magnitudes</returns>
        public static IMarkGeometry[] Translate(IMarkGeometry[] geometries, double dx, double dy, double dz = 0)
        {
            foreach(var geometry in geometries)
            {
                Translate(geometry, dx, dy, dz);
            }

            return geometries;
        }

        /// <summary>
        ///     Scales the given geometry by the specifed parameters
        /// </summary>
        /// <param name="geometry">The geometry to scale</param>
        /// <param name="sx">The magnitude of scalation in the X axis</param>
        /// <param name="sy">The magnitude of scalation in the Y axis</param>
        /// <param name="sz">The magnitude of scalation in the Z axis</param>
        /// <returns>Return the geometry scaled by the specified magnitudes</returns>
        public static IMarkGeometry Scale(IMarkGeometry geometry, double sx, double sy, double sz = 1)
        {
            var dx = geometry.Extents.Centre.X;
            var dy = geometry.Extents.Centre.Y;
            var dz = geometry.Extents.Centre.Z;

            // scale around the origin and translate back to the geometry's centre
            var transformationMatrix = CombineTransformations(
                GetTranslationTransformationMatrix(-dx, -dy, -dz),
                GetScalingTransformationMatrix(sx, sy, sz),
                GetTranslationTransformationMatrix(dx, dy, dz)
            );


            geometry.Transform(transformationMatrix);
            return geometry;
        }

        public static IMarkGeometry Scale(IMarkGeometry geometry, double sx, double sy, double sz, double cx, double cy, double cz)
        {
            // scale around the origin and translate back to the geometry's centre
            var transformationMatrix = CombineTransformations(
                GetTranslationTransformationMatrix(-cx, -cy, -cz),
                GetScalingTransformationMatrix(sx, sy, sz),
                GetTranslationTransformationMatrix(cx, cy, cz)
            );


            geometry.Transform(transformationMatrix);
            return geometry;
        }

        /// <summary>
        ///     Rotates the given geometry by the specifed parameters
        /// </summary>
        /// <param name="geometry">The geometry to scale</param>
        /// <param name="rx">The magnitude of rotation in the X axis</param>
        /// <param name="ry">The magnitude of rotation in the Y axis</param>
        /// <param name="rz">The magnitude of rotation in the Z axis</param>
        /// <returns>Return the geometry scaled by the specified magnitudes</returns>
        public static IMarkGeometry Rotate(IMarkGeometry geometry, double rx, double ry, double rz, double cx, double cy, double cz)
        {
            // rotate around the origin and translate back to the geometry's centre
            var transformationMatrix = CombineTransformations(
                GetTranslationTransformationMatrix(-cx, -cy, -cz),
                GetRotationTransformationMatrix(rx, ry, rz),
                GetTranslationTransformationMatrix(cx, cy, cz)
            );

            geometry.Transform(transformationMatrix);
            return geometry;
        }

        /// <summary>
        ///     Rotates the given geometry by the specifed parameters
        /// </summary>
        /// <param name="geometry">The geometry to scale</param>
        /// <param name="rx">The magnitude of rotation in the X axis</param>
        /// <param name="ry">The magnitude of rotation in the Y axis</param>
        /// <param name="rz">The magnitude of rotation in the Z axis</param>
        /// <returns>Return the geometry scaled by the specified magnitudes</returns>
        public static IMarkGeometry Rotate(IMarkGeometry geometry, double rx, double ry, double rz = 0)
        {
            var dx = geometry.Extents.Centre.X;
            var dy = geometry.Extents.Centre.Y;
            var dz = geometry.Extents.Centre.Z;

            // rotate around the origin and translate back to the geometry's centre
            var transformationMatrix = CombineTransformations(
                GetTranslationTransformationMatrix(-dx, -dy, -dz),
                GetRotationTransformationMatrix(rx, ry, rz),
                GetTranslationTransformationMatrix(dx, dy, dz)
            );

            geometry.Transform(transformationMatrix);
            return geometry;
        }

        /// <summary>
        ///     Transforms the given geometries such that its combined top left position is aligned with the origin.
        /// </summary>
        /// <param name="geometries">The input geometries to align</param>
        /// <returns>The transformed geometries.</returns>
        public static IMarkGeometry[] AlignTopLeftToOrigin(IMarkGeometry[] geometries)
        {
            var extent = CalculateExtents(geometries);

            foreach (var geometry in geometries)
            {
                Translate(geometry, -extent.MinX, -extent.MinY, -extent.MinZ);
            }

            return geometries;
        }

        /// <summary>
        ///     Transforms the given geometries such that its combined centre position is aligned with the origin.
        /// </summary>
        /// <param name="geometries">The input geometries to align</param>
        /// <returns>The transformed geometries.</returns>
        public static IMarkGeometry[] AlignCentreToOrigin(IMarkGeometry[] geometries)
        {
            var extent = CalculateExtents(geometries);
            return AlignCentreToReferencePoint(geometries, extent.Centre);
        }

        /// <summary>
        ///     Transforms the given geometries such that its combined centre position is aligned with the reference point.
        /// </summary>
        /// <param name="geometries">The input geometries to align</param>
        /// <returns>The transformed geometries.</returns>
        public static IMarkGeometry[] AlignCentreToReferencePoint(IMarkGeometry[] geometries, MarkGeometryPoint point)
        {
            foreach (var geometry in geometries)
            {
                Translate(geometry, -point.X, -point.Y, -point.Z);
            }

            return geometries;
        }

        /// <summary>
        ///     Calculates the gradient of a given line.
        /// </summary>
        /// <param name="line">The line of which to calculate the gradient.</param>
        /// <returns>A double representing the calculated gradient</returns>
        public static double CalculateGradient2D(MarkGeometryLine line)
        {
            return Math.Tan(CalculateOrientation(line).Yaw);
        }

        /// <summary>
        ///     Use to calculate the major and minor axes for a given ellipse
        /// </summary>
        /// <param name="centrePoint"></param>
        /// <param name="referencePoint"></param>
        /// <param name="referenceAngle"></param>
        /// <returns></returns>
        public static (double MajorAxis, double MinorAxis) CalculateMajorMinorAxis(MarkGeometryPoint centrePoint, MarkGeometryPoint referencePoint, double referenceAngle)
        {
            double majorAxis = (referencePoint.X - centrePoint.X) / Math.Cos(referenceAngle);
            double minorAxis = (referencePoint.Y - centrePoint.Y) / Math.Sin(referenceAngle);

            return (majorAxis, minorAxis);
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="centrePoint"></param>
        /// <param name="startPoint"></param>
        /// <returns></returns>
        public static double CalculateRadius(MarkGeometryPoint centrePoint, MarkGeometryPoint startPoint)
        {
            return ABSMeasure(centrePoint, startPoint);
        }

        public static MarkGeometryPoint CalculateIntersection2D(MarkGeometryLine l1, MarkGeometryLine l2, double resolution = 0.001)
        {
            double x1 = l1.StartPoint.X;
            double x2 = l1.EndPoint.X;
            double y1 = l1.StartPoint.Y;
            double y2 = l1.EndPoint.Y;

            double x3 = l2.StartPoint.X;
            double x4 = l2.EndPoint.X;
            double y3 = l2.StartPoint.Y;
            double y4 = l2.EndPoint.Y;

            Matrix<double> a = _MB.DenseIdentity(2);
            a[0, 0] = x1; a[0, 1] = y1;
            a[1, 0] = x2; a[1, 1] = y2;

            Matrix<double> b = _MB.DenseIdentity(2);
            b[0, 0] = x1; b[0, 1] = 1d;
            b[1, 0] = x2; b[1, 1] = 1d;

            Matrix<double> c = _MB.DenseIdentity(2);
            c[0, 0] = x3; c[0, 1] = y3;
            c[1, 0] = x4; c[1, 1] = y4;

            Matrix<double> d = _MB.DenseIdentity(2);
            d[0, 0] = x3; d[0, 1] = 1d;
            d[1, 0] = x4; d[1, 1] = 1d;

            Matrix<double> e = _MB.DenseIdentity(2);
            e[0, 0] = y1; e[0, 1] = 1d;
            e[1, 0] = y2; e[1, 1] = 1d;

            Matrix<double> f = _MB.DenseIdentity(2);
            f[0, 0] = y3; f[0, 1] = 1d;
            f[1, 0] = y4; f[1, 1] = 1d;

            Matrix<double> A = _MB.DenseIdentity(2);
            A[0, 0] = a.Determinant(); A[0, 1] = b.Determinant();
            A[1, 0] = c.Determinant(); A[1, 1] = d.Determinant();

            Matrix<double> B = _MB.DenseIdentity(2);
            B[0, 0] = b.Determinant(); B[0, 1] = e.Determinant();
            B[1, 0] = d.Determinant(); B[1, 1] = f.Determinant();

            Matrix<double> C = _MB.DenseIdentity(2);
            C[0, 0] = a.Determinant(); C[0, 1] = e.Determinant();
            C[1, 0] = c.Determinant(); C[1, 1] = f.Determinant();

            double x, y;

            try
            {
                x = A.Determinant() / B.Determinant();
                y = C.Determinant() / B.Determinant();
            }
            catch (DivideByZeroException)
            {
                x = 0;
                y = 0;
            }

            var point = new MarkGeometryPoint(x, y);

            if (IsOnLine2D(point, l1, resolution) && IsOnLine2D(point, l2, resolution))
            {
                return point;
            }

            return null;
        }

        public static List<MarkGeometryPoint> CalculateIntersection2D(MarkGeometryPath path, MarkGeometryLine line, double resolution = 0.001)
        {
            return CalculateIntersection2D(path.Lines, line, resolution);
        }

        public static List<MarkGeometryPoint> CalculateIntersection2D(List<MarkGeometryLine> lines, MarkGeometryLine line, double resolution = 0.001)
        {
            var intersectionPoints = new List<MarkGeometryPoint>();

            foreach(var ln in lines)
            {
                var intersection = CalculateIntersection2D(ln, line, resolution);

                if (intersection != null && !intersectionPoints.Any(x => Compare2D(x, intersection, resolution) == 0))
                {
                    intersectionPoints.Add(intersection);
                }
            }

            return intersectionPoints;
        }

        public static List<MarkGeometryPoint> CalculateIntersection2D(MarkGeometryPath p1, MarkGeometryPath p2, double resolution = 0.001)
        {
            var intersectionPoints = new List<MarkGeometryPoint>();

            foreach(var l1 in p1.Lines)
            {
                foreach(var l2 in p2.Lines)
                {
                    var intersection = CalculateIntersection2D(l1, l2);
                    if (intersection != null)
                    {
                        intersectionPoints.Add(intersection);
                    }
                }
            }

            return intersectionPoints;
        }

        /// <summary>
        ///     Returns an interpolated point on the specified position given the QuadraticBezier parameters.
        /// </summary>
        /// <param name="startPoint">The start point</param>
        /// <param name="endPoint">The end point</param>
        /// <param name="controlPoint">The control point</param>
        /// <param name="position">The position on the curve between 0 and 1.0</param>
        /// <returns>Returns a point interpolated from the given QuadraticBezier parameters</returns>
        public static MarkGeometryPoint GetPointAtPosition(MarkGeometryPoint startPoint, MarkGeometryPoint endPoint, MarkGeometryPoint controlPoint, double position)
        {
            // position must be between 0 - 1
            position = position % 1.00277777;

            var a = startPoint * Math.Pow(1 - position, 2);
            var b = controlPoint * position * 2 * (1 - position);
            var c = endPoint * Math.Pow(position, 2);

            return a + b + c;
        }

        /// <summary>
        ///     Returns an interpolated point on the specified position given the CubicBezier parameters
        /// </summary>
        /// <param name="startPoint">The start point</param>
        /// <param name="endPoint">The end point</param>
        /// <param name="controlPoint_1">The first control point</param>
        /// <param name="controlPoint_2">The second control point</param>
        /// <param name="position">The position on the curve between 0 and 1.0</param>
        /// <returns>Returns a point interpolated from the given CubicBezier parameters</returns>
        public static MarkGeometryPoint GetPointAtPosition(MarkGeometryPoint startPoint, MarkGeometryPoint endPoint, MarkGeometryPoint controlPoint_1, MarkGeometryPoint controlPoint_2, double position)
        {
            // position must be between 0 - 1
            position = position % 1.00277777;

            var a = startPoint * Math.Pow(1 - position, 3);
            var b = controlPoint_1 * position * 3 * Math.Pow(1 - position, 2);
            var c = controlPoint_2 * Math.Pow(position, 2) * 3 * (1 - position);
            var d = endPoint * Math.Pow(position, 3);

            return a + b + c + d;
        }

        /// <summary>
        ///     Returns an interpolated point on the specified position on a geometry.
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <param name="position">The position on the geometry. 0.0, 0.5, 1.0 represents the start, middle, end positions of the geometry respectively.</param>
        /// <returns>Returns a point interpolated from the given geometry</returns>
        public static MarkGeometryPoint GetPointAtPosition(IMarkGeometry geometry, double position)
        {
            // position must be between 0 - 1
            position = position % 1.00277777;

            if (geometry is MarkGeometryPoint point)
            {
                return point;
            }
            else if (geometry is MarkGeometryLine line)
            {
                return new MarkGeometryPoint(
                    line.StartPoint.X + ((line.EndPoint.X) * position),
                    line.StartPoint.Y + ((line.EndPoint.Y) * position),
                    line.StartPoint.Z + ((line.EndPoint.Z) * position)
                );
            }
            else if (geometry is MarkGeometryCircle circle)
            {
                return GetPointAtAngle(circle.CentrePoint, circle.Radius, Map(position, 0, 1.0, 0, 2 * Math.PI));
            }
            else if (geometry is MarkGeometryArc arc)
            {
                return GetPointAtAngle(arc, arc.StartAngle + (position * (arc.Sweep)));
            }
            else if (geometry is MarkGeometryEllipse ellipse)
            {
                return GetPointAtAngle(ellipse, Map(position, 0, 1.0, ellipse.StartAngle, ellipse.EndAngle));
            }
            else if (geometry is MarkGeometryCubicBezier cubicBezier)
            {
                return GetPointAtPosition(cubicBezier.StartPoint, cubicBezier.EndPoint, cubicBezier.ControlPoint_1, cubicBezier.ControlPoint_2, position);
            }
            else if (geometry is MarkGeometryQuadraticBezier quadraticBezier)
            {
                return GetPointAtPosition(quadraticBezier.StartPoint, quadraticBezier.EndPoint, quadraticBezier.ControlPoint, position);
            }
            else if (geometry is MarkGeometryPath path)
            {
                if (path.Lines.Count() <= 0)
                {
                    throw new Exception("Path must not be empty");
                }

                // optimize for last and first points
                if (Math.Abs(position) <= 0.0001)
                {
                    return path.Lines.First().StartPoint;
                }
                else if (Math.Abs(position) >= 0.9995)
                {
                    return path.Lines.Last().EndPoint;
                }

                double pathLength = CalculatePerimeter(path);
                double lengthToPointPosition = position * pathLength;
                double lengthSoFar = 0;

                foreach (MarkGeometryLine ln in path.Lines)
                {
                    double lengthOfLine = CalculatePerimeter(ln);

                    // if point is on this line
                    if ((lengthSoFar + lengthOfLine) >= lengthToPointPosition)
                    {
                        double localPosition = (((lengthSoFar + lengthOfLine) - lengthToPointPosition) / lengthOfLine);
                        return GetPointAtPosition(ln, localPosition);
                    }

                    lengthSoFar += CalculatePerimeter(ln);
                }

                // return last point
                return path.Lines[path.Lines.Count() - 1].EndPoint;
            }
            else if (geometry is MarkGeometriesWrapper wrapper)
            {
                return GetPointAtPosition(new MarkGeometryPath((MarkGeometryPoint[])wrapper), position);
            }

            throw new NotSupportedException($"Geometry of type '{geometry.GetType().Name}' is not supported with this function");
        }

        /// <summary>
        ///     Use this class method to retrieve any point at the specified major & minor axis from a centre point
        /// </summary>
        /// <param name="centrePoint">The centre point</param>
        /// <param name="majorAxis">The major axis of the ellipse</param>
        /// <param name="minorAxis">The minor axis of the ellipse</param>
        /// <param name="angle">The angle of the point</param>
        /// <returns>A point at the requested position</returns>
        public static MarkGeometryPoint GetPointAtAngle(MarkGeometryPoint centrePoint, double majorAxis, double minorAxis, double angle)
        {
            double x = centrePoint.X + (majorAxis * Math.Cos(angle));
            double y = centrePoint.Y + (minorAxis * Math.Sin(angle));

            return new MarkGeometryPoint(x, y, centrePoint.Z);
        }

        /// <summary>
        ///     Use this class method to retrieve any point at the specified radius from a centre point
        /// </summary>
        /// <param name="centrePoint">The centre point</param>
        /// <param name="radius">The radius of the point</param>
        /// <param name="angle">The angle of the point</param>
        /// <returns>A point at the requested position</returns>
        public static MarkGeometryPoint GetPointAtAngle(MarkGeometryPoint centrePoint, double radius, double angle)
        {
            double x = centrePoint.X + (radius * Math.Cos(angle));
            double y = centrePoint.Y + (radius * Math.Sin(angle));

            return new MarkGeometryPoint(x, y, centrePoint.Z);
        }

        /// <summary>
        ///     Use this class method to retrieve any point at a specified angle on the circle
        /// </summary>
        /// <param name="circle">The circle from which to get the point</param>
        /// <param name="angle">The angle of the point</param>
        /// <returns>A point at the requested position</returns>
        public static MarkGeometryPoint GetPointAtAngle(MarkGeometryCircle circle, double angle)
        {
            return GetPointAtAngle(circle.CentrePoint, circle.Radius, angle);
        }

        /// <summary>
        ///     Use this class method to retrieve any point at a specified angle on the arc
        /// </summary>
        /// <param name="arc">The arc from which to get the point</param>
        /// <param name="angle">The angle of the point</param>
        /// <returns>A point at the requested position</returns>
        public static MarkGeometryPoint GetPointAtAngle(MarkGeometryArc arc, double angle)
        {
            return GetPointAtAngle(arc.CentrePoint, arc.Radius, angle);
        }

        /// <summary>
        ///     Use this class method to retrieve any point at a specified angle on the arc
        /// </summary>
        /// <param name="ellipse">The ellipse from which to get the point</param>
        /// <param name="angle">The angle of the point</param>
        /// <returns>A point at the requested position</returns>
        public static MarkGeometryPoint GetPointAtAngle(MarkGeometryEllipse ellipse, double angle)
        {
            return GetPointAtAngle(ellipse.CentrePoint, ellipse.MajorAxis, ellipse.MinorAxis, angle);
        }

        /// <summary>
        ///     Splits a given geometry into a List of lines.
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <param name="howmany">The number of lines</param>
        /// <returns>Returns a given geometry split into a list of lines</returns>
        public static List<MarkGeometryLine> SplitGeometry(IMarkGeometry geometry, int howmany)
        {
            List<MarkGeometryLine> lines = new List<MarkGeometryLine>();

            for (int i = 0; i < howmany; i++)
            {
                lines.Add(
                    new MarkGeometryLine(
                        GetPointAtPosition(geometry, i / (double)howmany),
                        GetPointAtPosition(geometry, (i + 1) / (double)howmany)
                    )
                );
            }

            return lines;
        }

        /// <summary>
        ///     Gets a list of even spaced points on the given geometry
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <param name="howmany">The number of points</param>
        /// <returns>A list of evnly spaced points on a given geometry</returns>
        public static List<MarkGeometryPoint> GetPointsOnGeometry(IMarkGeometry geometry, int howmany)
        {
            double maxCount = howmany - 1;
            List<MarkGeometryPoint> points = new List<MarkGeometryPoint>();

            for (int i = 0; i < howmany; i++)
            {
                points.Add(
                    GetPointAtPosition(geometry, i / maxCount)
                );
            }

            return points;
        }

        /// <summary>
        ///     Returns the angle shifted by 2PI radians
        /// </summary>
        /// <param name="angle">The angle (in radians) to shift</param>
        /// <returns>A double representing the angle shifted</returns>
        public static double GetPositiveAngle(double angle)
        {
            if (angle < 0)
            {
                return GetPositiveAngle(angle + (Math.PI * 2.0));
            }
            else
            {
                return angle;
            }
        }

        /// <summary>
        ///     Compare two points; Equal -> 0, Less -> -1, Greater -> 1
        /// </summary>
        /// <param name="p1">The first point to compare</param>
        /// <param name="p2">The second point to compare</param>
        /// <param name="precision">The number of decimal places to compare</param>
        /// <returns>A number describing how the two points compare; Equal:0, Less:-1, Greater:-1</returns>
        public static int Compare(MarkGeometryPoint p1, MarkGeometryPoint p2, int precision)
        {
            // TODO : please review/optimize this solution

            double displacement = Math.Round(Measure(p1, p2), precision);

            if (displacement > 0)
            {
                return 1;
            }
            else if (displacement < 0)
            {
                return -1;
            }

            return 0;
        }

        /// <summary>
        ///     Compare two points; Equal -> 0, Less -> -1, Greater -> 1
        /// </summary>
        /// <param name="p1">First point to compare</param>
        /// <param name="p2">Second point to compare</param>
        /// <param name="tolerance">The max. permissible tolerance</param>
        /// <returns>A number describing how the two points compare; Equal:0, Less:-1, Greater:-1</returns>
        public static int Compare(MarkGeometryPoint p1, MarkGeometryPoint p2, double tolerance)
        {
            double diff = Measure(p1, p2);

            if (diff > tolerance)
            {
                return 1;
            }
            else if (diff < -tolerance)
            {
                return -1;
            }

            return 0;
        }

        public static int Compare2D(MarkGeometryPoint p1, MarkGeometryPoint p2, double tolerance)
        {
            double diff = Measure2D(p1, p2);

            if (diff > tolerance)
            {
                return 1;
            }
            else if (diff < -tolerance)
            {
                return -1;
            }

            return 0;
        }

        /// <summary>
        ///     Compare two points; Equal -> 0, Less -> -1, Greater -> 1
        /// </summary>
        /// <param name="p1">The first point to compare</param>
        /// <param name="p2">The second point to compare</param>
        /// <returns>A number describing how the two points compare; Equal:0, Less:-1, Greater:-1</returns>
        public static int Compare(MarkGeometryPoint p1, MarkGeometryPoint p2)
        {
            return Compare(p1, p2, 0);
        }

        /// <summary>
        ///     returns the maximum from a range of given values
        /// </summary>
        /// <typeparam name="T">The type of the given values to compare</typeparam>
        /// <param name="values">The values to compare</param>
        /// <returns>The maximum value from the range of given values</returns>
        public static T Max<T>(params T[] values)
        {
            return Enumerable.Max(values);
        }

        /// <summary>
        ///     returns the minimum from a range of given values
        /// </summary>
        /// <typeparam name="T">The type of the given values to compare</typeparam>
        /// <param name="values">The values to compare</param>
        /// <returns>The minimum value from the range of given values</returns>
        public static T Min<T>(params T[] values)
        {
            return Enumerable.Min(values);
        }

        /// <summary>
        ///     Use to check if point lies on a line.
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <param name="line">Line to check</param>
        /// <param name="resolution">Maximum allowed error</param>
        /// <returns>True is the point lies on line given the resolution</returns>
        public static bool IsOnLine(MarkGeometryPoint point, MarkGeometryLine line, double resolution=0.001)
        {
            // This works based on the principle
            // A--C-----B; where AC,CB,AB are a lines
            // and AC + CB == AB

            var ac = Measure(line.StartPoint, point);
            var cb = Measure(point, line.EndPoint);
            var ab = Measure(line);

            return Math.Abs((ac + cb) - ab) <= resolution;
        }

        /// <summary>
        ///     Use to check if point lies on a line.
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <param name="line">Line to check</param>
        /// <param name="resolution">Maximum allowed error</param>
        /// <returns>True is the point lies on line given the resolution</returns>
        public static bool IsOnLine2D(MarkGeometryPoint point, MarkGeometryLine line, double resolution = 0.001)
        {
            // This works based on the principle
            // A--C-----B; where AC,CB,AB are a lines
            // and AC + CB == AB

            var ac = ABSMeasure2D(line.StartPoint, point);
            var cb = ABSMeasure2D(point, line.EndPoint);
            var ab = ABSMeasure2D(line);

            return Math.Abs((ac + cb) - ab) <= resolution;
        }

        /// <summary>
        ///     Returns true if the given number is within the specified range
        /// </summary>
        /// <param name="number">The number to compare</param>
        /// <param name="minRange">The minimum allowed value for the number</param>
        /// <param name="maxRange">The maximum allowed value for the number</param>
        /// <returns>returns True if number is touching and or between the range specified</returns>
        public static bool IsWithin(double number, double minRange, double maxRange)
        {
            bool result = (number >= minRange) && (number <= maxRange);
            return result;
        }

        /// <summary>
        ///     Checks if point's X position is within the specified range
        /// </summary>
        /// <param name="point">The point to compare</param>
        /// <param name="extents">The limits/range to compare against</param>
        /// <returns>returns True if the point's X value is touching or within the specified limits</returns>
        public static bool IsWithinX(MarkGeometryPoint point, GeometryExtents<double> extents)
        {
            return IsWithin(point.X, extents.MinX, extents.MaxX);
        }

        /// <summary>
        ///     Checks if point's Y position is within the specified range
        /// </summary>
        /// <param name="point">The point to compare</param>
        /// <param name="extents">The limits/range to compare against</param>
        /// <returns>returns True if the point's Y value is touching or within the specified limits</returns>
        public static bool IsWithinY(MarkGeometryPoint point, GeometryExtents<double> extents)
        {
            return IsWithin(point.Y, extents.MinY, extents.MaxY);
        }

        /// <summary>
        ///     Checks if point's Z position is within the specified range
        /// </summary>
        /// <param name="point">The point to compare</param>
        /// <param name="extents">The limits/range to compare against</param>
        /// <returns>returns True if the point's Z value is touching or within the specified limits</returns>
        public static bool IsWithinZ(MarkGeometryPoint point, GeometryExtents<double> extents)
        {
            return IsWithin(point.Z, extents.MinZ, extents.MaxZ);
        }

        /// <summary>
        ///     Checks if a geometries extremes are within the other geometries extremes
        /// </summary>
        /// <param name="geometryAIn">The inner geometry</param>
        /// <param name="geometryBIn">The outer geometry</param>
        /// <returns>True if geometry A is completely within geometry B</returns>
        public static bool IsWithin2D(IMarkGeometry geometryAIn, IMarkGeometry geometryBIn)
        {
            var minA = new MarkGeometryPoint(
                geometryAIn.Extents.MinX,
                geometryAIn.Extents.MinY
            );

            var maxA = new MarkGeometryPoint(
                geometryAIn.Extents.MaxX,
                geometryAIn.Extents.MaxY
            );

            return IsWithin2D(minA, geometryBIn.Extents) && IsWithin2D(maxA, geometryBIn.Extents);
        }

        /// <summary>
        ///     Checks if point's 2D position is within the specified range
        /// </summary>
        /// <param name="point">The point to compare</param>
        /// <param name="extents">The limits/range to compare against</param>
        /// <returns>returns True if the point's 2D position is touching or within the specified limits</returns>
        public static bool IsWithin2D(MarkGeometryPoint point, GeometryExtents<double> extents)
        {
            return IsWithinX(point, extents) && IsWithinY(point, extents);
        }

        //public static bool IsWithin2D(MarkGeometryPoint point, MarkGeometryPath path, double resolution=0.0001)
        //{
        //    var extent = CalculateExtents(new IMarkGeometry[] { point, path });
        //    var testLine = new MarkGeometryLine(point, new MarkGeometryPoint(extent.MaxX, extent.MaxY));

        //    // if the number of intersection is odd, then the point is within
        //    return (CalculateIntersection2D(path, testLine, resolution).Count % 2) != 0;
        //}

        /// <summary>
        ///     Checks if point's position is within the specified range
        /// </summary>
        /// <param name="point">The point to compare</param>
        /// <param name="extents">The limits/range to compare against</param>
        /// <returns>returns True if the point's position is touching or within the specified limits</returns>
        public static bool IsWithin3D(MarkGeometryPoint point, GeometryExtents<double> extents)
        {
            return IsWithinX(point, extents) && IsWithinY(point, extents) && IsWithinZ(point, extents);
        }

        /// <summary>
        ///     Calculates the extents for a group of mark geometries
        /// </summary>
        /// <param name="geometries">The geometries of which to calculate the extents</param>
        /// <returns>The geometry extents of the given mark geometries</returns>
        public static GeometryExtents<double> CalculateExtents(params IMarkGeometry[] geometries)
        {
            var MinX = double.MaxValue;
            var MaxX = double.MinValue;

            var MinY = double.MaxValue;
            var MaxY = double.MinValue;

            var MinZ = double.MaxValue;
            var MaxZ = double.MinValue;

            foreach (var g in geometries)
            {
                MaxX = Max<double>(g.Extents.MaxX, MaxX);
                MaxY = Max<double>(g.Extents.MaxY, MaxY);
                MaxZ = Max<double>(g.Extents.MaxZ, MaxZ);

                MinX = Min<double>(g.Extents.MinX, MinX);
                MinY = Min<double>(g.Extents.MinY, MinY);
                MinZ = Min<double>(g.Extents.MinZ, MinZ);
            }

            var extents = new GeometryExtents<double>();
            extents.MinX = MinX;
            extents.MinY = MinY;
            extents.MinZ = MinZ;
            extents.MaxX = MaxX;
            extents.MaxY = MaxY;
            extents.MaxZ = MaxZ;

            return extents;
        }

        /// <summary>
        ///     Calculates the extents for a group of mark geometries
        /// </summary>
        /// <param name="geometries">The geometries of which to calculate the extents</param>
        /// <returns>The geometry extents of the given mark geometries</returns>
        public static GeometryExtents<double> CalculateExtents(IEnumerable<IMarkGeometry> geometries)
        {
            var MinX = double.MaxValue;
            var MaxX = double.MinValue;

            var MinY = double.MaxValue;
            var MaxY = double.MinValue;

            var MinZ = double.MaxValue;
            var MaxZ = double.MinValue;

            foreach (var g in geometries)
            {
                MaxX = Max<double>(g.Extents.MaxX, MaxX);
                MaxY = Max<double>(g.Extents.MaxY, MaxY);
                MaxZ = Max<double>(g.Extents.MaxZ, MaxZ);

                MinX = Min<double>(g.Extents.MinX, MinX);
                MinY = Min<double>(g.Extents.MinY, MinY);
                MinZ = Min<double>(g.Extents.MinZ, MinZ);
            }

            var extents = new GeometryExtents<double>();
            extents.MinX = MinX;
            extents.MinY = MinY;
            extents.MinZ = MinZ;
            extents.MaxX = MaxX;
            extents.MaxY = MaxY;
            extents.MaxZ = MaxZ;

            return extents;
        }

        public static void LookAheadStepPositionIterationHelper(int loopCount, Func<double, double, bool> handler)
        {
            int maxCount = loopCount - 1;

            for (int i=0; i<maxCount; i++)
            {
                double currPosition = Map(i, 0, maxCount, 0, 1.0);
                double nextPosition = Map(i + 1, 0, maxCount, 0, 1.0);

                handler(currPosition, nextPosition);
            }
        }

        /// <summary>
        ///     Converts an array of points into an enumerable of lines
        /// </summary>
        /// <param name="points">The array of points</param>
        /// <returns>Returns the generated enumerable of lines</returns>
        public static List<MarkGeometryLine> ToLines(params MarkGeometryPoint[] points)
        {
            List<MarkGeometryLine> lines = new List<MarkGeometryLine>();

            for (int i=0; i<points.Length-1; i++)
            {
                lines.Add(new MarkGeometryLine(points[i], points[i + 1]));
            }

            return lines;
        }

        public static List<MarkGeometryPath> SplitByIntersections(MarkGeometryPath p1In, MarkGeometryPath p2In, double resolution=0.0001)
        {
            var intersectingSegments = new List<MarkGeometryPath>();

            var lines = new List<MarkGeometryLine>();
            foreach(var line in p1In.Lines)
            {
                var intersections = CalculateIntersection2D(p2In, line).OrderBy(x => Measure2D(x, line.StartPoint)).ToList();

                if (intersections.Count > 0)
                {
                    lines.Add(new MarkGeometryLine(line.StartPoint, intersections[0]));
                    intersectingSegments.Add(
                        new MarkGeometryPath(lines.ToArray())
                        {
                            Fill = p1In.Fill,
                            Stroke = p1In.Stroke
                        }
                    );

                    lines.Clear();
                    lines = ToLines(intersections.ToArray());

                    foreach(var ln in lines)
                    {
                        intersectingSegments.Add(
                            new MarkGeometryPath(ln)
                            {
                                Fill = p1In.Fill,
                                Stroke = p1In.Stroke
                            }
                        );
                    }
                    
                    lines.Clear();
                    lines.Add(new MarkGeometryLine(intersections[0], line.EndPoint));
                }
                else
                {
                    lines.Add(line);
                }
            }

            if (lines.Count > 0)
            {
                intersectingSegments.Add(
                    new MarkGeometryPath(lines.ToArray())
                    {
                        Fill = p1In.Fill,
                        Stroke = p1In.Stroke
                    }
                );
            }

            return intersectingSegments;
        }

        public static MarkGeometryPath MakeFromUnion2D(MarkGeometryPath p1, MarkGeometryPath p2, double resolution=0.0001)
        {
            var segA = SplitByIntersections(p1, p2, resolution);
            var segB = SplitByIntersections(p2, p1, resolution);

            segA.RemoveAll(x => IsWithin2D(x.Extents.Centre, p2));
            segB.RemoveAll(x => IsWithin2D(x.Extents.Centre, p1));

            var lines = new List<MarkGeometryLine>();

            foreach(var seg in segA)
            {
                lines.AddRange(seg.Lines);
            }

            foreach (var seg in segB)
            {
                lines.AddRange(seg.Lines);
            }

            return new MarkGeometryPath(lines.ToArray())
            {
                Fill = p1.Fill,
                Stroke = p1.Stroke,
                Transparency = p1.Transparency
            };
        }

        public static MarkGeometryPath MakeFromIntersection(MarkGeometryPath p1, MarkGeometryPath p2, double resolution = 0.0001)
        {
            var segA = SplitByIntersections(p1, p2, resolution);
            var segB = SplitByIntersections(p2, p1, resolution);

            segA.RemoveAll(x => !IsWithin2D(x.Extents.Centre, p2));
            segB.RemoveAll(x => !IsWithin2D(x.Extents.Centre, p1));

            var lines = new List<MarkGeometryLine>();

            foreach (var seg in segA)
            {
                lines.AddRange(seg.Lines);
            }

            foreach (var seg in segB)
            {
                lines.AddRange(seg.Lines);
            }

            return new MarkGeometryPath(lines.ToArray())
            {
                Fill = p1.Fill,
                Stroke = p1.Stroke,
                Transparency = p1.Transparency
            };
        }

        public static MarkGeometryPath MakeFromSubtraction(MarkGeometryPath p1, MarkGeometryPath p2, double resolution = 0.0001)
        {
            var segA = SplitByIntersections(p1, p2, resolution);
            segA.RemoveAll(x => IsWithin2D(x.Extents.Centre, p2));

            var segB = SplitByIntersections(p2, p1, resolution);
            segB.RemoveAll(x => !IsWithin2D(x.Extents.Centre, p1));

            var lines = new List<MarkGeometryLine>();

            foreach (var seg in segA)
            {
                lines.AddRange(seg.Lines);
            }

            foreach (var seg in segB)
            {
                lines.AddRange(seg.Lines);
            }

            return new MarkGeometryPath(lines.ToArray())
            {
                Fill = p1.Fill,
                Stroke = p1.Stroke,
                Transparency = p1.Transparency
            };
        }

        public static List<IMarkGeometry> ClipGeometry(IMarkGeometry[] geometriesIn, MarkGeometryRectangle boundaryIn)
        {
            var geometries = new List<IMarkGeometry>();

            foreach (var geometry in geometriesIn)
            {
                var gs = ClipGeometry(geometry, boundaryIn);
                if (gs != null)
                {
                    geometries.AddRange(gs);
                }
            }

            return geometries;
        }

        public static List<IMarkGeometry> ClipGeometry(IMarkGeometry geometryIn, MarkGeometryRectangle boundaryIn)
        {
            if (ABSMeasure2D(geometryIn.Extents.Centre, boundaryIn.Extents.Centre) > (geometryIn.Extents.Hypotenuse + boundaryIn.Extents.Hypotenuse))
            {
                return null;
            }
            else if (IsWithin2D(geometryIn, boundaryIn))
            {
                return new List<IMarkGeometry>() { geometryIn };
            }

            if (geometryIn is MarkGeometryPoint point)
            {
                if (IsWithin2D(point, boundaryIn.Extents))
                {
                    return new List<IMarkGeometry>() { point };
                }
                else
                {
                    return null;
                }
            }
            else if (geometryIn is MarkGeometryLine line)
            {
                var intersections = CalculateIntersection2D(boundaryIn, line);

                if (intersections.Count <= 0)
                {
                    if (IsWithin2D(line.StartPoint, boundaryIn.Extents) && IsWithin2D(line.EndPoint, boundaryIn.Extents))
                    {
                        return new List<IMarkGeometry>() { geometryIn };
                    }
                }
                else if (intersections.Count == 1)
                {
                    if (IsWithin2D(line.StartPoint, boundaryIn.Extents))
                    {
                        return new List<IMarkGeometry>() {
                            new MarkGeometryLine(line.StartPoint, intersections[0])
                            {
                                Fill = line.Fill,
                                Stroke = line.Stroke
                            }
                        };
                    }
                    else if (IsWithin2D(line.EndPoint, boundaryIn.Extents))
                    {
                        return new List<IMarkGeometry>() {
                            new MarkGeometryLine(intersections[0], line.EndPoint)
                            {
                                Fill = line.Fill,
                                Stroke = line.Stroke
                            }
                        };
                    }
                }
                else if (intersections.Count == 2)
                {
                    return new List<IMarkGeometry>() {
                            new MarkGeometryLine(intersections[0], intersections[1])
                            {
                                Fill = line.Fill,
                                Stroke = line.Stroke
                            }
                        };
                }

                return null;
            }
            else if (geometryIn is MarkGeometryPath path)
            {
                var segA = SplitByIntersections(path, boundaryIn);

                var expanded = new MarkGeometryRectangle(boundaryIn.CentrePoint, boundaryIn.Width + 2, boundaryIn.Height + 2);
                segA.RemoveAll(x => !IsWithin2D(x, expanded));
                return segA.ConvertAll<IMarkGeometry>(x => x);
            }
            else if (geometryIn is MarkGeometryCircle circle)
            {
                return ClipGeometry( new MarkGeometryPath(circle), boundaryIn);
            }
            else if (geometryIn is MarkGeometryArc arc)
            {
                return ClipGeometry(new MarkGeometryPath(arc), boundaryIn);
            }
            else if (geometryIn is MarkGeometriesWrapper wrapper)
            {
                var buffer = new List<IMarkGeometry>();

                wrapper.BeginGetAll((reference) =>
                {
                    var foo = ClipGeometry(reference, boundaryIn);
                    if (foo != null)
                    {
                        buffer.AddRange(foo);
                    }
                    return true;
                });

                return buffer;
            }

            return null;
        }

        public static Dictionary<MarkGeometryRectangle, List<IMarkGeometry>> GenerateTiles(IMarkGeometry[] geometriesIn, double tileWidth, double tileHeight, int padding=5)
        {
            var _tiles = new Dictionary<MarkGeometryRectangle, List<IMarkGeometry>>();

            var extents = CalculateExtents(geometriesIn.ToArray());

            if (tileWidth <= 3 || tileHeight <= 3)
            {
                throw new Exception("Tile size cannot be smaller `3`");
            }

            double refWidth = extents.Width + padding;
            double refHeight = extents.Height + padding;

            int nRows = (int)Math.Ceiling(refHeight / tileHeight);
            int nColumns = (int)Math.Ceiling(refWidth / tileWidth);

            var _halfTileWidth = 0.5 * tileWidth;
            var _halfTileHeight = 0.5 * tileHeight;
            var _centre = extents.Centre - new MarkGeometryPoint(0.5 * (nColumns * tileWidth), 0.5 * (nRows * tileHeight));

            for (int row = 0; row < nRows; row++)
            {
                for (int col = 0; col < nColumns; col++)
                {
                    var centrePoint = new MarkGeometryPoint(
                        (col * tileWidth) + _halfTileWidth,
                        (row * tileHeight) + _halfTileHeight
                    );

                    Translate(centrePoint, _centre.X, _centre.Y);

                    var tileBoundary = new MarkGeometryRectangle(centrePoint, tileWidth, tileHeight);

                    var tile = ClipGeometry(geometriesIn.ToArray(), tileBoundary);

                    if (tile?.Count > 0)
                    {
                        _tiles.Add(tileBoundary, tile);
                    }
                }
            }

            return _tiles;
        }

        public static (List<MarkGeometryPath> Paths, List<MarkGeometryLine> UnusedLines) GeneratePathsFromLineSequence(List<MarkGeometryLine> linesIn, double closureToleranceIn = 0.001)
        {
            var paths = new List<MarkGeometryPath>();
            var unUsedLines = new List<MarkGeometryLine>();

            MarkGeometryLine previous = null;
            var buffer = new List<MarkGeometryLine>();

            int counter = 0;
            foreach(var line in linesIn)
            {
                if (previous != null)
                {
                    if (Compare(previous.EndPoint, line.StartPoint, closureToleranceIn) == 0)
                    {
                        buffer.Add(previous);

                        if (counter == linesIn.Count - 1)
                        {
                            buffer.Add(line);
                            paths.Add(
                                new MarkGeometryPath(buffer.ToArray())
                            );
                        }
                    }
                    else if (buffer.Count > 0)
                    {
                        buffer.Add(previous);
                        paths.Add(
                            new MarkGeometryPath(buffer.ToArray())
                        );

                        buffer = new List<MarkGeometryLine>();

                        if (counter == linesIn.Count - 1)
                        {
                            unUsedLines.Add(line);
                        }
                    }
                    else
                    {
                        unUsedLines.Add(previous);

                        if (counter == linesIn.Count - 1)
                        {
                            unUsedLines.Add(line);
                        }
                    }
                }
                else if (counter == linesIn.Count - 1)
                {
                    unUsedLines.Add(line);
                }

                previous = line;
                counter++;
            }

            return (paths, unUsedLines);
        }

        /// <summary>
        ///     Convert angle to degrees
        /// </summary>
        /// <param name="angle">The angle in radians</param>
        /// <returns>A double representing the angle in degrees</returns>
        public static double ToDegrees(double angle)
        {
            return angle * 180.0 / Math.PI;
        }

        /// <summary>
        ///     Convert angle to radians
        /// </summary>
        /// <param name="angle">The angle in degrees</param>
        /// <returns>A double representing the angle in radians</returns>
        public static double ToRadians(double angle)
        {
            return angle * Math.PI / 180.0;
        }

        /// <summary>
        ///     Use function to loop through all geometries within layers of a given DXF file.
        /// </summary>
        /// <param name="filename">Full file path to DXF</param>
        /// <param name="layerNames">Names of layers to load, Reads all layers if null or empty</param>
        /// <param name="callbackIn">Function to handle the input geometries</param>
        public static void BeginExtractLabelledGeometriesFromDXF(string filename, string[] layerNames, Action<(string LayerName, IMarkGeometry Geometry)> callback)
        {
            if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
            {
                throw new FileNotFoundException($"Given file path is missing or invalid: {filename}");
            }

            bool loadAllLayers = (layerNames == null || layerNames.Length <= 0);

            var entities = DXFlibCS.CustomExtractVectorsInOrder(filename);

            foreach (var e in entities)
            {
                if (loadAllLayers || layerNames.Contains(e.Layer.Name))
                {
                    var layerName = e.Layer.Name;

                    if (e is netDxf.Entities.Point point)
                    {
                        callback((layerName, new MarkGeometryPoint(point)));
                    }
                    else if (e is netDxf.Entities.Spline spline)
                    {
                        callback(
                            (layerName, new MarkGeometrySpline(spline))
                        );
                    }
                    else if (e is netDxf.Entities.LwPolyline lwPLine)
                    {
                        callback(
                            (layerName, new MarkGeometryPath(lwPLine))
                        );
                    }
                    else if (e is netDxf.Entities.Polyline pLine)
                    {
                        callback(
                            (layerName, new MarkGeometryPath(pLine))
                        );
                    }
                    else if (e is netDxf.Entities.Line line)
                    {
                        callback(
                            (layerName, new MarkGeometryLine(line))
                        );
                    }
                    else if (e is netDxf.Entities.Circle circle)
                    {
                        callback(
                            (layerName, new MarkGeometryCircle(circle))
                        );
                    }
                    else if (e is netDxf.Entities.Arc arc)
                    {
                        callback(
                            (layerName, new MarkGeometryPath(new MarkGeometryArc(arc)))
                        );
                    }
                }
            }
        }

        /// <summary>
        ///     Extracts the layered geometries from the given DXF file.
        /// </summary>
        /// <param name="filename">The name of the input DXF file</param>
        /// <param name="layerNames"></param>
        /// <returns>A dictionary of labelled dictionaries extracted from the input DXF file.</returns>
        public static Dictionary<string, List<IMarkGeometry>> ExtractLabelledGeometriesFromDXF(string filename, params string[] layerNames)
        {
            var _labelledGeometries = new Dictionary<string, List<IMarkGeometry>>();

            BeginExtractLabelledGeometriesFromDXF(filename, layerNames, (labelledGeometry) =>
            {
                if (_labelledGeometries.ContainsKey(labelledGeometry.LayerName))
                {
                    _labelledGeometries[labelledGeometry.LayerName].Add(labelledGeometry.Geometry);
                }
                else
                {
                    _labelledGeometries[labelledGeometry.LayerName] = new List<IMarkGeometry> { labelledGeometry.Geometry };
                }
            });

            return _labelledGeometries;
        }

        /// <summary>
        ///     Extract the geometeries from the specified layers in the DXF
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="layerNames"></param>
        /// <returns></returns>
        public static List<IMarkGeometry> ExtractGeometriesFromDXF(string filename, params string[] layerNames)
        {
            var _geometries = new List<IMarkGeometry>();

            BeginExtractLabelledGeometriesFromDXF(filename, layerNames, (labelledGeometry) =>
            {
                _geometries.Add(labelledGeometry.Geometry);
            });

            return _geometries;
        }
        /// <summary>
        ///     Writes a dictionary of labelled geometries to a file.
        /// </summary>
        /// <param name="fileName">The name of the output/generated file</param>
        /// <param name="labelledGeometries">A dictionary of labelled geometries</param>
        /// <returns>`True` if file was successfully created else returns `False`</returns>
        public static bool SaveDXF(string fileName, Dictionary<string, IList<IMarkGeometry>> labelledGeometries)
        {
            DxfDocument document = new DxfDocument(new HeaderVariables());

            foreach(var lbo in labelledGeometries)
            {
                foreach(var gm in lbo.Value)
                {
                    document.AddEntity(gm.GetAsDXFEntity(lbo.Key));
                }
            }

            document.Save(fileName);
            return File.Exists(fileName);
        }

        /// <summary>
        ///     Writes the given mark geometries to a DXF file using the specified layer names.
        /// </summary>
        /// <param name="fileName">The name of the output/generated file</param>
        /// <param name="defaultLayerName">The default layer names</param>
        /// <param name="geometries">The geometries to write to file</param>
        /// <returns>`True` if file was successfully created else returns `False`</returns>
        public static bool SaveDXF(string fileName, string defaultLayerName, params IMarkGeometry[] geometries)
        {
            DxfDocument document = new DxfDocument(new HeaderVariables());

            foreach (var gm in geometries)
            {
                document.AddEntity(gm.GetAsDXFEntity(defaultLayerName));
            }

            document.Save(fileName);
            return File.Exists(fileName);
        }

        /// <summary>
        ///     Writes the given mark geometries to a DXF file.
        /// </summary>
        /// <param name="fileName">The name of the output/generated file</param>
        /// <param name="geometries">The geometries to write to file</param>
        /// <returns>`True` if file was successfully created else returns `False`</returns>
        public static bool SaveDXF(string fileName, params IMarkGeometry[] geometries)
        {
            DxfDocument document = new DxfDocument(new HeaderVariables());

            foreach (var gm in geometries)
            {
                document.AddEntity(gm.GetAsDXFEntity());
            }

            document.Save(fileName);
            return File.Exists(fileName);
        }
    }
}
