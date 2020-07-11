using MarkGeometriesLib.Classes.Generics;
using MRecipeStructure.Classes.MRecipeStructure;
using MRecipeStructure.Dialogs.ProcessRecipeUtils;
using MSolvLib;
using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using MSolvLib.MarkGeometry;
using Prism.Commands;
using SharpGLShader;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;

namespace MRecipeStructure.Dialogs.DialogViewModel
{
    public class RunRecipeDialogViewModel : ViewModel
    {
        #region Section: Private Properties

        // for display logs
        private System.Windows.Controls.RichTextBox _logsRichTextBox = null;

        private RunRecipeDialogModel _model;
        private List<IDisposable> _subscriptions = new List<IDisposable>();

        // for managing duplicate tasks
        private TerminableTaskExecutor _terminableTaskExecutor;
        private TerminableTaskExecutor _processTerminableTaskExecutor;

        // for loading cached DXFs
        private CachedLoader<List<IMarkGeometry>> _dxfCachedLoader;

        // for finding fiducials
        private FiducialFinder _fiducialFinder;

        #endregion

        #region Section: Public Properties

        public long RecipeGeometriesCount { get; set; } = 0;
        public MGLShader MShader { get; set; } = new MGLShader();
        public GeometryExtents<double> RecipeExtents { get; set; } = GeometryExtents<double>.CreateDefaultDouble();

        #endregion

        #region Section: Data Binding

        private double _processProgress;

        public double ProcessProgress
        {
            get { return _processProgress; }
            set 
            { 
                _processProgress = value;
                NotifyPropertyChanged();
            }
        }


