using MSolvLib.MarkGeometry;
using SharpGLShader.Utils;
using System;
using System.Numerics;

namespace MarkGeometriesLib.Classes.Generics
{
    public class AlignmentCalculationWrapper
    {
        public MVertex FiducialA_CAD { get; set; }
        public MVertex FiducialB_CAD { get; set; }
        public MVertex Inverts { get; set; }
        public MVertex C2BOffsets { get; set; }
        public MVertex FiducialA_Stage { get; set; }
        public MVertex FiducialB_Stage { get; set; }

        public double TargetAngle { get; protected set; }
        public double ExpectedAngle { get; protected set; }
        public double ActualAngle { get; protected set; }
        public double Angle { get; protected set; }

        public MVertex PositionRelativeFidA { get; protected set; }
        public MVertex PositionRelativeFidB { get; protected set; }
        public MVertex OriginRelativeCAM { get; protected set; }
        public MVertex OriginRelativeBEAM { get; protected set; }

        public AlignmentCalculationWrapper(
            MVertex invertsIn,
            MVertex c2bOffsetsIn,
            MVertex fiducialAIn,
            MVertex fiducialStageAIn,
            MVertex fiducialBIn,
            MVertex fiducialStageBIn
        )
        {
            // update the inverts
            Inverts = invertsIn;

            // update the camera to beam offsets
            C2BOffsets = c2bOffsetsIn;

            // update the CAD fiducials
            FiducialA_CAD = fiducialAIn;
            FiducialB_CAD = fiducialBIn;

            // update the stage fiducials
            FiducialA_Stage = fiducialStageAIn;
            FiducialB_Stage = fiducialStageBIn;

            Update();
        }

        public void UpdateTargetAngle(double targetAngleIn)
        {
            TargetAngle = targetAngleIn;
            Update();
        }

        public void Update()
        {
            // calculate the expected angle in radians
            // Note: Atan2 is Y / X
            ExpectedAngle = Math.Atan2(
                FiducialB_CAD.Y - FiducialA_CAD.Y,
                FiducialB_CAD.X - FiducialA_CAD.X
            );

            // add the target angle to the expected angle
            ExpectedAngle += TargetAngle;

            // calculate the actual angle in radians
            // Note: Atan2 is Y / X
            ActualAngle = Math.Atan2(
                (Inverts.Y * FiducialB_Stage.Y) - (Inverts.Y * FiducialA_Stage.Y),
                (Inverts.X * FiducialB_Stage.X) - (Inverts.X * FiducialA_Stage.X)
            );

            // calculate the angle in radians
            Angle = ActualAngle - ExpectedAngle;

            // calculate the position based on fiducial A (rel to camera)
            PositionRelativeFidA = new MVertex(
                FiducialA_Stage.X - (Inverts.X * ((FiducialA_CAD.X * Math.Cos(Angle)) - (FiducialA_CAD.Y * Math.Sin(Angle)))),
                FiducialA_Stage.Y - (Inverts.Y * ((FiducialA_CAD.X * Math.Sin(Angle)) + (FiducialA_CAD.Y * Math.Cos(Angle))))
            );

            // calculate the position based on fiducial A (rel to camera)
            PositionRelativeFidB = new MVertex(
                FiducialB_Stage.X - (Inverts.X * ((FiducialB_CAD.X * Math.Cos(Angle)) - (FiducialB_CAD.Y * Math.Sin(Angle)))),
                FiducialB_Stage.Y - (Inverts.Y * ((FiducialB_CAD.X * Math.Sin(Angle)) + (FiducialB_CAD.Y * Math.Cos(Angle))))
            );

            // calculate the origin (rel to camera)
            OriginRelativeCAM = Average(PositionRelativeFidA, PositionRelativeFidB);

            // calculate the origin (rel to beam)
            OriginRelativeBEAM = Sum(OriginRelativeCAM, C2BOffsets);
        }

        /// <summary>
        /// Calculates the target (as per CAD) on the stage centred about the
        /// optical axis.
        /// </summary>
        /// <param name="stageOriginIn">The stage origin from a previous calculated result</param>
        /// <param name="targetOnCADIn">The target position as per CAD</param>
        /// <param name="stageAngleIn">The angle from a previously calculated result</param>
        /// <returns>The actual stage coordinate</returns>
        public MVertex GetTargetPositionOnStage(MVertex targetOnCADIn, MVertex stageOriginIn, double stageAngleIn)
        {
            return new MVertex(
                (stageOriginIn.X + (Inverts.X * (((targetOnCADIn.X) * Math.Cos(stageAngleIn)) - ((targetOnCADIn.Y) * Math.Sin(stageAngleIn))))),
                (stageOriginIn.Y + (Inverts.Y * (((targetOnCADIn.X) * Math.Sin(stageAngleIn)) + ((targetOnCADIn.Y) * Math.Cos(stageAngleIn)))))
            );
        }

        /// <summary>
        /// Calculates the target (as per CAD) on the stage centred about the
        /// optical axis.
        /// </summary>
        /// <param name="stageOriginIn">The stage origin from a previous calculated result</param>
        /// <param name="targetOnCADIn">The target position as per CAD</param>
        /// <param name="stageAngleIn">The angle from a previously calculated result</param>
        /// <returns>The actual stage coordinate</returns>
        public static MVertex GetTargetPositionOnStage(MVertex invertsIn, MVertex targetOnCADIn, MVertex stageOriginIn, double stageAngleIn)
        {
            return new MVertex(
                (stageOriginIn.X + (invertsIn.X * (((targetOnCADIn.X) * Math.Cos(stageAngleIn)) - ((targetOnCADIn.Y) * Math.Sin(stageAngleIn))))),
                (stageOriginIn.Y + (invertsIn.Y * (((targetOnCADIn.X) * Math.Sin(stageAngleIn)) + ((targetOnCADIn.Y) * Math.Cos(stageAngleIn)))))
            );
        }

