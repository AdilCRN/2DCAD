using MarkGeometriesLib.Classes.Generics;
using Microsoft.Win32;
using MRecipeStructure.Classes.MRecipeStructure;
using MSolvLib;
using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using MSolvLib.Classes.ProcessConfiguration;
using MSolvLib.DialogForms;
using MSolvLib.MarkGeometry;
using Prism.Commands;
using SharpGLShader;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace MRecipeStructure.DialogsDialogs.DialogViewModel
{
    public class ImportPatternDialogViewModel : ViewModel
    {
        #region Section: Private Properties

        // for managing duplicate tasks
        private TerminableTaskExecutor _terminableTaskExecutor;

        // buffer for geometries
        private List<IMarkGeometry> _fiducialPattern;
        private Dictionary<string, List<IMarkGeometry>> _geometriesBuffer;
        private double[][] _colours = new double[][] { MGLShader.White, MGLShader.Cyan, MGLShader.Blue, MGLShader.Violet, MGLShader.Red, MGLShader.Yellow, MGLShader.Green };
        private double[] _tileColor = new double[] { 1.0, 0.7, 0.7, 0.5 };
        public double[] _fiducialColor = MGLShader.Cyan;

        #endregion

        #region Section: Public Properties

        public MGLShader MShader;
        public string DefaultRecipeDirectory;
        public string DefaultPatternDirectory;

        #endregion

        #region Section: Data Binding

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


        private bool _seperateLayers;

        public bool SeperateLayers
        {
            get { return _seperateLayers; }
            set
            {
                _seperateLayers = value;
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


        #endregion

        #region Section: Delegate Command

        public DelegateCommand SelectPatternFileCommand { get; set; }
        public DelegateCommand SelectParametersFileCommand { get; set; }
        public DelegateCommand AddFiducialCommand { get; set; }
        public DelegateCommand<MFiducialInfo> DeleteFiducialCommand { get; set; }
        public DelegateCommand DeleteAllFiducialsCommand { get; set; }
        public DelegateCommand RefreshCommand { get; set; }

        #endregion

        public ImportPatternDialogViewModel(IEnumerable<IProcessConfiguration> availableProcessConfigurations, string defaultPatternDirectory, string defaultRecipeDirectory)
        {
            MShader = new MGLShader();
            _terminableTaskExecutor = new TerminableTaskExecutor();

            DefaultRecipeDirectory = defaultRecipeDirectory;
            DefaultPatternDirectory = defaultPatternDirectory;

            SeperateLayers = true;
            AlignmentType = MAlignmentType.TypeAuto;
            TileSettings = new MTileSettings();
            Fiducials = new ObservableCollection<MFiducialInfo>();
            AvailableProcessModes = new ObservableCollection<IProcessConfiguration>(availableProcessConfigurations);

            SelectPatternFileCommand = new DelegateCommand(
                () =>
                {
                    var dialog = new OpenFileDialog();
                    dialog.Filter = "DXF files (*.dxf)|*.dxf";
                    dialog.AddExtension = true;
                    dialog.InitialDirectory = defaultPatternDirectory;

                    if (dialog.ShowDialog() != true)
                        return;

                    PatternFilePath = dialog.FileName;
                    FetchDXF(PatternFilePath);
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
        }

        public void Render()
        {
            _terminableTaskExecutor.CancelCurrentAndRun(
                (ctIn) =>
                {
                    // reset shader
                    MShader.Reset();

                    if (_geometriesBuffer != null)
                    {
                        int count = 0;
                        var extents = GeometryExtents<double>.CreateDefaultDouble();
                        foreach (var layerName in _geometriesBuffer.Keys)
                        {
                            MShader.AddDefault(
                                _geometriesBuffer[layerName],
                                _colours[(count++) % _colours.Length]
                            );

                            extents = GeometryExtents<double>.Combine(
                                extents, GeometricArithmeticModule.CalculateExtents(_geometriesBuffer[layerName])
                            );
                        }

                        // overlay tiles
                        foreach (var tile in MTileSettings.ToTiles(TileSettings, extents))
                            MShader.AddDefault((MarkGeometryRectangle)tile, _tileColor);

                        // calculate size of fiducial relative to the node
                        var fiducialSize = 0.05 * extents.Hypotenuse;

                        // generate fiducial pattern
                        GenerateFiducialPattern(fiducialSize);

                        // get base transform
                        var baseTransform = GeometricArithmeticModule.GetTranslationTransformationMatrix(
                            extents.Centre.X, extents.Centre.Y
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

                    DrawingExtentsX = MShader.Width;
                    DrawingExtentsY = MShader.Height;
                    DrawingCount = MShader.Count;
                }
            );
        }

        public MRecipe GenerateRecipe(string outputFileName)
        {
            if (
                _geometriesBuffer == null || 
                _geometriesBuffer.Keys.Count <= 0 ||
                !File.Exists(PatternFilePath)
            )
                throw new Exception("Missing or Invalid pattern");

            var patternName = Path.GetFileNameWithoutExtension(outputFileName);
            var device = new MRecipeDevice(patternName);
            var recipe = new MRecipe(new MRecipePlate("Plate", device));

            // add fiducials to recipe plate
            recipe.Plates.First().Fiducials = new ObservableCollection<MFiducialInfo>(Fiducials);

            if (SeperateLayers)
            {
                var dataDirectory = Path.Combine(
                    Path.GetDirectoryName(outputFileName),
                    $"{patternName} Data"
                );

                if (!Directory.Exists(dataDirectory))
                    Directory.CreateDirectory(dataDirectory);

                foreach (var layerName in _geometriesBuffer?.Keys)
                {
                    var fileName = Path.Combine(
                        dataDirectory, $"{layerName}.dxf"
                    );
                    GeometricArithmeticModule.SaveDXF(fileName, _geometriesBuffer[layerName]);

                    var layer = new MRecipeDeviceLayer(layerName);
                    layer.AlignmentType = AlignmentType;
                    layer.PatternFilePath = fileName;
                    layer.ProcessParametersFilePath = ProcessParametersFilePath;
                    layer.TargetProcessMode = TargetProcessMode.Name.EnglishValue;
                    layer.TileSettings = (MTileSettings)TileSettings.Clone();
                    device.AddLayer(layer);
                }

                recipe.Save(
                    Path.Combine(
                        dataDirectory, $"{patternName}.{MRecipe.DefaultFileExtension}"
                    )
                );
            }
            else
            {
                var layer = new MRecipeDeviceLayer(patternName);
                layer.AlignmentType = AlignmentType;
                layer.PatternFilePath = PatternFilePath;
                layer.ProcessParametersFilePath = ProcessParametersFilePath;
                layer.TargetProcessMode = TargetProcessMode.Name.EnglishValue;
                layer.TileSettings = (MTileSettings)TileSettings.Clone();
                device.AddLayer(layer);

                recipe.Save(
                    Path.Combine(
                        Path.GetDirectoryName(PatternFilePath), $"{patternName}.{MRecipe.DefaultFileExtension}"
                    )
                );
            }

            // update recipe's parents
            recipe.UpdateParents();

            return recipe;
        }

        public void UpdateMousePosition()
        {
            // update mouse to reflect the reference point
            MouseX = MShader.Mouse.X;
            MouseY = MShader.Mouse.Y;
        }

        private void FetchDXF(string filePathIn)
        {
            if (!File.Exists(filePathIn))
            {
                _geometriesBuffer = null;
            }

            // attempt to load from cache if it exists else load using getter
            _geometriesBuffer = GeometricArithmeticModule.ExtractLabelledGeometriesFromDXF(filePathIn);
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
