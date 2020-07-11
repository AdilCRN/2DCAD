using MSolvLib;
using System;

namespace MRecipeStructure.Classes.MRecipeStructure
{
	[Serializable]
    public class MFiducialInfo : ViewModel, ICloneable
    {

		private int _index;

		public int Index
		{
			get { return _index; }
			set 
			{ 
				_index = value;
				NotifyPropertyChanged();
			}
		}


		private double _x;

		public double X
		{
			get { return _x; }
			set 
			{ 
				_x = Math.Round(value, 5);
				NotifyPropertyChanged();
			}
		}

		private double _y;

		public double Y
		{
			get { return _y; }
			set 
			{
				_y = Math.Round(value, 5);
				NotifyPropertyChanged();
			}
		}

		private double _z;

		public double Z
		{
			get { return _z; }
			set 
			{ 
				_z = Math.Round(value, 5);
				NotifyPropertyChanged();
			}
		}

		public MFiducialInfo()
		{
			Index = 0;
			X = 0d; Y = 0d; Z = 0d;
		}

		/// <summary>
		///		The copy constructor
		/// </summary>
		/// <param name="info"></param>
		private MFiducialInfo(MFiducialInfo info)
		{
			Index = info.Index;
			X = info.X; Y = info.Y; Z = info.Z;
		}

		public object Clone()
		{
			return new MFiducialInfo(this);
		}
	}
}
