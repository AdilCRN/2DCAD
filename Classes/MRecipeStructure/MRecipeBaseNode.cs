using MSolvLib;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Serialization;

namespace MRecipeStructure.Classes.MRecipeStructure
{
    [Serializable]
    public abstract class MRecipeBaseNode : MLinkedNode
    {
        public string Tag
		{
			get { return _tag; }
			set 
			{ 
				_tag = value;
				NotifyPropertyChanged();
			}
		}
        private string _tag;

        private MArrayInfo _arrayInfo;

        public MArrayInfo ArrayInfo
        {
            get { return _arrayInfo; }
            set
            {
                _arrayInfo = value;
                NotifyPropertyChanged();
            }
        }

        public TransformInfo TransformInfo
        {
            get { return _transformInfo; }
            set
            {
                _transformInfo = value;
                NotifyPropertyChanged();
            }
        }
        private TransformInfo _transformInfo;


        public ObservableCollection<MFiducialInfo> Fiducials
        {
            get { return _fiducials; }
            set 
            { 
                _fiducials = value;
                NotifyPropertyChanged();
            }
        }
        private ObservableCollection<MFiducialInfo> _fiducials;


        private MAlignmentType _alignmentType = MAlignmentType.TypeAuto;

        public MAlignmentType AlignmentType
        {
            get { return _alignmentType; }
            set
            {
                _alignmentType = value;
                NotifyPropertyChanged();
            }
        }


        public MRecipeBaseNode()
            : base()
        {
            ArrayInfo = new MArrayInfo();
            TransformInfo = new TransformInfo();
            Fiducials = new ObservableCollection<MFiducialInfo>();
        }

        protected MRecipeBaseNode(MRecipeBaseNode node)
            : base(node)
        {
            Tag = (string)node.Tag.Clone();
            ArrayInfo = (MArrayInfo)node.ArrayInfo.Clone();
            TransformInfo = (TransformInfo)node.TransformInfo.Clone();
            Fiducials = new ObservableCollection<MFiducialInfo>();
            foreach (var fiducial in node.Fiducials)
                Fiducials.Add((MFiducialInfo)fiducial.Clone());
        }

        public override string ToString()
        {
            return $"{Parent}>{Tag}";
        }
    }
}
