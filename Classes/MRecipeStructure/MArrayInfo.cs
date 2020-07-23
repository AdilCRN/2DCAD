using MSolvLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRecipeStructure.Classes.MRecipeStructure
{

    [Serializable]
    public class MArrayInfo : ViewModel, ICloneable
    {
		private int _numX;

		public int NumX
		{
			get { return _numX; }
			set 
			{ 
				_numX = value;
				NotifyPropertyChanged();
			}
		}

		private int _numY;

		public int NumY
		{
			get { return _numY; }
			set 
			{
				_numY = value;
				NotifyPropertyChanged();
			}
		}

		private int _numZ;

		public int NumZ
		{
			get { return _numZ; }
			set 
			{
				_numZ = value;
				NotifyPropertyChanged();
			}
		}

		private double _pitchX;

		public double PitchX
		{
			get { return _pitchX; }
			set 
			{ 
				_pitchX = value;
				NotifyPropertyChanged();
			}
		}

		private double _pitchY;

		public double PitchY
		{
			get { return _pitchY; }
			set
			{
				_pitchY = value;
				NotifyPropertyChanged();
			}
		}

		private double _pitchZ;

		public double PitchZ
		{
			get { return _pitchZ; }
			set
			{
				_pitchZ = value;
				NotifyPropertyChanged();
			}
		}

        private MArrayStyle _arrayStyle = MArrayStyle.SPIRAL_OUT_CW;

        public MArrayStyle ArrayStyle
		{
            get { return _arrayStyle; }
            set
			{ 
				_arrayStyle = value;
				NotifyPropertyChanged();
			}
        }

        private MArrayStartLocation _startLocation = MArrayStartLocation.CENTER;

        public MArrayStartLocation StartLocation
        {
            get { return _startLocation; }
            set 
			{ 
				_startLocation = value;
				NotifyPropertyChanged();
			}
        }

        public MArrayInfo()
		{
			NumX = 1; NumY = 1; NumZ = 1;
			PitchX = 0; PitchY = 0; PitchZ = 0;
			ArrayStyle = MArrayStyle.SPIRAL_OUT_CW;
		}

		/// <summary>
		///		The copy constructor
		/// </summary>
		/// <param name="info"></param>
		private MArrayInfo(MArrayInfo info)
		{
			NumX = info.NumX;
			NumY = info.NumY;
			NumZ = info.NumZ;

			PitchX = info.PitchX;
			PitchY = info.PitchY;
			PitchZ = info.PitchZ;

			ArrayStyle = info.ArrayStyle;
		}

		/// <summary>
		///		Auto generates position info using the given array info.
		/// </summary>
		/// <param name="info">The array info.</param>
		/// <param name="callback">A callback to receive the calculated values.</param>
		public static void BeginGetAll(MArrayInfo info, Action<(MArrayPositionInfo XInfo, MArrayPositionInfo YInfo, MArrayPositionInfo ZInfo)> callback)
        {
			// TODO : Add support for 3D array indexing styles
			GenerateArray2D(info, (data) =>
			{
				for (int z = 0; z < info.NumZ; z++)
				{
					callback((
						data.XInfo,
						data.YInfo,
						new MArrayPositionInfo(z, z * info.PitchZ)
					));
				}
			});
        }

		/// <summary>
		///		Auto generates position info using the given array info.
		///		Sacrifice order/arrangement for speed.
		/// </summary>
		/// <param name="info">The array info.</param>
		/// <param name="callback">A callback to receive the calculated values.</param>
		public static void BeginGetAll_Parallel(MArrayInfo info, Action<(MArrayPositionInfo XInfo, MArrayPositionInfo YInfo, MArrayPositionInfo ZInfo)> callback)
		{
			Parallel.For(0, info.NumX, (x) =>
			{
				Parallel.For(0, info.NumY, (y) =>
				{
					Parallel.For(0, info.NumZ, (z) =>
					{
						callback((
							new MArrayPositionInfo(x, x * info.PitchX),
							new MArrayPositionInfo(y, y * info.PitchY),
							new MArrayPositionInfo(z, z * info.PitchZ)
						));
					});
				});
			});
		}

		public object Clone()
		{
			return new MArrayInfo(this);
		}

		#region Section: Static Generators

		public static void GenerateArray2D(MArrayInfo info, Action<(MArrayPositionInfo XInfo, MArrayPositionInfo YInfo)> callback)
        {
			int xOffset = 0;
			int yOffset = 0;

			#region TODO: Implement Offsets
			//switch (info.StartLocation)
			//{
			//	case MArrayStartLocation.TOP_LEFT:
			//	case MArrayStartLocation.BOTTOM_LEFT:
			//		xOffset = -info.NumX / 2;
			//		break;
			//	case MArrayStartLocation.CENTER:
			//		xOffset = 0;
			//		break;
			//	case MArrayStartLocation.TOP_RIGHT:
			//	case MArrayStartLocation.BOTTOM_RIGHT:
			//		xOffset = info.NumX / 2;
			//		break;
			//}

			//switch (info.StartLocation)
			//{
			//	case MArrayStartLocation.BOTTOM_LEFT:
			//	case MArrayStartLocation.BOTTOM_RIGHT:
			//		yOffset = -info.NumY / 2;
			//		break;
			//	case MArrayStartLocation.CENTER:
			//		yOffset = 0;
			//		break;
			//	case MArrayStartLocation.TOP_LEFT:
			//	case MArrayStartLocation.TOP_RIGHT:
			//		yOffset = info.NumY / 2;
			//		break;
			//} 
			#endregion

			switch (info.ArrayStyle)
            {
                case MArrayStyle.SPIRAL_IN_CW:
					GenerateArray2D_SpiralIn_CW(
						info.NumX,
						info.NumY,
						info.StartLocation,
						0,
						xOffset,
						yOffset,
						(index) => {
							var _x = index.X + xOffset;
							var _y = index.Y + yOffset;
							callback((
								new MArrayPositionInfo(_x, _x * info.PitchX),
								new MArrayPositionInfo(_y, _y * info.PitchY)
							));
						}
					);
					break;
                case MArrayStyle.SPIRAL_IN_CCW:
					GenerateArray2D_SpiralIn_CCW(
						info.NumX,
						info.NumY,
						info.StartLocation,
						0,
						xOffset,
						yOffset,
						(index) => {
							var _x = index.X + xOffset;
							var _y = index.Y + yOffset;
							callback((
								new MArrayPositionInfo(_x, _x * info.PitchX),
								new MArrayPositionInfo(_y, _y * info.PitchY)
							));
						}
					);
					break;
                case MArrayStyle.SPIRAL_OUT_CW:
                case MArrayStyle.SPIRAL_OUT_CCW:
				case MArrayStyle.SERPENTILE:
					for (int y = 0; y < info.NumY; y++)
					{
						if (y % 2 == 0)
                        {
							for (int x = 0; x < info.NumX; x++)
							{
								var _x = x + xOffset;
								var _y = y + yOffset;
								callback((
									new MArrayPositionInfo(_x, _x * info.PitchX),
									new MArrayPositionInfo(_y, _y * info.PitchY)
								));
							}
						}
						else
                        {
							for (int x = info.NumX-1; x >= 0; x--)
							{
								var _x = x + xOffset;
								var _y = y + yOffset;
								callback((
									new MArrayPositionInfo(_x, _x * info.PitchX),
									new MArrayPositionInfo(_y, _y * info.PitchY)
								));
							}
						}
					}
					break;
				case MArrayStyle.RANDOM:
				case MArrayStyle.RASTER:
				case MArrayStyle.BOUNDARY_CW:
				case MArrayStyle.BOUNDARY_CCW:
				case MArrayStyle.CHECKER_RASTER:
                case MArrayStyle.CHECKER_SERPENTILE:
                case MArrayStyle.CHECKER_INV_RASTER:
                case MArrayStyle.CHECKER_INV_SERPENTILE:
                default:
					for (int x = 0; x < info.NumX; x++)
					{
						for (int y = 0; y < info.NumY; y++)
						{
							var _x = x + xOffset;
							var _y = y + yOffset;
							callback((
								new MArrayPositionInfo(_x, _x * info.PitchX),
								new MArrayPositionInfo(_y, _y * info.PitchY)
							));
						}
					}
					break;
            }
        }

		public static void GenerateArray2D_SpiralIn_CW(int nx, int ny, MArrayStartLocation location, int indexOffset, int xOffset, int yOffset, Action<(int X, int Y)> callback)
		{
			int _nx = nx - 1, _ny = ny - 1;
			int x, y, dx, dy;

			// determine dx, dy
			switch (location)
			{
				case MArrayStartLocation.TOP_LEFT: // NW
					x = 0; y = _ny;
					dx = 1; dy = 0;
					break;
				case MArrayStartLocation.TOP_RIGHT: // NE
					x = _nx; y = _ny;
					dx = 0; dy = -1;
					break;
				default:
				case MArrayStartLocation.BOTTOM_LEFT: // SW
					x = 0; y = 0;
					dx = 0; dy = 1;
					break;
				case MArrayStartLocation.BOTTOM_RIGHT: // SE
					x = _nx; y = 0;
					dx = -1; dy = 0;
					break;
			}

			int index = 0;
			int m = (int)(((2 * nx) + (2 * ny)) - 4);
			int n = nx * ny;

			if ((nx <= 0) || (ny <= 0))
			{
				return;
			}
			else if ((nx == 1) || (ny == 1))
			{
				if (nx == 1)
				{
					switch (location)
					{
						case MArrayStartLocation.TOP_LEFT: // NW
						case MArrayStartLocation.TOP_RIGHT: // NE
							dx = 0; dy = -1;
							break;
						default:
						case MArrayStartLocation.BOTTOM_LEFT: // SW
						case MArrayStartLocation.BOTTOM_RIGHT: // SE
							dx = 0; dy = 1;
							break;
					}
				}
				else
				{
					switch (location)
					{
						case MArrayStartLocation.TOP_LEFT: // NW
						case MArrayStartLocation.BOTTOM_LEFT: // SW
							dx = 1; dy = 1;
							break;

						case MArrayStartLocation.TOP_RIGHT: // NE
						case MArrayStartLocation.BOTTOM_RIGHT: // SE
							dx = -1; dy = 0;
							break;
					}
				}

				for (index = 0; index < Math.Max(nx, ny); index++)
				{
					callback((x + xOffset, y + yOffset));

					x += dx;
					y += dy;
				}

				return;
			}

			for (int i = 0; i < m; i++)
			{
				callback((x + xOffset, y + yOffset));

				if (index > 0)
				{
					if (
					  ((y == _ny) && (x == _nx)) ||
					  ((y == 0) && (x == _nx)) ||
					  ((x == 0) && (y == _ny)) ||
					  ((x == 0) && (y == 0))
					)
					{
						int lastDx = dx;
						dx = dy;
						dy = -lastDx;
					}
				}

				x = x + dx;
				y = y + dy;
				index += 1;
			}

			if (m < n)
			{
				GenerateArray2D_SpiralIn_CW(nx - 2, ny - 2, MArrayStartLocation.TOP_LEFT, index + indexOffset, xOffset + 1, yOffset + 1, callback);
			}
		}

		public static void GenerateArray2D_SpiralIn_CCW(int nx, int ny, MArrayStartLocation location, int indexOffset, int xOffset, int yOffset, Action<(int X, int Y)> callback)
		{
			int _nx = nx - 1, _ny = ny - 1;
			int x, y, dx, dy;

			// determine dx, dy
			switch (location)
			{
				case MArrayStartLocation.TOP_LEFT: // NW
					x = 0; y = _ny;
					dx = 0; dy = -1;
					break;
				case MArrayStartLocation.TOP_RIGHT: // NE
					x = _nx; y = _ny;
					dx = -1; dy = 0;
					break;
				default:
				case MArrayStartLocation.BOTTOM_LEFT: // SW
					x = 0; y = 0;
					dx = 1; dy = 0;
					break;
				case MArrayStartLocation.BOTTOM_RIGHT: // SE
					x = _nx; y = 0;
					dx = 0; dy = 1;
					break;
			}

			int index = 0;
			int m = (int)(((2 * nx) + (2 * ny)) - 4);
			int n = nx * ny;

			if ((nx <= 0) || (ny <= 0))
			{
				return;
			}
			else if ((nx == 1) || (ny == 1))
			{
				if (nx == 1)
				{
					switch (location)
					{
						case MArrayStartLocation.TOP_LEFT: // NW
						case MArrayStartLocation.TOP_RIGHT: // NE
							dx = 0; dy = -1;
							break;
						default:
						case MArrayStartLocation.BOTTOM_LEFT: // SW
						case MArrayStartLocation.BOTTOM_RIGHT: // SE
							dx = 0; dy = 1;
							break;
					}
				}
				else
				{
					switch (location)
					{
						case MArrayStartLocation.TOP_LEFT: // NW
						case MArrayStartLocation.BOTTOM_LEFT: // SW
							dx = 1; dy = 1;
							break;

						case MArrayStartLocation.TOP_RIGHT: // NE
						case MArrayStartLocation.BOTTOM_RIGHT: // SE
							dx = -1; dy = 0;
							break;
					}
				}

				for (index = 0; index < Math.Max(nx, ny); index++)
				{
					callback((x + xOffset, y + yOffset));

					x += dx;
					y += dy;
				}

				return;
			}

			for (int i = 0; i < m; i++)
			{
				callback((x + xOffset, y + yOffset));

				if (index > 0)
				{
					if (
					  ((y == _ny) && (x == _nx)) ||
					  ((y == 0) && (x == _nx)) ||
					  ((x == 0) && (y == _ny)) ||
					  ((x == 0) && (y == 0))
					)
					{
						int lastDx = dx;
						dx = -dy;
						dy = lastDx;
					}
				}

				x = x + dx;
				y = y + dy;
				index += 1;
			}

			if (m < n)
			{
				GenerateArray2D_SpiralIn_CCW(nx - 2, ny - 2, MArrayStartLocation.TOP_LEFT, index + indexOffset, xOffset + 1, yOffset + 1, callback);
			}
		}

		#endregion
	}
}
