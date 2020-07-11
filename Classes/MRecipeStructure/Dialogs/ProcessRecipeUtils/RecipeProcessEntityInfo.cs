using MRecipeStructure.Classes.MRecipeStructure;
using MSolvLib;
using System;
using System.Diagnostics;
using System.IO;

namespace MRecipeStructure.Dialogs.ProcessRecipeUtils
{
    public class RecipeProcessEntityInfo : ViewModel
	{
		#region Section : Private Properties

		private Stopwatch _stopwatch = new Stopwatch();

		#endregion

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

		private MRecipeDeviceLayer _layer;

		public MRecipeDeviceLayer Layer
		{
			get { return _layer; }
			set
			{
				_layer = value;
				NotifyPropertyChanged();
				NotifyPropertyChanged(nameof(NumberOfTiles));
			}
		}

		public int NumberOfTiles
		{
			get
			{
				return Layer.TileDescriptions.Count;
			}
		}

		private TimeSpan _estimatedTime;

		public TimeSpan EstimatedTime
		{
			get { return _estimatedTime; }
			set
			{
				_estimatedTime = value;
				NotifyPropertyChanged();
				NotifyPropertyChanged(nameof(TimeRemaining));
			}
		}


		public string TimeRemaining
		{
			get
			{
				if (
					State == EntityState.COMPLETED
				)
                {
					var data = _stopwatch.Elapsed;
					return $"{data.Hours:00}:{data.Minutes:00}:{data.Seconds:00}";
				}
				else
                {
                    var data = TimeSpan.FromSeconds(
                        (100 - ProgressPercentage) * EstimatedTime.TotalSeconds
                    );

                    return $"{data.Hours:00}:{data.Minutes:00}:{data.Seconds:00}";
				}
			}
		}

		private EntityState _state;

		public EntityState State
		{
			get { return _state; }
			set
			{
				_state = value;
				NotifyPropertyChanged();

                switch (value)
                {
                    case EntityState.RUNNING:
						if (_stopwatch.IsRunning)
							_stopwatch.Restart();
						else
							_stopwatch.Start();
						break;
					case EntityState.ABORTED:
					case EntityState.EMPTY:
					case EntityState.ERROR:
					case EntityState.WAITING:
					case EntityState.PAUSED:
                    case EntityState.COMPLETED:
                    default:
						_stopwatch.Stop();
                        break;
                }
            }
		}

		private double _progressPrecentage;

		public double ProgressPercentage
		{
			get { return _progressPrecentage; }
			set
			{
				_progressPrecentage = Math.Min(Math.Max(0, value), 100);
				NotifyPropertyChanged();
				NotifyPropertyChanged(nameof(TimeRemaining));
				NotifyPropertyChanged(nameof(IsIndeterminate));
			}
		}

		public bool IsIndeterminate
		{
			get
			{
				return (ProgressPercentage < 1);
			}
		}

		public bool IsValid
		{
			get
			{
				return File.Exists(Layer?.PatternFilePath) && File.Exists(Layer?.ProcessParametersFilePath);
			}
		}

		public bool NeedsAligment
		{
			get
			{
				return Layer?.Fiducials.Count > 0;
			}
		}

		public RecipeProcessEntityInfo()
		{
			State = EntityState.WAITING;
			ProgressPercentage = 0;
		}
	}
}
