using MarkGeometriesLib.Classes.Generics;
using MRecipeStructure.Classes.MRecipeStructure;
using MSolvLib.Classes.Alignment;
using MSolvLib.Classes.ProcessConfiguration;
using MSolvLib.ExtentionMethods;
using MSolvLib.Interfaces;
using MSolvLib.MarkGeometry;
using MSolvLib.UtilityClasses.Cam2BeamUtility.CamToBeamInterface;
using MSolvLib.UtilityClasses.ProcessModeSelector;
using NLog;
using SharpGLShader.Utils;
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

        private Func<(bool InvertX, bool InvertY)> _getInverts = () => (false, false);
        private Func<(double X, double Y)> _getCamToBeamOffsetIn = () => (0d, 0d);
        private Func<(double X, double Y, double ThetaDegrees)> _getMachineOrigin = () => (0d, 0d, 0d);

        private MVertex _origin;
        private double _originThetaRadians;
        private MVertex _inverts;

        #endregion

        #region Section: Public Properties
        
        public ProcessModeSelectorModel MarkingModeSelector { get; internal set; } 

        #endregion

        #region Section: Constructor

        public RunRecipeDialogModel(
            IList<IProcessConfigurationTasksHandler> processConfigTasksHandlerIn,
            ProcessModeSelectorModel markingModeSelectorIn,
            Func<(bool InvertX, bool InvertY)> getInvertsIn,
            Func<(double X, double Y)> getCamToBeamOffsetIn,
            Func<(double X, double Y, double Theta)> getMachineOriginIn,
            IPrePostRecipe prePostRecipeIn,
            ICamToBeam cam2BeamIn,
            IAbortComponent abort
        )
        {
            _processConfigTasksHandlers = processConfigTasksHandlerIn;
            _getCamToBeamOffsetIn = getCamToBeamOffsetIn;
            MarkingModeSelector = markingModeSelectorIn;
            _getMachineOrigin = getMachineOriginIn;
            _getInverts = getInvertsIn;
            _prePost = prePostRecipeIn;
            _cam2Beam = cam2BeamIn;
            _abort = abort;

            ReloadOrigin();
        }

        #endregion

        #region Section: Class Method

        public async Task<bool> ProcessLayer(
            MRecipe recipe,
            RecipeProcessEntityInfo entityInfo,
            List<IMarkGeometry> layerPattern,
            IMarkParametersComplete markParameters,
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

                // TODO: use fiducials to update the machine origin
                //// update the origin data model based on the measured fiducials
                //_origin = new MVertex(alignmentInfo.CalculatedXYTheta.X, alignmentInfo.CalculatedXYTheta.Y);
                //_originThetaRadians = GeometricArithmeticModule.ToRadians(alignmentInfo.CalculatedXYTheta.Theta);

                // get the layer's transform
                var layerThetaRad = 0d;
                var layerTransform = MRecipe.GetRelativeTransform(recipe, entityInfo.Layer);

                {
                    // apply transform to calculate it's angle
                    var refLine = new MarkGeometryLine(
                        new MarkGeometryPoint(),
                        new MarkGeometryPoint(100, 0)
                    );
                    refLine.Transform(layerTransform);

                    layerThetaRad = refLine.Angle;
                }

                // prepare layer's tiles
                var extents = GeometricArithmeticModule.CalculateExtents(layerPattern);
                if (entityInfo.Layer.TileDescriptions.Count <= 0)
                    entityInfo.Layer.GenerateTileDescriptionsFromSettings(extents);

                // activate the configuration for the device layer
                await ActivateConfigurationForLayer(entityInfo.Layer, ctIn);

                // get the process configuration
                var processConfig = GetProcessConfiguration(entityInfo.Layer);

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

                    // calculate the tile's CAD origin
                    var tileOrigin = new MarkGeometryPoint(tile.CentreX, tile.CentreY);
                    tileOrigin.Transform(layerTransform);

                    // extract clipped pattern
                    var clippedPattern = new List<IMarkGeometry>();

                    // centre clipped pattern
                    var scannerTransform = GeometricArithmeticModule.CombineTransformations(
                        GeometricArithmeticModule.GetTranslationTransformationMatrix(
                            -tile.CentreX, -tile.CentreY
                        )
                    );

                    // align pattern in scanner's frame
                    foreach (var geometry in GeometricArithmeticModule.ClipGeometry(layerPattern, (MarkGeometryRectangle)tile))
                    {
                        var clone = (IMarkGeometry)geometry.Clone();
                        clone.Transform(scannerTransform);
                        clippedPattern.Add(clone);
                    }

                    //// update CAD view
                    //updateCadView((layer, tileOrigin, _originThetaRadians + deviceRotation, count));

                    // calculate the tile's stage origin
                    var estimatedTilePosition = AlignmentCalculationWrapper.GetTargetPositionOnStage(
                        _inverts, tileOrigin, _origin, _originThetaRadians
                    );

                    var _alignment = new PanelXYThetaScale(
                        estimatedTilePosition.X, estimatedTilePosition.Y, GeometricArithmeticModule.ToDegrees(_originThetaRadians + layerThetaRad)
                    );

                    logInfo($"Executing Tile: {count} at X: {Math.Round(_alignment.X, 4)}, Y: {Math.Round(_alignment.Y, 4)}, Th: {Math.Round(_alignment.Theta, 4)} deg.");

                    //// mark pattern + repeat
                    if (!await GetProcessConfigurationTaskHandler(entityInfo.Layer)?.MarkPattern(clippedPattern, markParameters, _alignment, processConfig, ctIn, false))
                    {
                        Log($"Failed to process {entityInfo.Layer.Tag}.");
                        entityInfo.State = EntityState.ERROR;
                        return false;
                    }

                    // update the counter
                    entityInfo.ProgressPercentage += progressIncrement;
                    count += 1;
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

        public IProcessConfiguration GetProcessConfiguration(MRecipeDeviceLayer layer)
        {
            return GetProcessConfiguration(layer.TargetProcessMode);
        }

        public IProcessConfigurationTasksHandler GetProcessConfigurationTaskHandler(string processModeName)
        {
            var tasksHandler = _processConfigTasksHandlers?.FirstOrDefault(handler => handler.Tag == processModeName);

            if (tasksHandler == null || processModeName == null)
                return _processConfigTasksHandlers?.FirstOrDefault(handler => handler.Tag == MarkingModeSelector.CurrentConfiguration.Name.EnglishValue);

            return tasksHandler;
        }

        public IProcessConfigurationTasksHandler GetProcessConfigurationTaskHandler(MRecipeDeviceLayer layer)
        {
            return GetProcessConfigurationTaskHandler(layer.TargetProcessMode);
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

        public void ReloadOrigin()
        {
            var inverts = _getInverts();
            var origin = _getMachineOrigin();

            _origin = new MVertex(origin.X, origin.Y);
            _inverts = new MVertex(inverts.InvertX ? -1 : 1, inverts.InvertY ? -1 : 1);
            _originThetaRadians = GeometricArithmeticModule.ToRadians(origin.ThetaDegrees);
        }

        public MVertex GetTargetPositionOnStage(double cadX, double cadY)
        {
            return AlignmentCalculationWrapper.GetTargetPositionOnStage(
                _inverts, new MVertex(cadX, cadY), _origin, _originThetaRadians
            );
        }

        public Matrix4x4 GetTargetTransformOnStage(double cadX, double cadY)
        {
            var offset = GetTargetPositionOnStage(cadX, cadY);
            return GeometricArithmeticModule.CombineTransformations(
                GeometricArithmeticModule.GetRotationTransformationMatrix(
                    0, 0, _originThetaRadians
                ),
                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                    offset.X, offset.Y
                )
            );
        }

        public async Task<bool> ActivateConfigurationForLayer(MRecipeDeviceLayer layer, CancellationToken ctIn)
        {
            var processConfig = GetProcessConfiguration(layer);

            if (!await processConfig.ActivateConfiguration(ctIn))
                throw new Exception("Failed to activate the selected process mode");

            if (!await processConfig.EnableConfigForProcessing(ctIn))
            {
                await processConfig.DisableConfigForProcessing(ctIn);
                throw new Exception("Failed to enable configuration for the selected process mode");
            }

            // load the process parameters into hardware
            processConfig.ProcessParameterFileManager.LoadFromfile(layer.ProcessParametersFilePath);
            if (!await processConfig.LoadProcessParametersToHardware(ctIn))
                throw new Exception($"Failed to load the process's parameters for layer {layer.Tag}");

            return true;
        }

        public async Task<bool> DeactivateConfigurationForLayer(MRecipeDeviceLayer layer, CancellationToken ctIn)
        {
            var processConfig = GetProcessConfiguration(layer);

            if (!await processConfig.DisableConfigForProcessing(ctIn))
                throw new Exception("Failed to disable configuration for the selected process mode");

            return true;
        }

        public async Task<bool> RunPreExecutionTasks(CancellationToken ctIn)
        {
            ReloadOrigin();

            if (!await _prePost.PreRecipeExecution())
                throw new Exception("Failed to run pre recipe execution tasks");

            return true;
        }

        public async Task<bool> RunPostExecutionTasks(CancellationToken ctIn)
        {
            await _prePost.PostRecipeExecution();

            return true;
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

        #endregion
    }
}
