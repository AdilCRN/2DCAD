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

        public override object Clone()
        {
            return new MRecipeDevice(this);
        }
    }
}
