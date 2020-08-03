using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace MRecipeStructure.Classes.MRecipeStructure
{
    [Serializable]
    public class MRecipeDeviceLayer : MRecipeBaseNode
    {
        [Category("Layer")]
        [DisplayName("Process Pattern")]
        public string PatternFilePath
        {
            get { return _patternFilePath; }
            set 
            { 
                _patternFilePath = value;
                NotifyPropertyChanged();
            }
        }
        private string _patternFilePath;

        [Category("Layer")]
        [DisplayName("Process Parameters")]
        public string ProcessParametersFilePath
        {
            get { return _processParametersFilePath; }
            set 
            { 
                _processParametersFilePath = value;
                NotifyPropertyChanged();
            }
        }
        private string _processParametersFilePath;

        [Category("Layer")]
        [DisplayName("Target Process Mode")]
        [Description("Select the target process mode (see the process mode selection utility).")]
        public string TargetProcessMode
        {
            get { return _targetProcessMode; }
            set 
            { 
                _targetProcessMode = value;
                NotifyPropertyChanged();
            }
        }

        private string _targetProcessMode;

        private ObservableCollection<MTileDescription> _tiles;

        public ObservableCollection<MTileDescription> TileDescriptions
        {
            get { return _tiles; }
            protected set 
            { 
                _tiles = value;
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


        /// <summary>
        ///     The copy constructor.
        /// </summary>
        /// <param name="layer"></param>
        private MRecipeDeviceLayer(MRecipeDeviceLayer layer)
            : base(layer)
        {
            PatternFilePath = string.IsNullOrWhiteSpace(layer.PatternFilePath) ? "" : (string)layer.PatternFilePath.Clone();
            ProcessParametersFilePath = string.IsNullOrWhiteSpace(layer.ProcessParametersFilePath) ? "" : (string)layer.ProcessParametersFilePath.Clone();
            TargetProcessMode = string.IsNullOrWhiteSpace(layer.TargetProcessMode) ? "" : (string)layer.TargetProcessMode.Clone();
            TileSettings = (MTileSettings)layer.TileSettings.Clone();

            // clone tiles info
            TileDescriptions = new ObservableCollection<MTileDescription>();
            foreach (var tile in layer.TileDescriptions)
                TileDescriptions.Add((MTileDescription)tile.Clone());
        }

        public MRecipeDeviceLayer()
            : base()
        {
            Tag = "Layer";
            TileSettings = new MTileSettings();
            TileDescriptions = new ObservableCollection<MTileDescription>();
        }

        public MRecipeDeviceLayer(string nameIn)
            : this()
        {
            Tag = nameIn;
        }

        /// <summary>
        /// Use to get all tiles.
        /// </summary>
        /// <param name="callback"></param>
        public void BeginGetAllTiles(Action<MTileDescription> callback)
        {
            MArrayInfo.BeginGetAll(ArrayInfo, (index) =>
            {
                foreach (var td in TileDescriptions)
                {
                    var tile = (MTileDescription)td.Clone();
                    tile.CentreX += index.XInfo.Offset;
                    tile.CentreY += index.YInfo.Offset;
                    callback(tile);
                }
            });
        }

        /// <summary>
        /// Use to get all tiles, when order does not matter.
        /// </summary>
        /// <param name="callback"></param>
        public void BeginGetAllTiles_Parallel(Action<MTileDescription> callback)
        {
            MArrayInfo.BeginGetAll(ArrayInfo, (index) =>
            {
                Parallel.ForEach(TileDescriptions, (td) => 
                {
                    var tile = (MTileDescription)td.Clone();
                    tile.CentreX += index.XInfo.Offset;
                    tile.CentreY += index.YInfo.Offset;
                    callback(tile);
                });
            });
        }

        /// <summary>
        /// Use to get all tiles.
        /// </summary>
        /// <returns></returns>
        public List<MTileDescription> Flatten()
        {
            var tiles = new List<MTileDescription>();

            BeginGetAllTiles((tile) =>
            {
                tiles.Add(tile);
            });

            return tiles;
        }

        public void GenerateTileDescriptionsFromSettings(GeometryExtents<double> extentsIn)
        {
            TileDescriptions.Clear();

            if (!File.Exists(PatternFilePath))
                return;

            // generate tile
            TileDescriptions = new ObservableCollection<MTileDescription>(
                MTileSettings.ToTiles(TileSettings, extentsIn)
            );
        }

        public void GenerateTileDescriptionsFromSettings(Func<string, GeometryExtents<double>> measurePatternFunc)
        {
            if (!File.Exists(PatternFilePath))
                return;

            // measure pattern's extents
            GenerateTileDescriptionsFromSettings(measurePatternFunc.Invoke(PatternFilePath));
        }

        public override object Clone()
        {
            return new MRecipeDeviceLayer(this);
        }

        public override string ToString()
        {
            return $"{Parent}>{Tag}";
        }
    }
}
