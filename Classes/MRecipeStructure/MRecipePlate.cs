using System;
using System.Collections.ObjectModel;

namespace MRecipeStructure.Classes.MRecipeStructure
{
    [Serializable]
    public class MRecipePlate : MRecipeBaseNode
    {
        private ObservableCollection<MRecipeDevice> _devices;

        public ObservableCollection<MRecipeDevice> Devices
        {
            get { return _devices; }
            set
            {
                _devices = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        ///     The copy constructor.
        /// </summary>
        /// <param name="plate"></param>
        private MRecipePlate(MRecipePlate plate)
            : base(plate)
        {
            Devices = new ObservableCollection<MRecipeDevice>();

            foreach (var device in plate.Devices)
            {
                var _device = (MRecipeDevice)device.Clone();
                _device.Parent = this;
                Devices.Add(_device);
            }
        }

        public MRecipePlate()
            : base()
        {
            Tag = "Plate";
            Devices = new ObservableCollection<MRecipeDevice>();
        }

        public MRecipePlate(string nameIn)
            : this()
        {
            Tag = nameIn;
        }

        public MRecipePlate(string nameIn, MRecipeDevice device)
            : this(nameIn)
        {
            Devices.Add(device);
        }

        public void AddDeviceArray(MRecipeDevice device)
        {
            device.Parent = this;
            Devices.Add(device);
        }

        public override object Clone()
        {
            return new MRecipePlate(this);
        }
    }
}
