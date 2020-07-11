using MSolvLib;
using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using MSolvLib.MarkGeometry;
using System;
using System.Collections.Generic;

namespace MRecipeStructure.Classes.MRecipeStructure
{
	[Serializable]
	public class MTileSettings : ViewModel, ICloneable
	{
		private double _xSize = 50;

		public double XSize
		{
			get { return _xSize; }
			set
			{
				_xSize = Math.Round(value, 4);
				NotifyPropertyChanged();
			}
		}

		private double _ySize = 50;

		public double YSize
		{
			get { return _ySize; }
			set
			{
				_ySize = Math.Round(value, 4);
				NotifyPropertyChanged();
			}
		}

		private double _xOffset = 0;

		public double XOffset
		{
			get { return _xOffset; }
			set
			{
				_xOffset = Math.Round(value, 4);
				NotifyPropertyChanged();
			}
		}

		private double _yOffset = 0;

		public double YOffset
		{
			get { return _yOffset; }
			set
			{
				_yOffset = Math.Round(value, 4);
				NotifyPropertyChanged();
			}
		}

		private double _xPadding = 3;

		public double XPadding
		{
			get { return _xPadding; }
			set
			{
				_xPadding = Math.Round(value, 4);
				NotifyPropertyChanged();
			}
		}

		private double _yPadding = 3;

		public double YPadding
		{
			get { return _yPadding; }
			set
			{
				_yPadding = Math.Round(value, 4);
				NotifyPropertyChanged();
			}
		}


		private double _extension = 0;

		public double Extension
		{
			get { return _extension; }
			set
			{
				_extension = Math.Round(value, 3);
				NotifyPropertyChanged();
			}
		}

		private TileStyle _style = TileStyle.SEPERTINE;

		public TileStyle Style
		{
			get { return _style; }
			set
			{
				_style = value;
				NotifyPropertyChanged();
			}
		}

		/// <summary>
		///		The copy constructor.
		/// </summary>
		private MTileSettings(MTileSettings tileSettingsIn)
			: this()
		{
			XSize = tileSettingsIn.XSize;
			YSize = tileSettingsIn.YSize;
			XOffset = tileSettingsIn.XOffset;
			YOffset = tileSettingsIn.YOffset;
			XPadding = tileSettingsIn.XPadding;
			YPadding = tileSettingsIn.YPadding;
			Extension = tileSettingsIn.Extension;
			Style = tileSettingsIn.Style;
		}

		public MTileSettings()
		{
		}

		public object Clone()
		{
			return new MTileSettings(this);
		}

		public static List<MTileDescription> ToTiles(MTileSettings tileSettings, GeometryExtents<double> extents)
		{
			var tiles = new List<MTileDescription>();

			double refWidth = extents.Width + tileSettings.XPadding;
			double refHeight = extents.Height + tileSettings.YPadding;

			int nRows = (int)Math.Ceiling(refHeight / tileSettings.YSize);
			int nColumns = (int)Math.Ceiling(refWidth / tileSettings.XSize);

			var _halfTileWidth = 0.5 * tileSettings.XSize;
			var _halfTileHeight = 0.5 * tileSettings.YSize;
			var _centre = extents.Centre - new MarkGeometryPoint(0.5 * (nColumns * tileSettings.XSize), 0.5 * (nRows * tileSettings.YSize));

			int counter = 0;
			for (int row = 0; row < nRows; row++)
			{
				for (int col = 0; col < nColumns; col++)
				{
					var centrePoint = new MarkGeometryPoint(
						(col * tileSettings.XSize) + _halfTileWidth,
						(row * tileSettings.YSize) + _halfTileHeight
					);

					GeometricArithmeticModule.Translate(centrePoint, _centre.X + tileSettings.XOffset, _centre.Y + tileSettings.YOffset);

					tiles.Add(
						new MTileDescription(
							counter++, centrePoint.X, centrePoint.Y, tileSettings.XSize, tileSettings.YSize
						)
					);
				}
			}

			return tiles;
		}
	}
}
