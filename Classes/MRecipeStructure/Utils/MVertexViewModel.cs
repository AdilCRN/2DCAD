using MSolvLib;
using System;

namespace MRecipeStructure.Classes.MRecipeStructure.Utils
{
	public class MVertexViewModel : ViewModel
	{
		private double _x;

		public double X
		{
			get { return _x; }
			set
			{
				_x = Math.Round(value, 4);
				NotifyPropertyChanged();
			}
		}

		private double _y;

		public double Y
		{
			get { return _y; }
			set
			{
				_y = Math.Round(value, 4);
				NotifyPropertyChanged();
			}
		}

	}
}