        /// <summary>
        /// Calculates the target (as per CAD) on the stage centred about the
        /// optical axis.
        /// </summary>
        /// <param name="stageOriginIn">The stage origin from a previous calculated result</param>
        /// <param name="targetOnCADIn">The target position as per CAD</param>
        /// <param name="stageAngleIn">The angle from a previously calculated result</param>
        /// <returns>The actual stage coordinate</returns>
        public static MVertex GetTargetPositionOnStage(MVertex invertsIn, MarkGeometryPoint targetOnCADIn, MVertex stageOriginIn, double stageAngleIn)
        {
            return new MVertex(
                (stageOriginIn.X + (invertsIn.X * (((targetOnCADIn.X) * Math.Cos(stageAngleIn)) - ((targetOnCADIn.Y) * Math.Sin(stageAngleIn))))),
                (stageOriginIn.Y + (invertsIn.Y * (((targetOnCADIn.X) * Math.Sin(stageAngleIn)) + ((targetOnCADIn.Y) * Math.Cos(stageAngleIn)))))
            );
        }

        /// <summary>
        /// Calculates the target (as per CAD) on the stage centred about the
        /// optical axis.
        /// </summary>
        /// <param name="targetOnCADIn">The target position as per CAD</param>
        /// <returns>The actual stage coordinate</returns>
        public MVertex GetTargetPositionOnStage(MVertex targetOnCADIn)
        {
            return GetTargetPositionOnStage(targetOnCADIn, OriginRelativeBEAM, Angle);
        }

        /// <summary>
        /// Calculates the target (as per CAD) on the stage centred about the
        /// optical axis. The stage will have limited travel, and may not be
        /// able to reach the target coordinate. In this case, move the mask
        /// stage as close as possible to the target coordinate.
        /// The remaining distance to the target is applied as a SCANNER OFFSET.
        /// </summary>
        /// <param name="targetOnCADIn">The target position as per CAD</param>
        /// <param name="stageLimitsIn">The maximum and minimum stage boundaries</param>
        /// <param name="scannerLimitsIn">The maximum and minimum scanner boundaries</param>
        /// <returns>The actual stage coordinate and scanner offsets</returns>
        public (MVertex StagePosition, MVertex ScannerOffsets, bool IsWithinBounds) GetTargetPositionOnStage(MVertex targetOnCADIn, MLimit stageLimitsIn, MLimit scannerLimitsIn)
        {
            bool isWithinBounds = true;

            // calculate the position on the stage
            MVertex targetOnStage = GetTargetPositionOnStage(targetOnCADIn);

            // constrain the position to the stage's limits
            var constrainedStagePosition = new MVertex(
                Constrain(targetOnStage.X, stageLimitsIn.MinimumX, stageLimitsIn.MaximumX),
                Constrain(targetOnStage.Y, stageLimitsIn.MinimumY, stageLimitsIn.MaximumY)
            );

            // apply remaining offsets to the scanner
            var scannerOffsets = new MVertex(
                -1 * (targetOnStage.X - constrainedStagePosition.X),
                -1 * (targetOnStage.Y - constrainedStagePosition.Y)
            );

            // ensure scanner offsets are within the scanner's limits
            if (
                scannerOffsets.X < scannerLimitsIn.MinimumX ||
                scannerOffsets.X > scannerLimitsIn.MaximumX ||
                scannerOffsets.Y < scannerLimitsIn.MinimumY ||
                scannerOffsets.Y > scannerLimitsIn.MaximumY
            )
                isWithinBounds = false;

            return (constrainedStagePosition, scannerOffsets, isWithinBounds);
        }

        // TODO: Implement mask angle align func
        //     1. The mask angle is measured directly by camera.
        //     2. The mask is rotated to compensate for the angle.
        //     3. The procedure continues until the angle is zero.

        public MVertex ConvertCADToMachineCoordinate(MVertex cadOrigin, MVertex pointOnCAD)
        {
            var transform = GeometricArithmeticModule.CombineTransformations(
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    -cadOrigin.X, -cadOrigin.Y
                ),
                GeometricArithmeticModule.GetRotationTransformationMatrix(
                    0, 0, Angle
                ),
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    OriginRelativeBEAM.X, OriginRelativeBEAM.Y
                )
            );

            var result = Vector3.Transform(
                new Vector3((float)pointOnCAD.X, (float)pointOnCAD.Y, 0),
                transform
            );

            return new MVertex(Inverts.X * result.X, Inverts.Y * result.Y);
        }

        private static MVertex Average(MVertex aIn, MVertex bIn)
        {
            return new MVertex(
                (aIn.X + bIn.X) / 2d,
                (aIn.Y + bIn.Y) / 2d
            );
        }

        private static MVertex Sum(MVertex aIn, MVertex bIn)
        {
            return new MVertex(
                (aIn.X + bIn.X),
                (aIn.Y + bIn.Y)
            );
        }

        private double Constrain(double val, double minVal, double maxVal)
        {
            return Math.Max(Math.Min(maxVal, val), minVal);
        }
    }
}
