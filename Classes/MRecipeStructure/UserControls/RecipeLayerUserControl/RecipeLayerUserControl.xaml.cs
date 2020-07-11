using Microsoft.Win32;
using MSolvLib;
using MSolvLib.Classes.ProcessConfiguration;
using MSolvLib.DialogForms;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace MRecipeStructure.Classes.MRecipeStructure.UserControls.RecipeLayerUserControl
{
    /// <summary>
    /// Interaction logic for RecipeLayerUserControl.xaml
    /// </summary>
    public partial class RecipeLayerUserControl : UserControl
    {
        #region Section: DependencyProperty

        public static readonly DependencyProperty MRecipeDeviceLayerItemSourceProperty = DependencyProperty.Register(
            "MRecipeDeviceLayerItemSource", typeof(MRecipeDeviceLayer), typeof(RecipeLayerUserControl),
            new FrameworkPropertyMetadata(
                null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged
            )
        );

        public static readonly DependencyProperty AvailableProcessConfigurationsItemSourceProperty = DependencyProperty.Register(
            "AvailableProcessConfigurationsItemSource", typeof(ObservableCollection<IProcessConfiguration>), typeof(RecipeLayerUserControl),
            new FrameworkPropertyMetadata(
                null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged
            )
        );

        #endregion

        #region Section: Public Properties

        public MRecipeDeviceLayer MRecipeDeviceLayerItemSource
        {
            get
            {
                return (MRecipeDeviceLayer)GetValue(MRecipeDeviceLayerItemSourceProperty);
            }
            set
            {
                SetValue(MRecipeDeviceLayerItemSourceProperty, value);
            }
        }

        public ObservableCollection<IProcessConfiguration> AvailableProcessConfigurationsItemSource
        {
            get 
            { 
                return (ObservableCollection<IProcessConfiguration>)GetValue(AvailableProcessConfigurationsItemSourceProperty); 
            }
            set
            {
                SetValue(AvailableProcessConfigurationsItemSourceProperty, value);
            }
        }

        #endregion

        #region Section: Data Binding

        private IProcessConfiguration _selectedProcessConfiguration;

        public IProcessConfiguration SelectedProcessConfiguration
        {
            get { return _selectedProcessConfiguration; }
            set 
            { 
                _selectedProcessConfiguration = value;
                NotifyPropertyChanged();

                if (MRecipeDeviceLayerItemSource != null)
                {
                    // update process mode
                    MRecipeDeviceLayerItemSource.TargetProcessMode = SelectedProcessConfiguration?.Name.EnglishValue;
                }
            }
        }

        #endregion

        #region Section: Delegate Commands

        public DelegateCommand SelectPatternFileCommand { get; set; }
        public DelegateCommand SelectParametersFileCommand { get; set; }

        #endregion

        public RecipeLayerUserControl()
        {
            InitializeComponent();

            SelectPatternFileCommand = new DelegateCommand(
                () =>
                {
                    var dialog = new OpenFileDialog();
                    dialog.Filter = "DXF files (*.dxf)|*.dxf";
                    dialog.AddExtension = true;

                    if (File.Exists(MRecipeDeviceLayerItemSource.PatternFilePath))
                        dialog.InitialDirectory = Path.GetDirectoryName(MRecipeDeviceLayerItemSource.PatternFilePath);

                    if (dialog.ShowDialog() != true)
                        return;

                    MRecipeDeviceLayerItemSource.PatternFilePath = dialog.FileName;
                }
            );

            SelectParametersFileCommand = new DelegateCommand(
                () =>
                {
                    if (SelectedProcessConfiguration == null)
                    {
                        DispatcherMessageBox.ShowBox(
                            @"Invalid process mode selected, please select a valid process mode.",
                            "Select a Process Mode"
                        );

                        return;
                    }

                    var dialog = new MarkParamEditorDialog(
                        SelectedProcessConfiguration?.ProcessParameterFileManager,
                        MRecipeDeviceLayerItemSource.ProcessParametersFilePath
                    );

                    if (dialog.ShowDialog() == true)
                    {
                        MRecipeDeviceLayerItemSource.ProcessParametersFilePath = dialog.ParamFileManager.FilePath;
                    }
                }
            );
        }

        public void UpdateData()
        {
            try
            {
                var foo = AvailableProcessConfigurationsItemSource?.First(
                    config => config.Name.EnglishValue == MRecipeDeviceLayerItemSource?.TargetProcessMode
                );

                if (foo != null)
                {
                    _selectedProcessConfiguration = foo;
                    NotifyPropertyChanged(nameof(SelectedProcessConfiguration));
                }
            }
            catch (Exception) { }
        }

        public static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs Args)
        {
            var view = sender as RecipeLayerUserControl;
            view.UpdateData();
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
