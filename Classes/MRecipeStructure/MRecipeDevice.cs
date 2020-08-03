using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRecipeStructure.Classes.MRecipeStructure
{
    [Serializable]
    public class MRecipeDevice : MRecipeBaseNode
    {
        private ObservableCollection<MRecipeDeviceLayer> _layers;

        public ObservableCollection<MRecipeDeviceLayer> Layers
        {
            get { return _layers; }
            set 
            { 
                _layers = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        ///     The copy constructor.
        /// </summary>
        /// <param name="device"></param>
        private MRecipeDevice(MRecipeDevice device)
            : base(device)
        {
            Layers = new ObservableCollection<MRecipeDeviceLayer>();

            foreach (var layer in device.Layers)
            {
                var l = (MRecipeDeviceLayer)layer.Clone();
                l.Parent = this;
                Layers.Add(l);
            }
        }

        public MRecipeDevice()
            : base()
        {
            Tag = "Device";
            Layers = new ObservableCollection<MRecipeDeviceLayer>();
        }

        public MRecipeDevice(string nameIn)
            : this()
        {
            Tag = nameIn;
        }

        public MRecipeDevice(string nameIn, MRecipeDeviceLayer layerIn)
            : this(nameIn)
        {
            Layers.Add(layerIn);
        }

        public void AddLayer(MRecipeDeviceLayer layer)
        {
            layer.Parent = this;
            Layers.Add(layer);
        }

        /// <summary>
        /// Use to get all layers.
        /// </summary>
        /// <param name="callback"></param>
        public void BeginGetAllLayers(Action<MRecipeDeviceLayer> callback)
        {
            MArrayInfo.BeginGetAll(ArrayInfo, (index) =>
            {
                foreach (var l in Layers)
                {
                    var layer = (MRecipeDeviceLayer)l.Clone();

                    layer.TransformInfo.OffsetX += index.XInfo.Offset;
                    layer.TransformInfo.OffsetY += index.YInfo.Offset;
                    layer.TransformInfo.OffsetZ += index.ZInfo.Offset;

                    callback(layer);
                }
            });
        }

        /// <summary>
        /// Use to get layers when order does not matter.
        /// </summary>
        /// <param name="callback"></param>
        public void BeginGetAllLayers_Parallel(Action<MRecipeDeviceLayer> callback)
        {
            MArrayInfo.BeginGetAll_Parallel(ArrayInfo, (index) =>
            {
                Parallel.ForEach(Layers, (l) =>
                {
                    var layer = (MRecipeDeviceLayer)l.Clone();

                    layer.TransformInfo.OffsetX += index.XInfo.Offset;
                    layer.TransformInfo.OffsetY += index.YInfo.Offset;
                    layer.TransformInfo.OffsetZ += index.ZInfo.Offset;

                    callback(layer);
                });
            });
        }

        /// <summary>
        /// Use to get all devices.
        /// </summary>
        /// <returns></returns>
        public List<MRecipeDeviceLayer> Flatten()
        {
            var layers = new List<MRecipeDeviceLayer>();

            BeginGetAllLayers((layer) =>
            {
                layers.Add(layer);
            });

            return layers;
        }

        public override object Clone()
        {
            return new MRecipeDevice(this);
        }
    }
}
