using MSolvLib.Classes.ProcessConfiguration;
using MSolvLib.ExtentionMethods;
using MSolvLib.Interfaces;
using MSolvLib.MarkGeometry;
using MSolvLib.UtilityClasses.Cam2BeamUtility.CamToBeamInterface;
using MSolvLib.UtilityClasses.ProcessModeSelector;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace MRecipeStructure.Dialogs.ProcessRecipeUtils
{
    public class RunRecipeDialogModel
    {
        #region Section : Private Properties
        
        private ICamToBeam _cam2Beam;
        private IAbortComponent _abort;
        private IPrePostRecipe _prePost;
        private ILogger _logger = LogManager.GetCurrentClassLogger();
        private IList<IProcessConfigurationTasksHandler> _processConfigTasksHandlers; 

        #endregion

        public ProcessModeSelectorModel MarkingModeSelector { get; internal set; }

        public RunRecipeDialogModel(
            IList<IProcessConfigurationTasksHandler> processConfigTasksHandlerIn,
            ProcessModeSelectorModel markingModeSelectorIn,
            IPrePostRecipe prePostRecipeIn,
            ICamToBeam cam2BeamIn,
            IAbortComponent abort
        )
        {
            _processConfigTasksHandlers = processConfigTasksHandlerIn;
            MarkingModeSelector = markingModeSelectorIn;
            _prePost = prePostRecipeIn;
            _cam2Beam = cam2BeamIn;
            _abort = abort;
        }

        public async Task<bool> ProcessLayer(
            RecipeProcessEntityInfo entityInfo,
            List<IMarkGeometry> layerPattern,
            IMarkParametersComplete layerParameters,
            Func<IProcessConfigurationTasksHandler, Task<Matrix4x4>> fetchAlignmentTransform,
            Func<bool> shouldPause,
            Action<string> logInfo,
            Action<string> logError,
            CancellationToken ctIn
        )
        {
            try
            {
                entityInfo.State = EntityState.RUNNING;
                entityInfo.ProgressPercentage = 0;

                // get execution handler
                var taskHandler = GetProcessConfigurationTaskHandler(
                    entityInfo.Layer.TargetProcessMode
                );

                if (taskHandler == null)
                    throw new Exception($"Failed to load execution context for the target process mode `{entityInfo.Layer.TargetProcessMode}`");

                // inverts
                var (shouldInvertX, shouldInvertY) = await taskHandler.GetStageInverts(ctIn);
                double xInvert = shouldInvertX ? -1d : 1d;
                double yInvert = shouldInvertY ? -1d : 1d;

                // alignment transform + pattern offset
                var extents = GeometricArithmeticModule.CalculateExtents(layerPattern);
                var patternStageOffset = GeometricArithmeticModule.CombineTransformations(
                    await fetchAlignmentTransform.Invoke(taskHandler),
                    GeometricArithmeticModule.GetTranslationTransformationMatrix(
                        xInvert * extents.Centre.X, yInvert * extents.Centre.Y
                    )
                );

                if (entityInfo.Layer.TileDescriptions.Count <= 0)
                    entityInfo.Layer.GenerateTileDescriptionsFromSettings(extents);

                int count = 0;
                double progressIncrement = 100d / entityInfo.Layer.TileDescriptions.Count();

                // process tiles
                foreach (var tile in entityInfo.Layer.TileDescriptions)
                {
                    // pause if requested
                    while (
                        shouldPause() && 
                        !ctIn.IsCancellationRequested
                    )
                    { await Task.Delay(100); }

                    // abort if cancellation is requested
                    ctIn.ThrowIfCancellationRequested();

                    var tileStagePositionTransform = GeometricArithmeticModule.CombineTransformations(
                        patternStageOffset,
                        GeometricArithmeticModule.GetTranslationTransformationMatrix(
                            xInvert * tile.CentreX, yInvert * tile.CentreY
                        )
                    );

                    // extract clipped pattern
                    var clippedPattern = new List<IMarkGeometry>();

                    // centre clipped pattern
                    var scannerTransform = GeometricArithmeticModule.CombineTransformations(
                        GeometricArithmeticModule.GetTranslationTransformationMatrix(
                            -tile.CentreX, -tile.CentreY
                        )
                    );

                    foreach (var geometry in GeometricArithmeticModule.ClipGeometry(layerPattern, (MarkGeometryRectangle)tile))
                    {
                        var clone = (IMarkGeometry)geometry.Clone();
                        clone.Transform(scannerTransform);
                        clippedPattern.Add(clone);
                    }

                    // mark pattern + repeat
                    var tileCentre = new MarkGeometryPoint();
                    tileCentre.Transform(tileStagePositionTransform);
                    logInfo($"Marking tile {++count} at Stage: {tileCentre}");
                    logInfo($"Pattern extents : {GeometricArithmeticModule.CalculateExtents(clippedPattern)}");
                    await taskHandler.MarkPattern(
                        GetProcessConfiguration(entityInfo.Layer.TargetProcessMode),
                        tileStagePositionTransform,
                        clippedPattern,
                        layerParameters,
                        ctIn
                    );

                    entityInfo.ProgressPercentage += progressIncrement;
                }

                entityInfo.State = EntityState.COMPLETED;
                return true;
            }
            catch (Exception exp)
            {
                entityInfo.State = EntityState.ERROR;
                logError(exp.Message);
            }

            return false;
        }

        public IProcessConfiguration GetProcessConfiguration(string processModeName)
        {
            return MarkingModeSelector.AvailableConfigurations.FirstOrDefault(config => config.Name.EnglishValue == processModeName);
        }

        public IProcessConfigurationTasksHandler GetProcessConfigurationTaskHandler(string processModeName)
        {
            var tasksHandler = _processConfigTasksHandlers?.FirstOrDefault(handler => handler.Tag == processModeName);

            if (tasksHandler == null || processModeName == null)
                return _processConfigTasksHandlers?.FirstOrDefault(handler => handler.Tag == MarkingModeSelector.CurrentConfiguration.Name.EnglishValue);

            return tasksHandler;
        }

        public ICamToBeam GetCam2Beam()
        {
            return _cam2Beam;
        }

        public async Task<bool> ToggleCam2Beam(CancellationToken ctIn)
        {
            return await _cam2Beam.MoveCamToBeamOffset(ctIn);
        }

        public async Task<bool> ToggleBeam2Cam(CancellationToken ctIn)
        {
            return await _cam2Beam.MoveBeamToCamOffset(ctIn);
        }

        public void Abort()
        {
            _abort.ABORT();
        }

        public void Log(string message)
        {
            _logger.Info(message);
        }

        public void Log(Exception exp)
        {
            _logger.Warn(exp.ToDetailedString());
        }
    }
}
