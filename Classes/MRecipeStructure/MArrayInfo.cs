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

        private MArrayStyle _arrayStyle = MArrayStyle.SERPENTILE_W2E;

        public MArrayStyle ArrayStyle
		{
            get { return _arrayStyle; }
            set
			{ 
				_arrayStyle = value;
				NotifyPropertyChanged();
			}
        }


        public MArrayInfo()
		{
			NumX = 1; NumY = 1; NumZ = 1;
			PitchX = 0; PitchY = 0; PitchZ = 0;
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
		}

		/// <summary>
		///		Auto generates position info using the given array info.
		/// </summary>
		/// <param name="info">The array info.</param>
		/// <param name="callback">A callback to receive the calculated values.</param>
		public static void BeginGetAll(MArrayInfo info, Action<(MArrayPositionInfo XInfo, MArrayPositionInfo YInfo, MArrayPositionInfo ZInfo)> callback)
        {
			// TODO : Add support for array indexing styles
            for (int x = 0; x < info.NumX; x++)
            {
                for (int y = 0; y < info.NumY; y++)
                {
                    for (int z = 0; z < info.NumZ; z++)
                    {
						callback((
							new MArrayPositionInfo(x, x*info.PitchX),
							new MArrayPositionInfo(y, y*info.PitchY),
							new MArrayPositionInfo(z, z*info.PitchZ)
						));
					}
                }
            }
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
	}
}
