using HelixToolkit.Wpf;
using MarkGeometriesLib.Classes.Generics;
using Microsoft.Win32;
using MRecipeStructure.Classes.MRecipeStructure;
using MRecipeStructure.Classes.MRecipeStructure.Utils;
using MSolvLib;
using MSolvLib.Classes.ProcessConfiguration;
using MSolvLib.DialogForms;
using MSolvLib.MarkGeometry;
using Prism.Commands;
using SharpGLShader;
using STLSlicer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MRecipeStructure.Dialogs.DialogViewModel
{
    public class ImportSTLDialogViewModel : ViewModel
    {
        #region Section: Private Properties

        // for managing duplicate tasks
        private TerminableTaskExecutor _terminableTaskExecutor;

        // buffer for geometries
        private List<IMarkGeometry> _fiducialPattern;
        private List<MSTLSlice> _stlSlices;
        private double[][] _colours = new double[][] { MGLShader.White, MGLShader.Cyan, MGLShader.Blue, MGLShader.Violet, MGLShader.Red, MGLShader.Yellow, MGLShader.Green };
        private double[] _tileColor = new double[] { 1.0, 0.7, 0.7, 0.5 };
        public double[] _fiducialColor = MGLShader.Cyan;

        // for STL
        private HelixViewport3D _viewport3D = null;
        private Model3D _stlModel;
        private Model3DGroup _modelGroup;
        private List<GeometryModel3D> _slicePlanes;
        private static readonly Material PlaneMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(125, 50, 50, 200)));
        private static readonly DiffuseMaterial ModelMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Green));

        #endregion

        #region Section: Public Properties

        public MGLShader MShader;
        public string DefaultRecipeDirectory;
        public string DefaultStlDirectory;

        #endregion

        #region Section: Data Binding

        // for grid base
        private Point3D _gridLinesOrigin = new Point3D();

        public Point3D GridLinesOrigin
        {
            get { return _gridLinesOrigin; }
            set
            {
                _gridLinesOrigin = value;
                NotifyPropertyChanged();
            }
        }

        private double _gridLinesSize = 10;

        public double GridLinesSize
        {
            get { return _gridLinesSize; }
            set
            {
                _gridLinesSize = value;
                NotifyPropertyChanged();
            }
        }

        private double _gridLinesGridPitch = 1;

        public double GridLinesGridPitch
        {
            get { return _gridLinesGridPitch; }
            set
            {
                _gridLinesGridPitch = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(GridLinesGridThickness));
            }
        }

        public double GridLinesGridThickness
        {
            get
            {
                return 0.01 * GridLinesGridPitch;
            }
        }

        private MVertexViewModel _stlModelReferencePoint;

        public MVertexViewModel StlModelReferencePoint
        {
            get { return _stlModelReferencePoint; }
            set
            {
                _stlModelReferencePoint = value;
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
                return !IsLoading;
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

        private double _sliceXSize = 0;

        public double SliceXSize
        {
            get { return _sliceXSize; }
            set
            {
                _sliceXSize = Math.Round(value, 4);
                NotifyPropertyChanged();
            }
        }

        private double _sliceYSize = 0;

        public double SliceYSize
        {
            get { return _sliceYSize; }
            set
            {
                _sliceYSize = Math.Round(value, 4);
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

        private IProcessConfiguration _targetProcessMode;

        public IProcessConfiguration TargetProcessMode
        {
            get { return _targetProcessMode; }
            set
            {
                _targetProcessMode = value;
                NotifyPropertyChanged();
            }
        }


        private ObservableCollection<IProcessConfiguration> _availableProcessModes;

        public ObservableCollection<IProcessConfiguration> AvailableProcessModes
        {
            get { return _availableProcessModes; }
            set
            {
                _availableProcessModes = value;
                NotifyPropertyChanged();
            }
        }

        private string _patternFilePath;

        public string PatternFilePath
        {
            get { return _patternFilePath; }
            set
            {
                _patternFilePath = value;
                NotifyPropertyChanged();
            }
        }


        private string _processParametersFilePath;

        public string ProcessParametersFilePath
        {
            get { return _processParametersFilePath; }
            set
            {
                _processParametersFilePath = value;
                NotifyPropertyChanged();
            }
        }

        private MTileSettings _tileSettings;

        public MTileSettings TileSettings
        {
            get { return _tileSettings; }
            set
            {
                _tileSettings = value;
                NotifyPropertyChanged();
            }
        }

        private MHatchSettings _hatchSettings;

        public MHatchSettings HatchSettings
        {
            get { return _hatchSettings; }
            set
            {
                _hatchSettings = value;
                NotifyPropertyChanged();
            }
        }

        private ObservableCollection<MFiducialInfo> _fiducials;

        public ObservableCollection<MFiducialInfo> Fiducials
        {
            get { return _fiducials; }
            set
            {
                _fiducials = value;
                NotifyPropertyChanged();
            }
        }

        private MAlignmentType _alignmentType;

        public MAlignmentType AlignmentType
        {
            get { return _alignmentType; }
            set
            {
                _alignmentType = value;
                NotifyPropertyChanged();
            }
        }

        private Model3D _threeDeeModel;

        public Model3D ThreeDeeModel
        {
            get { return _threeDeeModel; }
            set
            {
                _threeDeeModel = value;
                NotifyPropertyChanged();
            }
        }

        private double _stitchTolerance = 0.01;

        public double StitchTolerance
        {
            get { return _stitchTolerance; }
            set
            {
                _stitchTolerance = Math.Round(Math.Max(value, 0.001), 4);
                NotifyPropertyChanged();
            }
        }

        private double _sliceThickness = 1;

        public double SliceThickness
        {
            get { return _sliceThickness; }
            set
            {
                _sliceThickness = Math.Round(value, 4);
                NotifyPropertyChanged();
            }
        }

        private int _sliceCount = 0;

        public int SliceCount
        {
            get { return _sliceCount; }
            set
            {
                _sliceCount = value;
                NotifyPropertyChanged();
            }
        }

        private int _currentSlice = 0;

        public int CurrentSlice
        {
            get { return _currentSlice; }
            set
            {
                _currentSlice = value;
                NotifyPropertyChanged();
            }
        }

        private int _numberOfSlices = 0;

        public int NumberOfSlices
        {
            get { return _numberOfSlices; }
            set
            {
                _numberOfSlices = value;
                NotifyPropertyChanged(nameof(NumberOfSlicesIndicator));
            }
        }

        public int NumberOfSlicesIndicator
        {
            get
            {
                return NumberOfSlices - 1;
            }
        }


        #endregion

        #region Section: Delegate Command

        public DelegateCommand SelectPatternFileCommand { get; set; }
        public DelegateCommand SelectParametersFileCommand { get; set; }
        public DelegateCommand AddFiducialCommand { get; set; }
        public DelegateCommand<MFiducialInfo> DeleteFiducialCommand { get; set; }
        public DelegateCommand DeleteAllFiducialsCommand { get; set; }
        public DelegateCommand RefreshCommand { get; set; }
        public DelegateCommand HatchCurrentLayerCommand { get; set; }
        public DelegateCommand TileCurrentLayerCommand { get; set; }
        public DelegateCommand HatchAllLayersCommand { get; set; }
        public DelegateCommand TileAllLayersCommand { get; set; }
        public DelegateCommand ShowNextSliceCommand { get; set; }
        public DelegateCommand ShowPreviousSliceCommand { get; set; }

        #endregion

        public ImportSTLDialogViewModel(IEnumerable<IProcessConfiguration> availableProcessConfigurations, string defaultSTLDirectory, string defaultRecipeDirectory)
        {
            MShader = new MGLShader();
            _terminableTaskExecutor = new TerminableTaskExecutor();

            DefaultRecipeDirectory = defaultRecipeDirectory;
            DefaultStlDirectory = defaultSTLDirectory;

            _modelGroup = new Model3DGroup();
            _stlSlices = new List<MSTLSlice>();
            _slicePlanes = new List<GeometryModel3D>();
            StlModelReferencePoint = new MVertexViewModel();
            HatchSettings = new MHatchSettings();
            AlignmentType = MAlignmentType.TypeAuto;
            TileSettings = new MTileSettings();
            Fiducials = new ObservableCollection<MFiducialInfo>();
            AvailableProcessModes = new ObservableCollection<IProcessConfiguration>(availableProcessConfigurations);

            SelectPatternFileCommand = new DelegateCommand(
                async () =>
                {
                    var dialog = new OpenFileDialog();
                    dialog.Filter = "STL files (*.stl)|*.stl";
                    dialog.AddExtension = true;
                    dialog.InitialDirectory = defaultSTLDirectory;

                    if (dialog.ShowDialog() != true)
                        return;

                    PatternFilePath = dialog.FileName;
                    await FetchSTL(PatternFilePath);
                    Render();
                }
            );

            SelectParametersFileCommand = new DelegateCommand(
                () =>
                {
                    if (TargetProcessMode == null)
                    {
                        MessageBox.Show(
                            @"Invalid process mode selected, please select a valid process mode.",
                            "Select a Process Mode"
                        );

                        return;
                    }

                    var dialog = new MarkParamEditorDialog(
                        TargetProcessMode?.ProcessParameterFileManager,
                        ProcessParametersFilePath
                    );

                    if (dialog.ShowDialog() != true)
                        return;

                    ProcessParametersFilePath = dialog.ParamFileManager.FilePath;
                }
            );

            AddFiducialCommand = new DelegateCommand(
                () =>
                {
                    Fiducials.Add(new MFiducialInfo() { Index = Fiducials.Count });
                    Render();
                }
            );

            DeleteFiducialCommand = new DelegateCommand<MFiducialInfo>(
                (fiducial) =>
                {
                    Fiducials.Remove(fiducial);

                    // update Index
                    for (int i = 0; i < Fiducials.Count; i++)
                        Fiducials[i].Index = i;

                    Render();
                }
            );

            DeleteAllFiducialsCommand = new DelegateCommand(
                () =>
                {
                    Fiducials.Clear();
                    Render();
                }
            );

            RefreshCommand = new DelegateCommand(
                () =>
                {
                    Render();
                }
            );

            ShowPreviousSliceCommand = new DelegateCommand(() =>
            {
                if (NumberOfSlices == 0)
                {
                    CurrentSlice = 0;
                    return;
                }
                else if (CurrentSlice == 0)
                {
                    CurrentSlice = NumberOfSlices;
                }

                CurrentSlice = Math.Abs((CurrentSlice - 1) % NumberOfSlices);
                ShowSlice(CurrentSlice);
            });

            ShowNextSliceCommand = new DelegateCommand(() =>
            {
                if (NumberOfSlices == 0)
                {
                    CurrentSlice = 0;
                    return;
                }

                CurrentSlice = (CurrentSlice + 1) % NumberOfSlices;
                ShowSlice(CurrentSlice);
            });

            HatchCurrentLayerCommand = new DelegateCommand(async () =>
            {
                if (NumberOfSlices == 0)
                {
                    DispatcherMessageBox.ShowBox(
                        "There's no slice to hatch"
                    );
                    return;
                }

                var response = DispatcherMessageBox.ShowBox(
                    "This could take a while, do you wish to continue",
                    "Warning",
                    MessageBoxButton.YesNo
                );

                if (response != MessageBoxResult.Yes)
                    return;

                try
                {
                    IsLoading = true;

                    await HatchSlice(CurrentSlice);

                    Render();
                }
                finally
                {
                    IsLoading = false;
                }
            });

            TileCurrentLayerCommand = new DelegateCommand(async () =>
            {
                if (NumberOfSlices == 0)
                {
                    DispatcherMessageBox.ShowBox(
                        "There's no slice to tile"
                    );
                    return;
                }

                try
                {
                    IsLoading = true;

                    await TileSlice(CurrentSlice);

                    Render();
                }
                finally
                {
                    IsLoading = false;
                }
            });

            HatchAllLayersCommand = new DelegateCommand(async () =>
            {
                if (NumberOfSlices == 0)
                {
                    DispatcherMessageBox.ShowBox(
                        "There's no slice to hatch"
                    );
                    return;
                }

                var response = DispatcherMessageBox.ShowBox(
                    "This could take a while, do you wish to continue",
                    "Warning",
                    MessageBoxButton.YesNo
                );

                if (response != MessageBoxResult.Yes)
                    return;

                try
                {
                    IsLoading = true;

                    await HatchAllSlices();

                    Render();
                }
                finally
                {
                    IsLoading = false;
                }
            });

            TileAllLayersCommand = new DelegateCommand(async () =>
            {
                if (NumberOfSlices == 0)
                {
                    DispatcherMessageBox.ShowBox(
                        "There's no slice to hatch"
                    );
                    return;
                }

                try
                {
                    IsLoading = true;

                    await TileAllSlices();

                    Render();
                }
                finally
                {
                    IsLoading = false;
                }
            });
        }

        public void Render()
        {
            _terminableTaskExecutor.CancelCurrentAndRun(
                (ctIn) =>
                {
                    // reset shader
                    MShader.Reset();

                    if (_stlSlices != null)
                    {
                        // add geometries
                        if (CurrentSlice >= 0 && CurrentSlice < _stlSlices.Count && _stlSlices[CurrentSlice] != null)
                        {
                            var stlSlice = _stlSlices[CurrentSlice];

                            // tiles
                            for (int i = 0; i < stlSlice.Tiles?.Count; i++)
                                MShader.AddDefault(stlSlice.Tiles[i], _tileColor);

                            // contours
                            for (int i = 0; i < stlSlice.ContourLines?.Count; i++)
                                MShader.AddDefault(stlSlice.ContourLines[i], MGLShader.Green);

                            // hatches
                            for (int i = 0; i < stlSlice.HatchLines?.Count; i++)
                                MShader.AddDefault(stlSlice.HatchLines[i], MGLShader.White);
                        }

                        // stop if new render is requested
                        ctIn.ThrowIfCancellationRequested();

                        // get base transform
                        var baseTransform = GeometricArithmeticModule.GetTranslationTransformationMatrix(
                            StlModelReferencePoint.X, StlModelReferencePoint.Y
                        );

                        // render fiducials in parent's reference frame
                        foreach (var fiducial in Fiducials)
                        {
                            var transform = GeometricArithmeticModule.CombineTransformations(
                                baseTransform,
                                GeometricArithmeticModule.GetTranslationTransformationMatrix(
                                    fiducial.X, fiducial.Y, fiducial.Z
                                )
                            );

                            foreach (var geometry in _fiducialPattern)
                            {
                                var clone = (IMarkGeometry)geometry.Clone();
                                clone.Transform(transform);
                                MShader.AddDefault(clone, _fiducialColor);
                            }
                        }
                    }

                    // stop if new render is requested
                    ctIn.ThrowIfCancellationRequested();

                    MShader.Render();
                }
            );
        }

        public MRecipe GenerateRecipe(string outputFileName)
        {
            if (
                _stlModel == null ||
                _stlSlices == null ||
                _stlSlices.Count <= 0 ||
                !File.Exists(PatternFilePath)
            )
                throw new Exception("Missing or Invalid pattern");

            try
            {
                IsLoading = true;

                string markParametersFilePath = TargetProcessMode?.ProcessParameterFileManager?.FilePath;

                var recipeName = Path.GetFileNameWithoutExtension(outputFileName);
                var dataDirectory = Path.Combine(
                    Path.GetDirectoryName(outputFileName),
                    $"{recipeName} Recipe Data"
                );

                // save recipe in it's Data Directory
                var recipeFilePath = Path.Combine(
                    dataDirectory, Path.GetFileName(outputFileName)
                );

                // overwrite data directory if it exists
                if (Directory.Exists(dataDirectory))
                    Directory.Delete(dataDirectory);
                Directory.CreateDirectory(dataDirectory);

                // copy the marking parameters to the data directory
                var newMarkParametersFilePath = Path.Combine(
                    dataDirectory, Path.GetFileName(markParametersFilePath)
                );

                // copy the marking parameters to the data directory
                if (File.Exists(newMarkParametersFilePath))
                    File.Delete(newMarkParametersFilePath);
                File.Copy(markParametersFilePath, newMarkParametersFilePath);

                // create device
                var device = new MRecipeDevice("Slices");

                // centre device about it's reference point
                device.TransformInfo.OffsetX = -StlModelReferencePoint.X;
                device.TransformInfo.OffsetY = -StlModelReferencePoint.Y;

                // create template layers so that
                // they can be updated in parallel
                for (int i = 0; i < _stlSlices.Count; i++)
                    device.Layers.Add(new MRecipeDeviceLayer());

                // generate and export DXFs
                Parallel.For(0, _stlSlices.Count, (i) =>
                {
                    // device - representing slices
                    var layer = device.Layers[i];
                    layer.Tag = $"Slice {i}";
                    layer.TargetProcessMode = TargetProcessMode?.Name.EnglishValue;
                    layer.ProcessParametersFilePath = markParametersFilePath;
                    layer.PatternFilePath = Path.Combine(dataDirectory, $"{layer.Tag}.dxf");
                    layer.TileSettings = TileSettings;

                    for (int j = 0; j < _stlSlices[i].Tiles.Count; j++)
                        layer.TileDescriptions.Add(
                            new MTileDescription(j, _stlSlices[i].Tiles[j])
                        );

                    _stlSlices[i].SaveAsDXF(layer.PatternFilePath);
                });

                // create recipe
                var recipe = new MRecipe(new MRecipePlate(recipeName, device));

                // set alignment type
                recipe.Plates[0].AlignmentType = AlignmentType;

                // add alignment fiducials to recipe plate
                recipe.Plates[0].Fiducials = new ObservableCollection<MFiducialInfo>(Fiducials);

                // update recipe's parents
                recipe.UpdateParents();

                recipe.Save(Path.Combine(dataDirectory, recipeFilePath));

                return recipe;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void UpdateMousePosition()
        {
            // update mouse to reflect the reference point
            MouseX = MShader.Mouse.X - StlModelReferencePoint.X;
            MouseY = MShader.Mouse.Y - StlModelReferencePoint.Y;
        }

        public void UpdateModelViewPort(HelixViewport3D viewport3D)
        {
            _viewport3D = viewport3D;
        }

        private async Task<bool> FetchSTL(string filePathIn)
        {
            if (!File.Exists(filePathIn))
            {
                _stlSlices = new List<MSTLSlice>();
            }

            var stlModel = ImportModel3D(filePathIn);

            if (stlModel != null)
            {
                _stlModel = stlModel;
                PatternFilePath = filePathIn;
                ThreeDeeModel = _modelGroup;
                SliceThickness = 0.1 * _stlModel.Bounds.SizeZ;

                NumberOfSlices = (int)Math.Ceiling(_stlModel.Bounds.SizeZ / SliceThickness);

                _slicePlanes.Clear();

                // calculate plane dimensions with padding
                double planeSizeX = 1.2 * _stlModel.Bounds.SizeX;
                double planeSizeY = 1.2 * _stlModel.Bounds.SizeY;
                double planeSizeZ = 0.1 * SliceThickness;

                double xPosition = _stlModel.Bounds.X + (0.5 * _stlModel.Bounds.SizeX);
                double yPosition = _stlModel.Bounds.Y + (0.5 * _stlModel.Bounds.SizeY);

                StlModelReferencePoint.X = xPosition;
                StlModelReferencePoint.Y = yPosition;

                GenerateFiducialPattern(0.05 * Math.Max(_stlModel.Bounds.SizeX, _stlModel.Bounds.SizeY));

                GridLinesOrigin = new Point3D(xPosition, yPosition, _stlModel.Bounds.Z);
                GridLinesSize = Math.Max(2 * planeSizeX, 2 * planeSizeY);
                GridLinesGridPitch = Math.Pow(10, Math.Log10(GridLinesSize) - 1);

                var _mSlicer = new MSTLSlicer();
                if (!_mSlicer.Load(filePathIn))
                    throw new Exception("Failed to load STL");

                var backgroundTasks = new List<Task<bool>>()
                {
                    Task.Run(
                        () =>
                        {
                            foreach(var contours in _mSlicer.SliceParallel(SliceThickness, StitchTolerance))
                                _stlSlices.Add(new MSTLSlice(contours));
                            return true;
                        }
                    )
                };

                for (int i = 0; i < NumberOfSlices; i++)
                {
                    var modelBuilder = new MeshBuilder(false, false);
                    modelBuilder.AddBox(
                        new Point3D(
                            xPosition,
                            yPosition,
                            (i * SliceThickness) + _stlModel.Bounds.Z
                        ),
                        planeSizeX, planeSizeY, planeSizeZ
                    );

                    _slicePlanes.Add(
                        new GeometryModel3D
                        {
                            Geometry = modelBuilder.ToMesh(true),
                            Material = PlaneMaterial
                        }
                    );
                }

                await Task.WhenAll(backgroundTasks);

                // reverse slices - orientate from top to bottom
                // by defaults slices are from the bottom to the top
                // but we need this flipped for this process
                _slicePlanes.Reverse();
                _stlSlices.Reverse();

                // hatch and tile slices
                await HatchAllSlices();
                await TileAllSlices();

                // update slice size info
                SliceXSize = _stlSlices[CurrentSlice].Extents.Width;
                SliceYSize = _stlSlices[CurrentSlice].Extents.Height;

                if (_stlSlices[CurrentSlice] != null)
                    SliceCount = _stlSlices[CurrentSlice].ContourLines.Count + _stlSlices[CurrentSlice].HatchLines.Count;

                if (NumberOfSlices > 0)
                    ShowSlice(0);

                return true;
            }

            return false;
        }

        public async Task<bool> HatchAllSlices()
        {
            var taskList = new List<Task<bool>>();

            foreach (var slice in _stlSlices)
                taskList.Add(
                    Task.Run(
                        () => slice.GenerateHatches(HatchSettings)
                    )
                );

            await Task.WhenAll(taskList);
            return true;
        }

        private void ShowSlice(int sliceIndex)
        {
            try
            {
                _modelGroup.Children.Clear();
                _modelGroup.Children.Add(_stlModel);
                _modelGroup.Children.Add(_slicePlanes[sliceIndex]);

                _viewport3D.ZoomExtents();

                CurrentSlice = sliceIndex;
            }
            catch (Exception)
            {
            }

            // render slice
            Render();
        }

        public async Task<bool> HatchSlice(int sliceIndex)
        {
            return await Task.Run(() =>
                _stlSlices[sliceIndex].GenerateHatches(HatchSettings)
            );
        }

        public async Task<bool> TileAllSlices()
        {
            var taskList = new List<Task<bool>>();

            foreach (var slice in _stlSlices)
                taskList.Add(
                    Task.Run(
                        () => slice.GenerateTiles(TileSettings)
                    )
                );

            await Task.WhenAll(taskList);
            return true;
        }



        public async Task<bool> TileSlice(int sliceIndex)
        {
            return await Task.Run(
                () => _stlSlices[sliceIndex].GenerateTiles(TileSettings)
            );
        }

        private Model3D ImportModel3D(string stlFilePathIn)
        {
            Model3D model3d = null;
            try
            {
                var modelImporter = new ModelImporter();
                modelImporter.DefaultMaterial = ModelMaterial;
                model3d = modelImporter.Load(stlFilePathIn);
            }
            catch (Exception)
            {
                //_model.Log(exp);
                //_model.Log("Failed to parse STL file");

                DispatcherMessageBox.ShowBox(
                    "Failed to parse STL file."
                );
            }

            return model3d;
        }

        private void GenerateFiducialPattern(double radius = 2.5, int numOfLines = 4)
        {
            _fiducialPattern = new List<IMarkGeometry>();

            var baseLine = new MarkGeometryLine(new MarkGeometryPoint(-radius, 0), new MarkGeometryPoint(radius, 0));
            var transform = GeometricArithmeticModule.GetRotationTransformationMatrix(
                0, 0, Math.PI / numOfLines
            );

            for (int i = 0; i < numOfLines; i++)
            {
                _fiducialPattern.Add((IMarkGeometry)baseLine.Clone());
                baseLine.Transform(transform);
            }

            _fiducialPattern.Add(new MarkGeometryCircle(radius));
        }
    }
}
