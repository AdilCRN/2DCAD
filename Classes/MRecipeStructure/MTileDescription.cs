using MSolvLib;
using MSolvLib.MarkGeometry;
using System;
using System.ComponentModel;

namespace MRecipeStructure.Classes.MRecipeStructure
{
    [Serializable]
    public class MTileDescription : ViewModel, ICloneable
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

		private double _sizeX;

		[DisplayName("Size X (mm)")]
		public double SizeX
		{
			get { return _sizeX; }
			set 
			{ 
				_sizeX = Math.Round(Math.Max(value, 0.5), 5);
				NotifyPropertyChanged();
			}
		}


		private double _sizeY;

		[DisplayName("Size Y (mm)")]
		public double SizeY
		{
			get { return _sizeY; }
			set 
			{ 
				_sizeY = Math.Round(Math.Max(value, 0.5), 5);
				NotifyPropertyChanged();
			}
		}

		private double _centreX;

		[DisplayName("Centre X (mm)")]
		public double CentreX
		{
			get { return _centreX; }
			set 
			{ 
				_centreX = Math.Round(value, 5);
				NotifyPropertyChanged();
			}
		}

		private double _centreY;

		[DisplayName("Centre Y (mm)")]
		public double CentreY
		{
			get { return _centreY; }
			set
			{
				_centreY = Math.Round(value, 5);
				NotifyPropertyChanged();

			}
		}

		/// <summary>
		///		The copy constructor.
		/// </summary>
		private MTileDescription(MTileDescription tile)
			: this(tile.Index, tile.CentreX, tile.CentreY, tile.SizeX, tile.SizeY)
		{
		}

		public MTileDescription()
			: base()
		{
			Index = 0;
			SizeX = 5; SizeY = 5;
			CentreX = 0; CentreY = 0;
		}

		public MTileDescription(int index)
			: this()
		{
			Index = index;
		}

		public MTileDescription(int index, MarkGeometryRectangle rectangleIn)
			: this(index)
		{
			rectangleIn.Update();

			SizeX = rectangleIn.Width;
			SizeY = rectangleIn.Height;
			CentreX = rectangleIn.Extents.Centre.X;
			CentreY = rectangleIn.Extents.Centre.Y;
		}

		public MTileDescription(int index, double centreX, double centreY, double width, double height)
			: this(index)
		{
			SizeX = width;
			SizeY = height;
			CentreX = centreX;
			CentreY = centreY;
		}

		public static explicit operator MarkGeometryRectangle(MTileDescription tileIn)
		{
			return new MarkGeometryRectangle(
				new MarkGeometryPoint(
					tileIn.CentreX, 
					tileIn.CentreY
				),
				tileIn.SizeX,
				tileIn.SizeY
			);
		}

		public object Clone()
		{
			return new MTileDescription(this);
		}
	}
}
