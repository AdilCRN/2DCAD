using MSolvLib.Classes;
using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using netDxf.Header;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Windows.Media;
using System.Data;
using MSolvLib.HardwareClasses.RTC.RTC5;

namespace MSolvLib.MarkGeometry
{
    public static class GeometricArithmeticModule
    {
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

                for (int i = 0; i < path.Points.Count - 1; i++)
                    length += ABSMeasure(path.Points[i], path.Points[i+1]);

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
        public static Matrix4x4 GetDefaultTransformationMatrix()
        {
            return new Matrix4x4(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );
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
        public static Matrix4x4 GetTranslationTransformationMatrix(double tx, double ty, double tz=0)
        {
            return Matrix4x4.CreateTranslation(
                (float)tx, (float)ty, (float)tz
            );
        }

        /// <summary>
        ///     Returns the translation transformation matrix for the given parameters.
        /// </summary>
        /// <param name="point">A point with its values representing the magnitude of transformation in XYZ</param>
        /// <returns>Returns the translation transformation matrix for the given parameters.</returns>
        public static Matrix4x4 GetTranslationTransformationMatrix(MarkGeometryPoint point)
        {
            return Matrix4x4.CreateTranslation(
                (float)point.X, (float)point.Y, (float)point.Z
            );
        }

        /// <summary>
        ///     Returns the scaling transformation matrix for the given parameters.
        /// </summary>
        /// <param name="sx">The magnitude of transformation in the X</param>
        /// <param name="sy">The magnitude of transformation in the Y</param>
        /// <param name="sz">The magnitude of transformation in the Z</param>
        /// <returns>Returns the scaling transformation matrix for the given parameters.</returns>
        public static Matrix4x4 GetScalingTransformationMatrix(double sx, double sy, double sz = 1)
        {
            return new Matrix4x4(
                (float)sx, 0, 0, 0,
                0, (float)sy, 0, 0,
                0, 0, (float)sz, 0,
                0, 0, 0, 1
            );
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
        public static Matrix4x4 GetShearingTransformationMatrix(
            double sh_xy,
            double sh_xz,
            double sh_yx,
            double sh_yz,
            double sh_zx,
            double sh_zy
        )
        {
            return new Matrix4x4(
                1, (float)sh_yx, (float)sh_zx, 0,
                (float)sh_xy, 1, (float)sh_zy, 0,
                (float)sh_xz, (float)sh_yz, 1, 0,
                0, 0, 0, 1
            );
        }

        /// <summary>
        /// Returns the 3D rotation transformation matrix for the given parameters.
        /// </summary>
        /// <param name="rxRad">The magnitude of transformation on the X axis</param>
        /// <param name="ryRad">The magnitude of transformation on the Y axis</param>
        /// <param name="rzRad">The magnitude of transformation on the Z axis</param>
        /// <returns>Returns the rotational transformation matrix for the given parameters.</returns>
        public static Matrix4x4 GetRotationTransformationMatrix(double rxRad, double ryRad, double rzRad)
        {
            return Matrix4x4.CreateFromYawPitchRoll(
                (float)ryRad,
                (float)rxRad,
                (float)rzRad
            );
        }

        /// <summary>
        ///     see: https://www.johndcook.com/blog/2018/05/05/svd/
        ///     see: https://youtu.be/PjeOmOz9jSY
        ///     see: https://lucidar.me/en/mathematics/calculating-the-transformation-between-two-set-of-points/
        /// </summary>
        /// <param name="inputPoints"></param>
        /// <param name="outputPoints"></param>
        /// <returns></returns>
        public static Matrix4x4 EstimateTransformationMatrixFromPoints(IList<MarkGeometryPoint> inputPoints, IList<MarkGeometryPoint> outputPoints)
        {

            if (inputPoints == null || inputPoints.Count <= 0)
                return GetDefaultTransformationMatrix();

            if (outputPoints == null || outputPoints.Count <= 0)
                return GetDefaultTransformationMatrix();

            var inputMatrix = Matrix<double>.Build.Dense(4, inputPoints.Count);
            for (int i = 0; i < inputPoints.Count; i++)
            {
                inputMatrix[0, i] = inputPoints[i].X;
                inputMatrix[1, i] = inputPoints[i].Y;
                inputMatrix[2, i] = inputPoints[i].Z;
                inputMatrix[3, i] = 1d;
            }

            var outputMatrix = Matrix<double>.Build.Dense(4, outputPoints.Count);
            for (int i = 0; i < inputPoints.Count; i++)
            {
                outputMatrix[0, i] = outputPoints[i].X;
                outputMatrix[1, i] = outputPoints[i].Y;
                outputMatrix[2, i] = outputPoints[i].Z;
                outputMatrix[3, i] = 1d;
            }

            // return the estimated transform
            return ToMatrix4x4(outputMatrix * inputMatrix.PseudoInverse());
        }

        /// <summary>
        ///     Convert a 4x4 Matrix<double> to Matrix4x4.
        /// </summary>
        /// <param name="_matrix">A 4x4 Matrix<double></param>
        /// <returns>A Matrix4x4 representation of the input Matrix<double></returns>
        public static Matrix4x4 ToMatrix4x4(Matrix<double> matrixIn)
        {
            var _matrix = matrixIn.Transpose();
            // copy matrix
            var result = Matrix4x4.Identity;

            // row 1
            result.M11 = (float)_matrix[0, 0];
            result.M12 = (float)_matrix[0, 1];
            result.M13 = (float)_matrix[0, 2];
            result.M14 = (float)_matrix[0, 3];

            // row 2
            result.M21 = (float)_matrix[1, 0];
            result.M22 = (float)_matrix[1, 1];
            result.M23 = (float)_matrix[1, 2];
            result.M24 = (float)_matrix[1, 3];

            // row 3
            result.M31 = (float)_matrix[2, 0];
            result.M32 = (float)_matrix[2, 1];
            result.M33 = (float)_matrix[2, 2];
            result.M34 = (float)_matrix[2, 3];

            // row 4
            result.M41 = (float)_matrix[3, 0];
            result.M42 = (float)_matrix[3, 1];
            result.M43 = (float)_matrix[3, 2];
            result.M44 = (float)_matrix[3, 3];

            return result;
        }

        /// <summary>
        ///     Combines a given list of transformation matrixes.
        /// </summary>
        /// <param name="transformations">The ordered list of transformation matrixes.</param>
        /// <returns>Returns a 3D transformation matrix deveried from the given parameters</returns>
        public static Matrix4x4 CombineTransformations(params Matrix4x4[] transformations)
        {
            // TODO : Review combining matrices
            // https://www.mauriciopoppe.com/notes/computer-graphics/transformation-matrices/combining-transformations/

            if (
                transformations == null || transformations.Length < 0
            )
                return Matrix4x4.Identity;

            var result = transformations[0];

            for(int i=1; i<transformations.Length; i++)
                result = Matrix4x4.Multiply(result, transformations[i]);

            return result;
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
        ///     Transforms the given geometries such that its combined top left position is aligned with the origin.
        /// </summary>
        /// <param name="geometries">The input geometries to align</param>
        /// <returns>The transformed geometries.</returns>
        public static IList<IMarkGeometry> AlignTopLeftToOrigin(IList<IMarkGeometry> geometries)
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
        /// <param name="reference">The reference extents</param>
        /// <returns>The transformed geometries.</returns>
        public static IList<IMarkGeometry> AlignCentreToExtents(IList<IMarkGeometry> geometries, GeometryExtents<double> reference)
        {
            // calculate the extents for the input geometries
            var extent = CalculateExtents(geometries);

            // calcuate the difference between both centres
            var transform = GetTranslationTransformationMatrix(
                reference.Centre.X - extent.Centre.X,
                reference.Centre.Y - extent.Centre.Y
            );

            // in this case, for loops are faster than foreach loops
            for (int i = 0; i < geometries.Count; i++)
                geometries[i].Transform(transform);

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
        ///     Transforms the given geometries such that its combined centre position is aligned with the origin.
        /// </summary>
        /// <param name="geometries">The input geometries to align</param>
        /// <returns>The transformed geometries.</returns>
        public static IList<IMarkGeometry> AlignCentreToOrigin(IList<IMarkGeometry> geometries)
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
            for (int i = 0; i < geometries.Length; i++)
            {
                Translate(geometries[i], -point.X, -point.Y, -point.Z);
            }

            return geometries;
        }

        /// <summary>
        ///     Transforms the given geometries such that its combined centre position is aligned with the reference point.
        /// </summary>
        /// <param name="geometries">The input geometries to align</param>
        /// <returns>The transformed geometries.</returns>
        public static IList<IMarkGeometry> AlignCentreToReferencePoint(IList<IMarkGeometry> geometries, MarkGeometryPoint point)
        {
            for (int i = 0; i < geometries.Count; i++)
            {
                Translate(geometries[i], -point.X, -point.Y, -point.Z);
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
            // TODO: Test implementation
            //return Math.Tan(CalculateOrientation(line).Yaw);

            // This implementation should be faster
            return CalculateGradient2D(line.StartPoint, line.EndPoint);
        }

        /// <summary>
        ///     Calculates the gradient of the line between two points
        /// </summary>
        /// <param name="p1">The start point</param>
        /// <param name="p2">The end point</param>
        /// <returns>A double representing the calculated gradient</returns>
        public static double CalculateGradient2D(MarkGeometryPoint p1, MarkGeometryPoint p2)
        {
            return (p2.Y - p1.Y) / (p2.X - p1.X);
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
            var x1 = (float)l1.StartPoint.X;
            var x2 = (float)l1.EndPoint.X;
            var y1 = (float)l1.StartPoint.Y;
            var y2 = (float)l1.EndPoint.Y;

            var x3 = (float)l2.StartPoint.X;
            var x4 = (float)l2.EndPoint.X;
            var y3 = (float)l2.StartPoint.Y;
            var y4 = (float)l2.EndPoint.Y;

            var a = new Matrix4x4(
                x1, y1, 0, 0,
                x2, y2, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

            var b = new Matrix4x4(
                x1, 1, 0, 0,
                x2, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

            var c = new Matrix4x4(
                x3, y3, 0, 0,
                x4, y4, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

            var d = new Matrix4x4(
                x3, 1, 0, 0,
                x4, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

            var e = new Matrix4x4(
                y1, 1, 0, 0,
                y2, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

            var f = new Matrix4x4(
                y3, 1, 0, 0,
                y4, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

            var A = new Matrix4x4(
                a.GetDeterminant(), b.GetDeterminant(), 0, 0,
                c.GetDeterminant(), d.GetDeterminant(), 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

            var B = new Matrix4x4(
                b.GetDeterminant(), e.GetDeterminant(), 0, 0,
                d.GetDeterminant(), f.GetDeterminant(), 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

            var C = new Matrix4x4(
                a.GetDeterminant(), e.GetDeterminant(), 0, 0,
                c.GetDeterminant(), f.GetDeterminant(), 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

            var point = new MarkGeometryPoint(
                A.GetDeterminant() / B.GetDeterminant(),
                C.GetDeterminant() / B.GetDeterminant()
            );

            if (
                IsOnLine2D(point, l1, resolution) && 
                IsOnLine2D(point, l2, resolution)
            )
                return point;

            return null;
        }

        public static List<MarkGeometryPoint> CalculateIntersection2D(MarkGeometryPath path, MarkGeometryLine line, double resolution = 0.001)
        {
            return CalculateIntersection2D(ToLines(path.Points), line, resolution);
        }

        public static List<MarkGeometryPoint> CalculateIntersection2D(List<MarkGeometryLine> lines, MarkGeometryLine line, double resolution = 0.001)
        {
            var intersectionPoints = new List<MarkGeometryPoint>();

            foreach (var ln in lines)
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

            var pln1 = ToLines(p1.Points);
            var pln2 = ToLines(p2.Points);

            for (int i = 0; i < pln1.Count; i++)
            {
                for (int j = 0; j < pln2.Count; j++)
                {
                    var intersection = CalculateIntersection2D(pln1[i], pln2[j]);
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
                if (path.Points.Count <= 0)
                {
                    throw new Exception("Path must not be empty");
                }

                // optimize for last and first points
                if (Math.Abs(position) <= 0.00001)
                    return path.StartPoint;
                else if (Math.Abs(position) >= 0.99995)
                    return path.EndPoint;

                double pathLength = path.Perimeter;
                double lengthToPointPosition = position * pathLength;
                double lengthSoFar = 0;

                for (int i = 0; i < path.Points.Count-1; i++)
                {
                    double lengthOfSection = ABSMeasure(path.Points[i], path.Points[i+1]);

                    // if point is on this line
                    if ((lengthSoFar + lengthOfSection) >= lengthToPointPosition)
                    {
                        double localPosition = (((lengthSoFar + lengthOfSection) - lengthToPointPosition) / lengthOfSection);
                        return GetPointAtPosition(new MarkGeometryLine(path.Points[i], path.Points[i+1]), localPosition);
                    }

                    lengthSoFar += lengthOfSection;
                }

                // return last point
                return path.EndPoint;
            }
            //else if (geometry is IMarkGeometryWrapper wrapper)
            //{
            //    return GetPointAtPosition(new MarkGeometryPath((MarkGeometryPoint[])wrapper), position);
            //}

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
        ///     Splits a given geometry into a List of points.
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <param name="howmany">The number of points</param>
        /// <returns>Returns a given geometry split into a list of points</returns>
        public static List<MarkGeometryPoint> Explode(IMarkGeometry geometry, int howmany)
        {
            List<MarkGeometryPoint> points = new List<MarkGeometryPoint>();

            for (int i = 0; i < howmany; i++)
            {
                points.Add(
                    GetPointAtPosition(geometry, i / (double)(howmany - 1))
                );
            }

            return points;
        }

        /// <summary>
        ///     Extend a line by a given amount about it's centre
        /// </summary>
        /// <param name="line">The line to extend</param>
        /// <param name="extension">The amount of extension to apply</param>
        /// <returns></returns>
        public static MarkGeometryLine Extend(MarkGeometryLine line, double extension)
        {
            var transform = CombineTransformations(
                GetTranslationTransformationMatrix(
                    -line.Extents.Centre.X,
                    -line.Extents.Centre.Y,
                    -line.Extents.Centre.Z
                ),
                GetRotationTransformationMatrix(
                    0, 0, 
                    -line.Angle
                ),
                GetScalingTransformationMatrix(
                    (line.Length + extension) / line.Length,
                    1, 1
                ),
                GetRotationTransformationMatrix(
                    0, 0,
                    line.Angle
                ),
                GetTranslationTransformationMatrix(
                    line.Extents.Centre.X,
                    line.Extents.Centre.Y,
                    line.Extents.Centre.Z
                )
            );

            line.Transform(transform);
            return line;

            //if (Math.Abs(line.Extents.Width) < 0.0001)
            //{
            //    return (MarkGeometryLine)Scale(
            //        line,
            //        1,
            //        (line.Extents.Height + extension) / Math.Max(line.Extents.Height, double.Epsilon)
            //    );
            //}
            //else if (Math.Abs(line.Extents.Height) < 0.0001)
            //{
            //    return (MarkGeometryLine)Scale(
            //        line,
            //        (line.Extents.Width + extension) / Math.Max(line.Extents.Width, double.Epsilon),
            //        1
            //    );
            //}

            //var xExtension = extension * Math.Cos(line.Angle);
            //var yExtension = extension * Math.Sin(line.Angle);

            //return (MarkGeometryLine)Scale(
            //    line,
            //    (line.Extents.Width + xExtension) / Math.Max(line.Extents.Width, double.Epsilon),
            //    (line.Extents.Height + yExtension) / Math.Max(line.Extents.Height, double.Epsilon)
            //);
        }

        ///// <summary>
        /////     Removes unnecessary joints in path, reducing the 
        /////     number contained of lines.
        ///// </summary>
        ///// <param name="path">The path to simplify</param>
        ///// <param name="deviationTolerance">Maximum gradient difference tolerance</param>
        //public static void Simplify(MarkGeometryPath path, double deviationTolerance = 0.001)
        //{
        //    path.Points = Simplify(path.Points, deviationTolerance);
        //    path.Update();
        //    return;
        //}

        /// <summary>
        ///     Removes unnecessary joints in lines, reducing the 
        ///     number contained of lines.
        ///     
        ///     Assumes lines are order sequencially and are in contact touching
        /// </summary>
        /// <param name="lines">The list of lines to simplify</param>
        /// <param name="deviationTolerance">Maximum gradient difference tolerance</param>
        public static List<MarkGeometryLine> Simplify(IList<MarkGeometryLine> lines, double deviationTolerance = 0.001)
        {
            var output = new List<MarkGeometryLine>(lines.Count);

            if (lines.Count <= 0)
                return output;

            var current = lines[0];
            for (int i = 1; i < lines.Count - 1; i++)
            {
                var angleDelta = Math.Abs(
                    lines[i].Angle - current.Angle
                );

                if (angleDelta <= deviationTolerance)
                {
                    current.EndPoint = lines[i].EndPoint;
                }
                else
                {
                    current.Update();
                    output.Add(current);
                    current = lines[i];
                }
            }

            current.Update();
            output.Add(current);
            return output;
        }

        /// <summary>
        ///     Combines connected paths, reducing the number of contained paths
        /// </summary>
        /// <param name="paths">The list of paths to simplify</param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static List<MarkGeometryPath> Simplify(IList<MarkGeometryPath> paths, double tolerance)
        {
            var output = new List<MarkGeometryPath>(paths.Count);

            while (paths.Count > 0)
            {
                var reference = paths[0];
                paths.RemoveAt(0);

                output.Add(
                    TraceConnected(reference, paths, tolerance).Trace
                );
            }

            return output;
        }

        /// <summary>
        ///     Combines connected lines to form path.
        /// </summary>
        /// <param name="lines">The list of lines</param>
        /// <param name="closureTolerance">The contact comparison tolerance</param>
        /// <returns></returns>
        public static List<MarkGeometryPath> GeneratePathsFromLines(IList<MarkGeometryLine> lines, double closureTolerance)
        {
            var output = new List<MarkGeometryPath>(lines.Count);

            while (lines.Count > 0)
            {
                var reference = lines[0];
                lines.RemoveAt(0);

                output.Add(
                    TraceConnected(reference, lines, closureTolerance).Trace
                );
            }

            return output;
        }

        /// <summary>
        ///     Creates a path from sequencial paths connected to the starting path
        /// </summary>
        /// <param name="start">The start path</param>
        /// <param name="paths">The list of paths to search</param>
        /// <param name="tolerance">The contact comparison tolerance</param>
        /// <returns>The created path and a list of unsed paths</returns>
        public static (MarkGeometryPath Trace, IList<MarkGeometryPath> UnusedPaths) TraceConnected(MarkGeometryPath start, IList<MarkGeometryPath> paths, double tolerance)
        {
            var trace = (MarkGeometryPath)start.Clone();
            var loop = true;

            while (loop)
            {
                if (trace.IsClosed)
                    break;

                var (connectedPath, connectionType) = GetNextConnectedPath(trace, paths, tolerance);

                switch (connectionType)
                {
                    case ConnectionType.START_TO_END: // don't add connected end point
                        trace.Points.InsertRange(0, connectedPath.Points.Take(connectedPath.Points.Count - 1));
                        trace.Update();
                        paths.Remove(connectedPath);
                        break;

                    case ConnectionType.END_TO_START: // don't add connected start point
                        trace.Points.AddRange(connectedPath.Points.Skip(1));
                        trace.Update();
                        paths.Remove(connectedPath);
                        break;

                    case ConnectionType.START_TO_START:
                        connectedPath.Points.Reverse(); // don't add connected end point
                        trace.Points.InsertRange(0, connectedPath.Points.Take(connectedPath.Points.Count - 1));
                        trace.Update();
                        paths.Remove(connectedPath);
                        break;

                    case ConnectionType.END_TO_END:
                        connectedPath.Points.Reverse(); // don't add connected start point
                        trace.Points.AddRange(connectedPath.Points.Skip(1));
                        trace.Update();
                        paths.Remove(connectedPath);
                        break;

                    default:
                    case ConnectionType.NONE:
                        loop = false;
                        break;
                }
            }

            return (trace, paths);
        }

        /// <summary>
        ///     Creates a path from sequencial paths connected to the starting path
        /// </summary>
        /// <param name="start">The start path</param>
        /// <param name="paths">The list of paths to search</param>
        /// <param name="tolerance">The contact comparison tolerance</param>
        /// <returns>The created path and a list of unsed paths</returns>
        public static (MarkGeometryPath Trace, IList<MarkGeometryLine> UnusedLines) TraceConnected(MarkGeometryLine start, IList<MarkGeometryLine> lines, double tolerance)
        {
            var trace = new MarkGeometryPath(start);
            var loop = true;

            while (loop)
            {
                if (trace.IsClosed)
                    break;

                var (connectedLine, connectionType) = GetNextConnectedLine(trace, lines, tolerance);

                switch (connectionType)
                {
                    case ConnectionType.START_TO_END:
                        //trace.Lines.Insert(0, connectedLine);
                        trace.Points.Insert(0, connectedLine.StartPoint);
                        trace.Update();
                        lines.Remove(connectedLine);
                        break;

                    case ConnectionType.END_TO_START:
                        trace.Points.Add(connectedLine.EndPoint);
                        //trace.Lines.Add(connectedLine);
                        trace.Update();
                        lines.Remove(connectedLine);
                        break;

                    case ConnectionType.START_TO_START:
                        trace.Points.Insert(0, connectedLine.EndPoint);
                        //connectedLine.Reverse();
                        //trace.Lines.Insert(0, connectedLine);
                        trace.Update();
                        lines.Remove(connectedLine);
                        break;

                    case ConnectionType.END_TO_END:
                        //connectedLine.Reverse();
                        //trace.Lines.Add(connectedLine);
                        trace.Points.Add(connectedLine.StartPoint);
                        trace.Update();
                        lines.Remove(connectedLine);
                        break;

                    default:
                    case ConnectionType.NONE:
                        loop = false;
                        break;
                }
            }

            return (trace, lines);
        }

        /// <summary>
        ///     Finds and returns the next path from the list of paths 
        ///     connected to the reference path.
        /// </summary>
        /// <param name="reference">The reference path</param>
        /// <param name="paths">The list of paths to search</param>
        /// <param name="tolerance">The contact tolerance</param>
        /// <returns>The connected path and connectiong type</returns>
        public static (MarkGeometryPath ConnectedPath, ConnectionType ConnectionType) GetNextConnectedPath(MarkGeometryPath reference, IList<MarkGeometryPath> paths, double tolerance)
        {
            for (int i = 0; i < paths.Count; i++)
            {
                var connectionType = IsConnected(reference, paths[i], tolerance);
                if (connectionType != ConnectionType.NONE)
                    return (paths[i], connectionType);
            }

            return (null, ConnectionType.NONE);
        }

        /// <summary>
        ///     Finds and returns the next path from the list of paths 
        ///     connected to the reference path.
        /// </summary>
        /// <param name="reference">The reference path</param>
        /// <param name="lines">The list of paths to search</param>
        /// <param name="tolerance">The contact tolerance</param>
        /// <returns>The connected path and connectiong type</returns>
        public static (MarkGeometryLine ConnectedPath, ConnectionType ConnectionType) GetNextConnectedLine(MarkGeometryLine reference, IList<MarkGeometryLine> lines, double tolerance)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var connectionType = IsConnected(reference, lines[i], tolerance);
                if (connectionType != ConnectionType.NONE)
                    return (lines[i], connectionType);
            }

            return (null, ConnectionType.NONE);
        }

        /// <summary>
        ///     Finds and returns the next path from the list of paths 
        ///     connected to the reference path.
        /// </summary>
        /// <param name="reference">The reference path</param>
        /// <param name="lines">The list of paths to search</param>
        /// <param name="tolerance">The contact tolerance</param>
        /// <returns>The connected path and connectiong type</returns>
        public static (MarkGeometryLine ConnectedPath, ConnectionType ConnectionType) GetNextConnectedLine(MarkGeometryPath reference, IList<MarkGeometryLine> lines, double tolerance)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var connectionType = IsConnected(reference, lines[i], tolerance);
                if (connectionType != ConnectionType.NONE)
                    return (lines[i], connectionType);
            }

            return (null, ConnectionType.NONE);
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
        ///     Checks and returns the type of connection/joint between lines/paths
        /// </summary>
        /// <param name="p1">The first path</param>
        /// <param name="p2">The second path</param>
        /// <param name="tolerance">The contact tolerance</param>
        /// <returns>Returns the type of connection/joint between lines/path</returns>
        public static ConnectionType IsConnected(MarkGeometryPath p1, MarkGeometryPath p2, double tolerance)
        {
            try
            {
                if (ABSMeasure2D(p1.StartPoint, p2.StartPoint) <= tolerance)
                    return ConnectionType.START_TO_START;
                else if (ABSMeasure2D(p1.StartPoint, p2.EndPoint) <= tolerance)
                    return ConnectionType.START_TO_END;
                else if (ABSMeasure2D(p1.EndPoint, p2.StartPoint) <= tolerance)
                    return ConnectionType.END_TO_START;
                else if (ABSMeasure2D(p1.EndPoint, p2.EndPoint) <= tolerance)
                    return ConnectionType.END_TO_END;

                return ConnectionType.NONE;
            }
            catch(Exception)
            {
                return ConnectionType.NONE;
            }
        }

        /// <summary>
        ///     Checks and returns the type of connection/joint between lines/paths
        /// </summary>
        /// <param name="p1">The first path</param>
        /// <param name="l2">The second line</param>
        /// <param name="tolerance">The contact tolerance</param>
        /// <returns>Returns the type of connection/joint between lines/path</returns>
        public static ConnectionType IsConnected(MarkGeometryPath p1, MarkGeometryLine l2, double tolerance)
        {
            try
            {
                if (ABSMeasure2D(p1.StartPoint, l2.StartPoint) <= tolerance)
                    return ConnectionType.START_TO_START;
                else if (ABSMeasure2D(p1.StartPoint, l2.EndPoint) <= tolerance)
                    return ConnectionType.START_TO_END;
                else if (ABSMeasure2D(p1.EndPoint, l2.StartPoint) <= tolerance)
                    return ConnectionType.END_TO_START;
                else if (ABSMeasure2D(p1.EndPoint, l2.EndPoint) <= tolerance)
                    return ConnectionType.END_TO_END;

                return ConnectionType.NONE;
            }
            catch (Exception)
            {
                return ConnectionType.NONE;
            }
        }

        /// <summary>
        ///     Checks and returns the type of connection/joint between lines
        /// </summary>
        /// <param name="l1">The first line</param>
        /// <param name="l2">The second line</param>
        /// <param name="tolerance">The contact tolerance</param>
        /// <returns>Returns the type of connection/joint between lines</returns>
        public static ConnectionType IsConnected(MarkGeometryLine l1, MarkGeometryLine l2, double tolerance)
        {
            try
            {
                if (ABSMeasure2D(l1.StartPoint, l2.StartPoint) <= tolerance)
                    return ConnectionType.START_TO_START;
                else if (ABSMeasure2D(l1.StartPoint, l2.EndPoint) <= tolerance)
                    return ConnectionType.START_TO_END;
                else if (ABSMeasure2D(l1.EndPoint, l2.StartPoint) <= tolerance)
                    return ConnectionType.END_TO_START;
                else if (ABSMeasure2D(l1.EndPoint, l2.EndPoint) <= tolerance)
                    return ConnectionType.END_TO_END;

                return ConnectionType.NONE;
            }
            catch (Exception)
            {
                return ConnectionType.NONE;
            }
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
            return (number >= minRange) && (number <= maxRange);
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

        public static bool IsWithin2D(MarkGeometryPoint point, MarkGeometryPath path, double resolution = 0.0001)
        {
            var extent = CalculateExtents(new IMarkGeometry[] { point, path });
            var testLine = new MarkGeometryLine(point, new MarkGeometryPoint(extent.MaxX, extent.MaxY));

            // if the number of intersection is odd, then the point is within
            return (CalculateIntersection2D(path, testLine, resolution).Count % 2) != 0;
        }

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

            if (geometries != null)
            {
                for (int i = 0; i < geometries.Length; i++)
                {
                    MaxX = Max<double>(geometries[i].Extents.MaxX, MaxX);
                    MaxY = Max<double>(geometries[i].Extents.MaxY, MaxY);
                    MaxZ = Max<double>(geometries[i].Extents.MaxZ, MaxZ);

                    MinX = Min<double>(geometries[i].Extents.MinX, MinX);
                    MinY = Min<double>(geometries[i].Extents.MinY, MinY);
                    MinZ = Min<double>(geometries[i].Extents.MinZ, MinZ);
                }
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
        public static GeometryExtents<double> CalculateExtents(IList<IMarkGeometry> geometries)
        {
            var MinX = double.MaxValue;
            var MaxX = double.MinValue;

            var MinY = double.MaxValue;
            var MaxY = double.MinValue;

            var MinZ = double.MaxValue;
            var MaxZ = double.MinValue;

            for (int i=0; i< geometries.Count; i++)
            {
                if (geometries[i].Extents.MaxX > MaxX)
                    MaxX = geometries[i].Extents.MaxX;
                if (geometries[i].Extents.MaxY > MaxY)
                    MaxY = geometries[i].Extents.MaxY;
                if (geometries[i].Extents.MaxZ > MaxZ)
                    MaxZ = geometries[i].Extents.MaxZ;

                if (geometries[i].Extents.MinX < MinX)
                    MinX = geometries[i].Extents.MinX;
                if (geometries[i].Extents.MinY < MinY)
                    MinY = geometries[i].Extents.MinY;
                if (geometries[i].Extents.MinZ < MinZ)
                    MinZ = geometries[i].Extents.MinZ;
            }

            var extents = new GeometryExtents<double>()
            {
                MinX = MinX,
                MinY = MinY,
                MinZ = MinZ,
                MaxX = MaxX,
                MaxY = MaxY,
                MaxZ = MaxZ
            };

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

            foreach(var g in geometries)
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

        /// <summary>
        ///     Converts an array of points into an enumerable of lines
        /// </summary>
        /// <param name="points">The array of points</param>
        /// <returns>Returns the generated enumerable of lines</returns>
        public static List<MarkGeometryLine> ToLines(IList<MarkGeometryPoint> points)
        {
            List<MarkGeometryLine> lines = new List<MarkGeometryLine>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                lines.Add(new MarkGeometryLine(points[i], points[i + 1]));
            }

            return lines;
        }

        /// <summary>
        ///     Converts an array of points into an enumerable of lines
        /// </summary>
        /// <param name="points">The array of points</param>
        /// <returns>Returns the generated enumerable of lines</returns>
        public static (List<MarkGeometryLine> Lines, double MinLineLength, double Perimeter) GetLinesAndStatistics(IList<MarkGeometryPoint> points)
        {
            double perimeter = 0;
            double minimumLength = double.MaxValue;
            List<MarkGeometryLine> lines = new List<MarkGeometryLine>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                var line = new MarkGeometryLine(points[i], points[i + 1]);

                if (line.Length < minimumLength)
                    minimumLength = line.Length;

                lines.Add(line);
                perimeter += line.Length;
            }

            return (lines, minimumLength, perimeter);
        }

        public static List<MarkGeometryPath> SplitByIntersections(MarkGeometryPath p1In, MarkGeometryPath p2In, double resolution = 0.0001)
        {
            var intersectingSegments = new List<MarkGeometryPath>();

            var lines = new List<MarkGeometryLine>();
            foreach (var line in ToLines(p1In.Points))
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

                    foreach (var ln in lines)
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

        public static MarkGeometryPath MakeFromUnion2D(MarkGeometryPath p1, MarkGeometryPath p2, double resolution = 0.0001)
        {
            var segA = SplitByIntersections(p1, p2, resolution);
            var segB = SplitByIntersections(p2, p1, resolution);

            segA.RemoveAll(x => IsWithin2D(x.Extents.Centre, p2));
            segB.RemoveAll(x => IsWithin2D(x.Extents.Centre, p1));

            var lines = new List<MarkGeometryLine>();

            foreach (var seg in segA)
            {
                lines.AddRange(ToLines(seg.Points));
            }

            foreach (var seg in segB)
            {
                lines.AddRange(ToLines(seg.Points));
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
                lines.AddRange(ToLines(seg.Points));
            }

            foreach (var seg in segB)
            {
                lines.AddRange(ToLines(seg.Points));
            }

            return new MarkGeometryPath(lines)
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
                lines.AddRange(ToLines(seg.Points));
            }

            foreach (var seg in segB)
            {
                lines.AddRange(ToLines(seg.Points));
            }

            return new MarkGeometryPath(lines.ToArray())
            {
                Fill = p1.Fill,
                Stroke = p1.Stroke,
                Transparency = p1.Transparency
            };
        }

        public static List<IMarkGeometry> ClipGeometry(IEnumerable<IMarkGeometry> geometriesIn, MarkGeometryRectangle boundaryIn)
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
                return ClipGeometry(new MarkGeometryPath(circle), boundaryIn);
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

        public static Dictionary<MarkGeometryRectangle, List<IMarkGeometry>> GenerateTiles(IMarkGeometry[] geometriesIn, double tileWidth, double tileHeight, int padding = 5)
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
        ///     Use method to load pattern into the scanner using the RTC5 Wrapper.
        /// </summary>
        /// <param name="pattern">A list of mark geometries to be loaded into the scanner</param>
        /// <param name="cardNumber">The number of the RTC 5 card</param>
        /// <param name="listNumber">The id of the RTC 5's list/instruction set</param>
        /// <param name="dwellDelayMicroseconds">The dwell delay (in microseconds) applied when marking points. See scanner manual for timed_mark_abs {@T parameter}</param>
        /// <returns>Returns true is succesful, otherwise returns false</returns>
        public static bool LoadPatternIntoRTC5_2D(
            IList<IMarkGeometry> pattern,
            Func<double, int> millimetresToBitsConverter,
            uint cardNumber = 1U,
            uint listNumber = 1U,
            double dwellDelayMicroseconds = 100d,
            bool _createList = true
        )
        {
            if (_createList)
            {
                while (RTC5Wrap.n_load_list(cardNumber, listNumber, 0) == 0) ;
                RTC5Wrap.n_long_delay(cardNumber, 1000U);
                RTC5Wrap.n_set_start_list(cardNumber, listNumber);
            }

            for (int i = 0; i < pattern.Count; i++)
            {
                if (pattern[i] is MarkGeometryPoint point)
                {
                    int x = millimetresToBitsConverter(point.X);
                    int y = millimetresToBitsConverter(point.Y);

                    RTC5Wrap.n_jump_abs(cardNumber, x, y);
                    RTC5Wrap.n_timed_mark_abs(cardNumber, x, y, dwellDelayMicroseconds);
                }
                else if (pattern[i] is MarkGeometryLine line)
                {
                    int sx = millimetresToBitsConverter(line.StartPoint.X);
                    int sy = millimetresToBitsConverter(line.StartPoint.Y);
                    int ex = millimetresToBitsConverter(line.EndPoint.X);
                    int ey = millimetresToBitsConverter(line.EndPoint.Y);

                    RTC5Wrap.n_jump_abs(cardNumber, sx, sy);
                    RTC5Wrap.n_mark_abs(cardNumber, ex, ey);
                }
                else if (pattern[i] is MarkGeometryCircle circle)
                {
                    int sx = millimetresToBitsConverter(circle.StartPoint.X);
                    int sy = millimetresToBitsConverter(circle.StartPoint.Y);
                    int cx = millimetresToBitsConverter(circle.CentrePoint.X);
                    int cy = millimetresToBitsConverter(circle.CentrePoint.Y);

                    RTC5Wrap.n_jump_abs(cardNumber, sx, sy);
                    RTC5Wrap.n_arc_abs(cardNumber, cx, cy, 360d);
                }
                else if (pattern[i] is MarkGeometryArc arc)
                {
                    int sx = millimetresToBitsConverter(arc.StartPoint.X);
                    int sy = millimetresToBitsConverter(arc.StartPoint.Y);
                    int cx = millimetresToBitsConverter(arc.CentrePoint.X);
                    int cy = millimetresToBitsConverter(arc.CentrePoint.Y);

                    RTC5Wrap.n_jump_abs(cardNumber, sx, sy);
                    RTC5Wrap.n_arc_abs(cardNumber, cx, cy, ToDegrees(arc.Sweep));
                }
                else if (pattern[i] is MarkGeometryPath path)
                {
                    if (path.Points.Count <= 0)
                        continue;

                    int sx = millimetresToBitsConverter(path.StartPoint.X);
                    int sy = millimetresToBitsConverter(path.StartPoint.Y);
                    RTC5Wrap.n_jump_abs(cardNumber, sx, sy);

                    for (int j = 1; j < path.Points.Count; j++)
                    {
                        int x = millimetresToBitsConverter(path.Points[j].X);
                        int y = millimetresToBitsConverter(path.Points[j].Y);
                        RTC5Wrap.n_mark_abs(cardNumber, x, y);
                    }
                }
                else if (pattern[i] is IMarkGeometryWrapper wrapper)
                {
                    if (!LoadPatternIntoRTC5_2D(wrapper.Flatten(), millimetresToBitsConverter, cardNumber, listNumber, dwellDelayMicroseconds, _createList))
                        return false;
                }
            }

            if (_createList)
            {
                RTC5Wrap.n_jump_abs(cardNumber, 0, 0);
                RTC5Wrap.n_set_end_of_list(cardNumber);
            }

            return true;
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
            var document = new netDxf.DxfDocument(new HeaderVariables());

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
            var document = new netDxf.DxfDocument(new HeaderVariables());

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
            var document = new netDxf.DxfDocument(new HeaderVariables());

            foreach (var gm in geometries)
            {
                document.AddEntity(gm.GetAsDXFEntity());
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
        public static bool SaveDXF(string fileName, IList<IMarkGeometry> geometries)
        {
            var document = new netDxf.DxfDocument(new HeaderVariables());

            foreach (var gm in geometries)
            {
                document.AddEntity(gm.GetAsDXFEntity());
            }

            document.Save(fileName);
            return File.Exists(fileName);
        }
    }
}
