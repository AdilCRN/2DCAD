using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Documents;

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

        /// <summary>
        /// Use to get all devices.
        /// </summary>
        /// <param name="callback"></param>
        public void BeginGetAllDevices(Action<MRecipeDevice> callback)
        {
            MArrayInfo.BeginGetAll(ArrayInfo, (index) =>
            {
                foreach (var d in Devices)
                {
                    var device = (MRecipeDevice)d.Clone();

                    device.TransformInfo.OffsetX += index.XInfo.Offset;
                    device.TransformInfo.OffsetY += index.YInfo.Offset;
                    device.TransformInfo.OffsetZ += index.ZInfo.Offset;

                    callback(device);
                }
            });
        }

        /// <summary>
        /// Use to get devices when order does not matter.
        /// </summary>
        /// <param name="callback"></param>
        public void BeginGetAllDevices_Parallel(Action<MRecipeDevice> callback)
        {
            MArrayInfo.BeginGetAll_Parallel(ArrayInfo, (index) =>
            {
                Parallel.ForEach(Devices, (d) =>
                {
                    var device = (MRecipeDevice)d.Clone();

                    device.TransformInfo.OffsetX += index.XInfo.Offset;
                    device.TransformInfo.OffsetY += index.YInfo.Offset;
                    device.TransformInfo.OffsetZ += index.ZInfo.Offset;

                    callback(device);
                });
            });
        }

        /// <summary>
        /// Use to get all devices.
        /// </summary>
        /// <returns></returns>
        public List<MRecipeDevice> Flatten()
        {
            var devices = new List<MRecipeDevice>();

            BeginGetAllDevices((device) =>
            {
                devices.Add(device);
            });

            return devices;
        }

        public override object Clone()
        {
            return new MRecipePlate(this);
        }
    }
}
