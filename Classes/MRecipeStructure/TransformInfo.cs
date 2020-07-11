using MSolvLib;
using MSolvLib.MarkGeometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MRecipeStructure.Classes.MRecipeStructure
{
    [Serializable]
    public class TransformInfo : ViewModel, ICloneable
    {
		private double _scaleX;

		public double ScaleX
		{
			get { return _scaleX; }
			set 
			{ 
				_scaleX = Math.Round(value, 5);
				NotifyPropertyChanged();
			}
		}

		private double _scaleY;

		public double ScaleY
		{
			get { return _scaleY; }
			set
			{
				_scaleY = Math.Round(value, 5);
				NotifyPropertyChanged();
			}
		}

		private double _scaleZ;

		public double ScaleZ
		{
			get { return _scaleZ; }
			set
			{
				_scaleZ = Math.Round(value, 5);
				NotifyPropertyChanged();
			}
		}

		private double _offsetX;

		public double OffsetX
		{
			get { return _offsetX; }
			set 
			{
				_offsetX = Math.Round(value, 5);
				NotifyPropertyChanged();
			}
		}

		private double _offsetY;

		public double OffsetY
		{
			get { return _offsetY; }
			set
			{
				_offsetY = Math.Round(value, 5);
				NotifyPropertyChanged();
			}
		}

		private double _offsetZ;

		public double OffsetZ
		{
			get { return _offsetZ; }
			set
			{
				_offsetZ = Math.Round(value, 5);
				NotifyPropertyChanged();
			}
		}

		private double _rotationDegX;

		public double RotationDegX
		{
			get { return _rotationDegX; }
			set 
			{ 
				_rotationDegX = Math.Round(value, 5);
				NotifyPropertyChanged();
			}
		}

		private double _rotationDegY;

		public double RotationDegY
		{
			get { return _rotationDegY; }
			set
			{
				_rotationDegY = Math.Round(value, 5);
				NotifyPropertyChanged();
			}
		}

		private double _rotationDegZ;

		public double RotationDegZ
		{
			get { return _rotationDegZ; }
			set
			{
				_rotationDegZ = Math.Round(value, 5);
				NotifyPropertyChanged();
			}
		}

		public TransformInfo()
		{
			ScaleX = 1d; ScaleY = 1d; ScaleZ = 1d;
			OffsetX = 0d; OffsetY = 0d; OffsetZ = 0d;
			RotationDegX = 0d; RotationDegY = 0d; RotationDegZ = 0d;
		}

		public TransformInfo(double offsetX, double offsetY, double rotationDegZ)
			: this()
		{
			OffsetX = offsetX;
			OffsetY = offsetY;
			RotationDegZ = rotationDegZ;
		}

		/// <summary>
		///		The copy constructor
		/// </summary>
		/// <param name="info"></param>
		private TransformInfo(TransformInfo info)
		{
			OffsetX = info.OffsetX;
			OffsetY = info.OffsetY;
			OffsetZ = info.OffsetZ;

			ScaleX = info.ScaleX;
			ScaleY = info.ScaleY;
			ScaleZ = info.ScaleZ;

			RotationDegX = info.RotationDegX;
			RotationDegY = info.RotationDegY;
			RotationDegZ = info.RotationDegZ;
		}

		public Matrix4x4 ToMatrix4x4()
		{
			return GeometricArithmeticModule.CombineTransformations(
				// apply scale
				GeometricArithmeticModule.GetScalingTransformationMatrix(
					ScaleX, ScaleY, ScaleZ
				),

				// apply rotation - don't forget to convert degrees to radians
				GeometricArithmeticModule.GetRotationTransformationMatrix(
					GeometricArithmeticModule.ToRadians(RotationDegX),
					GeometricArithmeticModule.ToRadians(RotationDegY),
					GeometricArithmeticModule.ToRadians(RotationDegZ)
				),

				// apply offset
				GeometricArithmeticModule.GetTranslationTransformationMatrix(
					OffsetX, OffsetY, OffsetZ
				)
			);
		}

		public object Clone()
		{
			return new TransformInfo(this);
		}
	}
}
