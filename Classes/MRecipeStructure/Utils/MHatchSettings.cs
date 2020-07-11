using MSolvLib;
using System;

namespace MRecipeStructure.Classes.MRecipeStructure.Utils
{
    public class MHatchSettings : ViewModel
	{
		private double _pitch = 1;

		public double Pitch
		{
			get { return _pitch; }
			set
			{
				_pitch = Math.Round(Math.Max(value, 0.001), 4);
				NotifyPropertyChanged();
			}
		}

		private double _angle = 45;

		public double Angle
		{
			get { return _angle; }
			set
			{
				_angle = Math.Round(Math.Max(Math.Min(value, 360), 0), 4);
				NotifyPropertyChanged();
			}
		}

		private double _extension = 0;

		public double Extension
		{
			get { return _extension; }
			set
			{
				_extension = Math.Round(value, 4);
				NotifyPropertyChanged();
			}
		}

		private HatchStyle _style = HatchStyle.SERPENTINE_GRID;

		public HatchStyle Style
		{
			get { return _style; }
			set
			{
				_style = value;
				NotifyPropertyChanged();
			}
		}

		private bool _invert = false;

		public bool Invert
		{
			get { return _invert; }
			set
			{
				_invert = value;
				NotifyPropertyChanged();
			}
		}


	}
}
