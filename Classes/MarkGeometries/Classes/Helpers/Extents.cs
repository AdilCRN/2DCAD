using MSolvLib.MarkGeometry;
using System;

namespace MSolvLib.Classes.MarkGeometries.Classes.Helpers
{
    public class GeometryExtents<T>
    {
        // minimums
        public T MinX { get; set; } = default(T);
        public T MinY { get; set; } = default(T);
        public T MinZ { get; set; } = default(T);

        // maximums
        public T MaxX { get; set; } = default(T);
        public T MaxY { get; set; } = default(T);
        public T MaxZ { get; set; } = default(T);

        // dimensions
        public double Width => (double)((MaxX as double?) - (MinX as double?));
        public double Height => (double)((MaxY as double?) - (MinY as double?));
        public double Depth => (double)((MaxZ as double?) - (MinZ as double?));

        public MarkGeometryPoint MaximumPoint => new MarkGeometryPoint((double)(MaxX as double?), (double)(MaxY as double?), (double)(MaxZ as double?));

        public MarkGeometryPoint MinimumPoint => new MarkGeometryPoint((double)(MinX as double?), (double)(MinY as double?), (double)(MinZ as double?));

        public double Hypotenuse => Math.Sqrt(Math.Pow(Width, 2) + Math.Pow(Height, 2));

        public MarkGeometryPoint Centre => new MarkGeometryPoint(
                (double)(MinX as double?) + (0.5 * Width),
                (double)(MinY as double?) + (0.5 * Height),
                (double)(MinZ as double?) + (0.5 * Depth)
            );
        public MarkGeometryRectangle Boundary => new MarkGeometryRectangle(Centre, Width, Height);

        public double Area => (Width * Height);
        public double Perimeter => 2 * (Width + Height);

        public MarkGeometryPoint GetClosestOrigin()
        {
            return new MarkGeometryPoint(
                    GeometricArithmeticModule.Constrain(0, (double)(MinX as double?), (double)(MaxX as double?)),
                    GeometricArithmeticModule.Constrain(0, (double)(MinY as double?), (double)(MaxY as double?)),
                    GeometricArithmeticModule.Constrain(0, (double)(MinZ as double?), (double)(MaxZ as double?))
                );
        }

        public override string ToString()
        {
            return $"{{MinX: {MinX}, MinY: {MinY}, MinZ: {MinZ}, MaxX: {MaxX}, MaxY: {MaxY}, MaxZ: {MaxZ}, Width: {Width}, Height: {Height}, Depth: {Depth}, Centre: {Centre}}}";
        }

        public static GeometryExtents<double> Combine(GeometryExtents<double> extentsA, GeometryExtents<double> extentsB)
        {
            return new GeometryExtents<double>()
            {
                MinX = Math.Min(extentsA.MinX, extentsB.MinX),
                MinY = Math.Min(extentsA.MinY, extentsB.MinY),
                MinZ = Math.Min(extentsA.MinZ, extentsB.MinZ),
                MaxX = Math.Max(extentsA.MaxX, extentsB.MaxX),
                MaxY = Math.Max(extentsA.MaxY, extentsB.MaxY),
                MaxZ = Math.Max(extentsA.MaxZ, extentsB.MaxZ),
            };
        }

        public static GeometryExtents<double> CreateDefaultDouble()
        {
            return new GeometryExtents<double>()
            {
                MinX = double.MaxValue,
                MinY = double.MaxValue,
                MinZ = double.MaxValue,
                MaxX = double.MinValue,
                MaxY = double.MinValue,
                MaxZ = double.MinValue,
            };
        }
    }
}
