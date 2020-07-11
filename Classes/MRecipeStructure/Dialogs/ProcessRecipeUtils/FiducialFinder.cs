using MRecipeStructure.Classes.MRecipeStructure;
using MSolvLib.DialogForms;
using MSolvLib.MarkGeometry;
using MSolvLib.UtilityClasses.Cam2BeamUtility.CamToBeamInterface;
using NLog;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MRecipeStructure.Dialogs.ProcessRecipeUtils
{
    public class FiducialFinder
    {
        // logger
        private ILogger _logger = LogManager.GetCurrentClassLogger();

        // for loading cached fiducials
        private static Dictionary<MFiducialInfo, MarkGeometryPoint> _cachedFiducials = new Dictionary<MFiducialInfo, MarkGeometryPoint>();

        public FiducialFinder()
        {
        }

        public void Reset()
        {
            _cachedFiducials.Clear();
        }

        /// <summary>
        ///     Use method to invert an input transform matrix about a given origin.
        ///     Could help when combining transformation for stages with inverted axis.
        /// </summary>
        /// <param name="taskHandler">An implementation of the process configuration tasks</param>
        /// <param name="parentTransform">The parent transform in the chain of transformations</param>
        /// <param name="transform">The input transformation</param>
        /// <param name="ctIn">A cancellation token</param>
        /// <returns>The input matrix flip about it's parent's transform</returns>
        public async Task<Matrix4x4> InvertTransform(IProcessConfigurationTasksHandler taskHandler, Matrix4x4 parentTransform, Matrix4x4 transform, CancellationToken ctIn)
        {
            var origin = new MarkGeometryPoint();
            origin.Transform(parentTransform);

            var inverts = await taskHandler.GetStageInverts(ctIn);
            return GeometricArithmeticModule.CombineTransformations(
                // flip about the base transform's origin
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    -origin.X, -origin.Y, -origin.Z
                ),

                // apply the next transform
                transform,

                // flip the next transform on the requested x-axis
                GeometricArithmeticModule.GetScalingTransformationMatrix(
                    inverts.InvertX ? -1 : 1,
                    inverts.InvertY ? -1 : 1
                ),

                // translate back to the base transform's origin
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    origin.X, origin.Y, origin.Z
                )
            );
        }

        public async Task<Matrix4x4> GetAbsoluteTransformFromStageOrigin(
            IProcessConfigurationTasksHandler taskHandler,
            ICamToBeam cam2Beam,
            MRecipe recipe, 
            MRecipeBaseNode recipeNode, 
            CancellationToken ctIn
        )
        {
            // firstly, find closest parent with fiducials
            var parent = recipeNode;
            while(
                parent != null &&
                parent.Fiducials.Count <= 0
            )
            { parent = parent.Parent as MRecipeBaseNode; }

            if (parent != null)
            {
                // recursion - get it's parent's transform
                var baseTransform = await GetAbsoluteTransformFromStageOrigin(
                    taskHandler, cam2Beam, recipe, parent.Parent as MRecipeBaseNode, ctIn
                );

                var fiducialPoints = new List<MarkGeometryPoint>();
                var measuredPoints = new List<MarkGeometryPoint>();

                for (int i = 0; i < parent.Fiducials.Count; i++)
                {
                    var fiducial = parent.Fiducials[i];
                    
                    if (!_cachedFiducials.ContainsKey(fiducial))
                    {
                        // estimate stage position
                        var stageLocation = new MarkGeometryPoint(
                            fiducial.X, fiducial.Y, fiducial.Z
                        );
                        stageLocation.Transform(baseTransform);

                        // goto estimated position
                        if (!await taskHandler.GotoXY(
                            stageLocation.X, stageLocation.Y, ctIn
                        ))
                            throw new Exception($"Failed to goto the estimated position; origin: {await taskHandler.GetStageOrigin(ctIn)}, fiducial: {(fiducial.X, fiducial.Y)} est: {stageLocation}");

                        // find and centre
                        {
                            if (!await cam2Beam.MoveBeamToCamOffset(ctIn))
                                throw new Exception($"Failed to move beam to cam offset");

                            if (!await taskHandler.MovetoCameraFocus(ctIn))
                                throw new Exception($"Failed to move camera to focus");

                            await taskHandler.SwitchCameraLedOn(ctIn);

                            // attempt to centre on fiducial
                            if (!await taskHandler.CentreOnVisibleObject(ctIn))
                            {
                                _logger.Info($"Failed to centre on fiducial");

                                // ask user to locate fiducials
                                Application.Current.Dispatcher.Invoke(
                                    () =>
                                    {
                                        var fidVm = taskHandler.GetNewFidFindViewModel();
                                        var inspectionDialog = new InspectionWindow(fidVm, Application.Current.MainWindow);
                                        inspectionDialog.Owner = Application.Current.MainWindow;

                                        if (inspectionDialog.ShowDialog() != true)
                                            throw new Exception("Failed to find fiducial");

                                        // TODO : think about updating the machine's origin
                                    }
                                );
                            }

                            if (!await cam2Beam.MoveCamToBeamOffset(ctIn))
                                throw new Exception($"Failed to move camera to beam offset");
                        }

                        // read fiducial position
                        var (stgX, stgY, success) = await taskHandler.GetStageXY(ctIn);

                        if (!success)
                            throw new Exception("Failed to read the current stage position");

                        // update cache
                        _cachedFiducials[fiducial] = new MarkGeometryPoint(stgX, stgY);
                    }

                    // update measured points
                    fiducialPoints.Add(new MarkGeometryPoint(fiducial.X, fiducial.Y));
                    measuredPoints.Add(_cachedFiducials[fiducial]);
                }

                // calculate transform from measured fiducials
                var parentTransform = GeometricArithmeticModule.EstimateTransformationMatrixFromPoints(
                    fiducialPoints, measuredPoints
                );

                return GeometricArithmeticModule.CombineTransformations(
                    parentTransform,
                    await InvertTransform(
                        taskHandler,
                        parentTransform,
                        MRecipe.GetRelativeTransformFromParent(
                            parent, recipeNode
                        ),
                        ctIn
                    )
                );
            }

            var origin = await taskHandler.GetStageOrigin(ctIn);
            var stageOrigin = GeometricArithmeticModule.GetTranslationTransformationMatrix(
                origin.X, origin.Y
            );
            return GeometricArithmeticModule.CombineTransformations(
                stageOrigin,
                await InvertTransform(
                    taskHandler,
                    stageOrigin,
                    MRecipe.GetRelativeTransform(
                        recipe, recipeNode
                    ), 
                    ctIn
                )
            );
        }
    }
}