        private bool _isLoading = false;

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotLoading));
            }
        }

        public bool IsNotLoading
        {
            get
            {
                return !(IsLoading || IsRunning);
            }
        }

        public bool IsNotRunning
        {
            get
            {
                return !IsRunning;
            }
        }

        private bool _isRunning;

        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                _isRunning = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotLoading));
                NotifyPropertyChanged(nameof(IsNotRunning));
            }
        }

        private bool _isPaused;

        public bool IsPaused
        {
            get { return _isPaused; }
            set 
            { 
                _isPaused = value;
                NotifyPropertyChanged();
            }
        }

        private double _mouseX = 0;

        public double MouseX
        {
            get { return _mouseX; }
            set
            {
                _mouseX = Math.Round(value, 4);
                NotifyPropertyChanged();
            }
        }

        private double _mouseY = 0;

        public double MouseY
        {
            get { return _mouseY; }
            set
            {
                _mouseY = Math.Round(value, 4);
                NotifyPropertyChanged();
            }
        }

        private double _drawingExtentsX = 0;

        public double DrawingExtentsX
        {
            get { return _drawingExtentsX; }
            set
            {
                _drawingExtentsX = Math.Round(value, 4);
                NotifyPropertyChanged();
            }
        }

        private double _drawingExtentsY = 0;

        public double DrawingExtentsY
        {
            get { return _drawingExtentsY; }
            set
            {
                _drawingExtentsY = Math.Round(value, 4);
                NotifyPropertyChanged();
            }
        }

        private int _drawingCount = 0;

        public int DrawingCount
        {
            get { return _drawingCount; }
            set
            {
                _drawingCount = value;
                NotifyPropertyChanged();
            }
        }

        private string _estimatedTimeRemaining;

        public string EstimatedTimeRemaining
        {
            get { return _estimatedTimeRemaining; }
            set
            {
                _estimatedTimeRemaining = value;
                NotifyPropertyChanged();
            }
        }


        private MRecipe _recipeVm;

        public MRecipe RecipeVm
        {
            get { return _recipeVm; }
            set
            {
                _recipeVm = value;
                NotifyPropertyChanged();
            }
        }

        private ObservableCollection<RecipeProcessEntityInfo> _recipeInfo;

        public ObservableCollection<RecipeProcessEntityInfo> RecipeInfo
        {
            get { return _recipeInfo; }
            set
            {
                _recipeInfo = value;
                NotifyPropertyChanged();
            }
        }

        private RecipeProcessEntityInfo _selectedSubProcessInfo;

        public RecipeProcessEntityInfo SelectedSubProcessInfo
        {
            get { return _selectedSubProcessInfo; }
            set
            {
                _selectedSubProcessInfo = value;
                NotifyPropertyChanged();

                Render();
            }
        }

        #endregion

        #region Section: Delegate Commands

        public DelegateCommand LoadRecipeCommand { get; private set; }
        public DelegateCommand StartRecipeCommand { get; private set; }
        public DelegateCommand PauseContinueCommand { get; private set; }
        public DelegateCommand RestartCommand { get; private set; }
        public DelegateCommand AbortRecipeCommand { get; private set; }
        public DelegateCommand FilterLogsCommand { get; private set; }

        #endregion

        #region Section: Constructors

        public RunRecipeDialogViewModel(RunRecipeDialogModel modelIn, MRecipe recipe)
        {
            _model = modelIn;
            _terminableTaskExecutor = new TerminableTaskExecutor();
            _processTerminableTaskExecutor = new TerminableTaskExecutor();
            _dxfCachedLoader = new CachedLoader<List<IMarkGeometry>>();
            RecipeInfo = new ObservableCollection<RecipeProcessEntityInfo>();
            _fiducialFinder = new FiducialFinder();

            IsPaused = false;
            IsRunning = false;

            try
            {
                IsLoading = true;
                SelectedSubProcessInfo = null;
                RecipeVm = recipe;
                GenerateRecipeInfo();
                Render();
            }
            catch (Exception exp)
            {
                _model.Log(exp);
                _model.Log("Failed to load recipe");
            }
            finally
            {
                IsLoading = false;
            }

            StartRecipeCommand = new DelegateCommand(
                () => {
                    StartProcess();
                }
            );

            RestartCommand = new DelegateCommand(
                async () =>
                {
                    if (IsRunning)
                    {
                        Abort();

                        // wait a second for running tasks to stop
                        await Task.Delay(1000);
                    }

                    StartProcess();
                }
            );

            PauseContinueCommand = new DelegateCommand(
                () =>
                {
                    try
                    {
                        if (!IsPaused)
                        {
                            DispatcherMessageBox.ShowBox(
                                "The process will be paused when it is safe to do so.",
                                "Pause Request"
                            );

                            PrintLog("Process Paused ...");
                        }
                        else
                        {
                            PrintLog("Continue Process");
                        }

                        IsPaused = !IsPaused;
                    }
                    finally
                    {

                    }
                }
            );

            AbortRecipeCommand = new DelegateCommand(
                () =>
                {
                    Abort();
                }
            );
        }

        #endregion

        #region Section: Class Methods

        public void StartProcess()
        {
            _processTerminableTaskExecutor.CancelCurrentAndRun(
                async (ctIn) =>
                {
                    try
                    {
                        IsRunning = true;
                        ClearLogs();
                        PrintLog("Starting process...");

                        #region Section: Executing Process
                        
                        ProcessProgress = 0;
                        double progressIncrement = (1.0 / RecipeInfo.Count) * 100d;

                        // reset all
                        _fiducialFinder.Reset();
                        foreach (var info in RecipeInfo)
                        {
                            info.State = EntityState.WAITING;
                        }

                        int counter = 0;
                        foreach (var info in RecipeInfo)
                        {
                            // wait if paused
                            while (
                                IsPaused && 
                                !ctIn.IsCancellationRequested
                            )
                            { await Task.Delay(100); }

                            // process layer
                            if (!await _model.ProcessLayer(
                                info,
                                FetchDXF(info.Layer),
                                FetchDXFParameters(info.Layer),
                                async (taskHandler) => await _fiducialFinder.GetAbsoluteTransformFromStageOrigin(taskHandler, _model.GetCam2Beam(), RecipeVm, info.Layer, ctIn),
                                () => IsPaused,
                                (message) => PrintLog(message),
                                (message) => PrintError(message),
                                ctIn
                            ))
                            {
                                throw new Exception($"Failed while processing layer {counter}");
                            }

                            ctIn.ThrowIfCancellationRequested();
                            ProcessProgress += progressIncrement;
                            counter += 1;
                        }

                        #endregion

                        PrintLog("Process completed successfully...");
                    }
                    catch (Exception exp)
                    {
                        PrintError("Process failed.");
                        PrintLog(exp.Message);
                    }
                    finally
                    {
                        IsRunning = false;
                    }
                }
            );
        }

        public void Abort()
        {
            try
            {
                PrintLog("Aborting process...");
                _processTerminableTaskExecutor.Cancel();
                _model.Abort();
            }
            finally
            {
                IsRunning = false;
                PrintLog("Process aborted.");
            }
        }

        public void GenerateRecipeInfo()
        {
            RecipeInfo.Clear();

            int index = 0;
            MRecipe.BeginGetAllLayers(RecipeVm, (layer) =>
            {
                if (layer.TileDescriptions.Count() <= 0)
                {
                    layer.GenerateTileDescriptionsFromSettings(
                        GeometricArithmeticModule.CalculateExtents(FetchDXF(layer))
                    );
                }

                var info = new RecipeProcessEntityInfo()
                {
                    Index = index++,
                    Layer = layer,
                    EstimatedTime = EstimateProcessTime(layer)
                };

                RecipeInfo.Add(info);
            });

            // fetch recipe's count and extents
            (RecipeGeometriesCount, RecipeExtents) = MRecipe.CalculateExtents(RecipeVm);
        }

        public void Render()
        {
            _terminableTaskExecutor.CancelCurrentAndRun(
                (ctIn) =>
                {
                    // reset shader
                    MShader.Reset();

                    // stop if new render is requested
                    ctIn.ThrowIfCancellationRequested();

                    // as we only render one layer at a time
                    // choose the closest layer in the recipe tree
                    var layer = GetClosestLayer();

                    if (layer != null)
                    {
                        // extract device's absolute transform
                        var baseTransform = MRecipe.GetRelativeTransform(RecipeVm, layer);

                        // calculate layer's transform
                        var transform = GeometricArithmeticModule.CombineTransformations(
                            baseTransform,
                            layer.TransformInfo.ToMatrix4x4()
                        );

                        // fetch pattern
                        var pattern = FetchDXF(layer);

                        for (int j = 0; j < pattern?.Count; j++)
                        {
                            // get copy of geometry
                            var geometry = (IMarkGeometry)pattern[j].Clone();

                            // apply transform to geometry
                            geometry.Transform(transform);

                            // add geometry
                            MShader.AddDefault(geometry);
                        }

                        // overlay tiles
                        foreach (var tile in layer.TileDescriptions)
                        {
                            var rect = (MarkGeometryRectangle)tile;
                            
                            // apply transform to rect
                            rect.Transform(transform);

                            // add geometry
                            MShader.AddDefault(rect, MGLShader.Violet);
                        }
                    }

                    // stop if new render is requested
                    ctIn.ThrowIfCancellationRequested();

                    MShader.Render();
                    UpdateStats();
                }
            );
        }

        public void UpdateStats()
        {
            double totalTime = 0;

            foreach (var entity in RecipeInfo)
                totalTime += (100 - entity.ProgressPercentage) * entity.EstimatedTime.TotalSeconds;

            var totalTimeSpan = TimeSpan.FromSeconds(totalTime);
            EstimatedTimeRemaining = $"{totalTimeSpan.Hours:00}:{totalTimeSpan.Minutes:00}:{totalTimeSpan.Seconds:00}";

            DrawingExtentsX = MShader.Width;
            DrawingExtentsY = MShader.Height;
            DrawingCount = MShader.Count;
        }

        public void UpdateMousePosition()
        {
            // update mouse to reflect the reference point
            MouseX = MShader.Mouse.X;
            MouseY = MShader.Mouse.Y;
        }

        public void UpdateLogsRichTextBox(System.Windows.Controls.RichTextBox richTextBoxIn)
        {
            _logsRichTextBox = richTextBoxIn;

            PrintLog($"Recipe contains: {RecipeGeometriesCount} geometry/geometries with extents: {RecipeExtents}");
        }

        #endregion

        #region Section: Helpers

        public void PrintLog(string message)
        {
            _model.Log(message);
            _logsRichTextBox?.Dispatcher.Invoke(() => {
                _logsRichTextBox?.Document.Blocks.Add(
                    new Paragraph(new Run($"{message}"))
                    {
                        Foreground = Brushes.DarkBlue
                    }
                );
            });
        }

        public void PrintError(string message)
        {
            _model.Log(message);
            _logsRichTextBox?.Dispatcher.Invoke(()=> {
                _logsRichTextBox?.Document.Blocks.Add(
                    new Paragraph(new Run($"{message}"))
                    {
                        Foreground = Brushes.Red
                    }
                );
            });
        }

        public void ClearLogs()
        {
            _logsRichTextBox.Dispatcher.Invoke(() => {
                _logsRichTextBox.Document.Blocks.Clear();
            });
        }

        public IMarkParametersComplete FetchDXFParameters(MRecipeDeviceLayer layer)
        {
            var processConfig = _model.GetProcessConfiguration(layer.TargetProcessMode);
            processConfig?.ProcessParameterFileManager.LoadFromfile(layer.ProcessParametersFilePath);
            return processConfig?.ProcessParameterFileManager.ProcessParameters as IMarkParametersComplete;
        }

        public List<IMarkGeometry> FetchDXF(MRecipeDeviceLayer layer)
        {
            return FetchDXF(layer.PatternFilePath);
        }

        public List<IMarkGeometry> FetchDXF(string filePathIn)
        {
            if (!File.Exists(filePathIn))
                return null;

            // attempt to load from cache if it exists else load using getter
            return _dxfCachedLoader.TryGet(filePathIn, () => GeometricArithmeticModule.ExtractGeometriesFromDXF(filePathIn));
        }

        public MRecipeDeviceLayer GetClosestLayer()
        {
            if (RecipeVm == null)
                return null;
            else if (SelectedSubProcessInfo == null)
                return RecipeInfo.FirstOrDefault()?.Layer;

            return SelectedSubProcessInfo?.Layer;
        }

        public IProcessConfigurationTasksHandler GetClosestConfigurationTasksHandler(MRecipeBaseNode startNode)
        {
            if (startNode is MRecipePlate plate)
                return _model.GetProcessConfigurationTaskHandler(
                    plate.Devices.FirstOrDefault()?.Layers.FirstOrDefault()?.TargetProcessMode
                );
            else if (startNode is MRecipeDevice device)
                return _model.GetProcessConfigurationTaskHandler(
                    device.Layers.FirstOrDefault()?.TargetProcessMode
                );
            else if (startNode is MRecipeDeviceLayer layer)
                return _model.GetProcessConfigurationTaskHandler(
                    layer.TargetProcessMode
                );

            return _model.GetProcessConfigurationTaskHandler(null);
        }

        public TimeSpan EstimateProcessTime(MRecipeDeviceLayer layer)
        {
            const double kConst = 0.000001;
            double totalTaktTime = 0;

            IMarkParametersComplete processParams = FetchDXFParameters(layer);

            if (processParams == null)
                return TimeSpan.Zero;

            int numOfJoints = 0;
            double jumpDistance = 0;
            double markDistance = 0;
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            MarkGeometryPoint lastPosition = null;
            foreach (var geometry in FetchDXF(layer))
            {
                if (geometry is MarkGeometryPoint point)
                {
                    if (lastPosition != null)
                        jumpDistance += GeometricArithmeticModule.ABSMeasure2D(lastPosition, point);

                    markDistance += point.Perimeter;
                    lastPosition = point;
                }
                else if (geometry is MarkGeometryLine line)
                {
                    if (lastPosition != null)
                        jumpDistance += GeometricArithmeticModule.ABSMeasure2D(lastPosition, line.StartPoint);

                    markDistance += line.Perimeter;
                    lastPosition = line.EndPoint;
                }
                else if (geometry is MarkGeometryCircle circle)
                {
                    if (lastPosition != null)
                        jumpDistance += GeometricArithmeticModule.ABSMeasure2D(
                            lastPosition,
                            GeometricArithmeticModule.GetPointAtPosition(
                                circle, 0
                            )
                        );

                    markDistance += circle.Perimeter;
                    numOfJoints += Math.Max(circle.VertexCount - 1, 0);
                    lastPosition = GeometricArithmeticModule.GetPointAtPosition(circle, 1.0);
                }
                else if (geometry is MarkGeometryArc arc)
                {
                    if (lastPosition != null)
                        jumpDistance += GeometricArithmeticModule.ABSMeasure2D(lastPosition, arc.StartPoint);

                    markDistance += arc.Perimeter;
                    numOfJoints += Math.Max(arc.VertexCount - 1, 0);
                    lastPosition = arc.EndPoint;
                }
                else if (geometry is MarkGeometryPath path)
                {
                    if (lastPosition != null)
                        jumpDistance += GeometricArithmeticModule.ABSMeasure2D(lastPosition, path.StartPoint);

                    markDistance += path.Perimeter;
                    numOfJoints += Math.Max(path.Points.Count - 1, 0);
                    lastPosition = path.EndPoint;
                }

                if (geometry.Extents.MinX < minX)
                    minX = geometry.Extents.MinX;

                if (geometry.Extents.MinY < minY)
                    minY = geometry.Extents.MinY;

                if (geometry.Extents.MaxX > maxX)
                    maxX = geometry.Extents.MaxX;

                if (geometry.Extents.MaxY > maxY)
                    maxY = geometry.Extents.MaxY;
            }

            double taktTime = (
                (markDistance / processParams.MarkSpeed) +
                (jumpDistance / processParams.JumpSpeed) +
                (kConst * numOfJoints * (processParams.JumpDelay_us + processParams.MarkDelay - processParams.Nprev))
            );

            // account for repeats and stepping between tiles - convert millisecond to second
            totalTaktTime += ((taktTime * processParams.Passes) + (0.001 * layer.TileDescriptions.Count() * processParams.SettlingTimems));

            return TimeSpan.FromSeconds(Math.Round(totalTaktTime, 4));
        }

        public int CountNumberOfFiducials(MRecipe recipeIn)
        {
            int count = 0;

            MRecipe.BeginGetNodes(
                recipeIn,
                (node) => {
                    count += node.Fiducials.Count;
                }
            );

            return count;
        }

        public async Task<Matrix4x4> GetAbsoluteTransform(MRecipeBaseNode node, Matrix4x4 parentTransform, CancellationToken ctIn)
        {
            if (parentTransform == null)
                parentTransform = GeometricArithmeticModule.GetDefaultTransformationMatrix();

            // node has no fiducials, hences it's position 
            // is relative to its parent
            if (node.Fiducials.Count <= 0)
                return GeometricArithmeticModule.CombineTransformations(
                    parentTransform,
                    node.TransformInfo.ToMatrix4x4()
                );

            // attempt to retrieve tasksHandler; handles process specific stage motion
            // camera, find n centre, etc.
            IProcessConfigurationTasksHandler tasksHandler = GetClosestConfigurationTasksHandler(node);
            if (tasksHandler == null)
                throw new Exception("Failed to retrieve a Task Handler for the closest process mode");

            // buffer to store estimated and measured fiducials
            var estimatedPoints = new List<MarkGeometryPoint>();
            var measuredPoints = new List<MarkGeometryPoint>();

            for (int i = 0; i < node.Fiducials.Count; i++)
            {
                var estimatedPosition = new MarkGeometryPoint(
                    node.Fiducials[i].X, node.Fiducials[i].Y
                );

                // transform to parents space
                estimatedPosition.Transform(parentTransform);
                estimatedPoints.Add(estimatedPosition);

                // move camera to position
                await tasksHandler.FocusCameraAtXY(
                    estimatedPosition.X,
                    estimatedPosition.Y,
                    ctIn
                );

                // get measured position
                var results = await tasksHandler.TakeMeasurement();

                // handle results not found
                if (!results.Found)
                    throw new Exception("Failed to find fiducial");

                // store measured point
                measuredPoints.Add(
                    new MarkGeometryPoint(
                        results.Position.X,
                        results.Position.Y
                    )
                );
            }

            return GeometricArithmeticModule.CombineTransformations(
                parentTransform,
                MAlignmentCalculator.GetAlignmentTransform(
                    node.AlignmentType,
                    estimatedPoints,
                    measuredPoints
                )
            );
        }

        #endregion
    }
}
