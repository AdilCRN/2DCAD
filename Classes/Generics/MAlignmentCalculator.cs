using MRecipeStructure.Classes.MRecipeStructure;
using MSolvLib.MarkGeometry;
using System.Collections.Generic;
using System.Numerics;

namespace MarkGeometriesLib.Classes.Generics
{
    public static class MAlignmentCalculator
    {
        public static Matrix4x4 GetAlignmentTransform(MAlignmentType alignmentType, List<MarkGeometryPoint> estimatedPoints, List<MarkGeometryPoint> measuredPoints)
        {
            if (alignmentType == MAlignmentType.Type1 && measuredPoints?.Count > 0)
            {
                return GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    measuredPoints[0].X, measuredPoints[0].Y
                );
            }

            return GeometricArithmeticModule.EstimateTransformationMatrixFromPoints(
                estimatedPoints, measuredPoints
            );
        }
    }
}
