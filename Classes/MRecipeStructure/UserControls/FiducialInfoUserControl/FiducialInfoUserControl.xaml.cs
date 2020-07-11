using Prism.Commands;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace MRecipeStructure.Classes.MRecipeStructure.UserControls.FiducialInfoUserControl
{
    /// <summary>
    /// Interaction logic for FiducialInfoUserControl.xaml
    /// </summary>
    public partial class FiducialInfoUserControl : UserControl
    {
        #region Section: DependencyProperty

        public static readonly DependencyProperty MRecipeNodeItemSourceProperty = DependencyProperty.Register(
            "MRecipeNodeItemSource", typeof(MRecipeBaseNode), typeof(FiducialInfoUserControl),
            new FrameworkPropertyMetadata(
                null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged
            )
        );

        #endregion

        #region Section: Public Properties

        public MRecipeBaseNode MRecipeNodeItemSource
        {
            get
            {
                return (MRecipeBaseNode)GetValue(MRecipeNodeItemSourceProperty);
            }
            set
            {
                SetValue(MRecipeNodeItemSourceProperty, value);
            }
        }

        #endregion

        #region Section: Data Binding

        #endregion

        #region Section: Delegate Commands

        public DelegateCommand AddCommand { get; set; }
        public DelegateCommand DeleteAllCommand { get; set; }
        public DelegateCommand<MFiducialInfo> DeleteCommand { get; set; }

        #endregion

        public FiducialInfoUserControl()
        {
            InitializeComponent();

            AddCommand = new DelegateCommand(
                ()=> {
                    // add new fiducial info
                    MRecipeNodeItemSource.Fiducials.Add(
                        new MFiducialInfo() 
                        { 
                            Index = MRecipeNodeItemSource.Fiducials.Count 
                        }
                    );
                }
            );

            DeleteAllCommand = new DelegateCommand(
                () => {
                    MRecipeNodeItemSource.Fiducials.Clear();
                }
            );

            DeleteCommand = new DelegateCommand<MFiducialInfo>(
                (selectedInfo) => {
                    // delete fiducial info
                    MRecipeNodeItemSource.Fiducials.Remove(selectedInfo);

                    // update index
                    for (int i = 0; i < MRecipeNodeItemSource.Fiducials.Count; i++)
                        MRecipeNodeItemSource.Fiducials[i].Index = i;
                }
            );
        }

        public static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs Args)
        {
            var view = sender as FiducialInfoUserControl;
        }

        #region Section: INotifyPropertyChanged
        [XmlIgnore]
        private bool ThrowOnInvalidPropertyName = true;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.VerifyPropertyName(propertyName);
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            if (Debugger.IsAttached)
            {
                // Verify that the property name matches a real, 
                // public, instance property on this object. 
                if (TypeDescriptor.GetProperties(this)[propertyName] == null)
                {
                    string msg = "Invalid property name: " + propertyName;
                    if (ThrowOnInvalidPropertyName)
                        throw new Exception(msg);
                    else
                        Debug.Fail(msg);
                }
            }
        }
        #endregion
    }
}
