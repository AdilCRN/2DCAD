using Prism.Commands;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace MRecipeStructure.Classes.MRecipeStructure.UserControls.TransformInfoUserControl
{
    /// <summary>
    /// Interaction logic for TransformInfoUserControl.xaml
    /// </summary>
    public partial class TransformInfoUserControl : UserControl
    {
        #region Section: DependencyProperty

        public static readonly DependencyProperty TransformInfoItemSourceProperty = DependencyProperty.Register(
            "TransformInfoItemSource", typeof(TransformInfo), typeof(TransformInfoUserControl),
            new FrameworkPropertyMetadata(
                null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged
            )
        );

        #endregion

        #region Section: Public Properties

        public TransformInfo TransformInfoItemSource
        {
            get
            {
                return (TransformInfo)GetValue(TransformInfoItemSourceProperty);
            }
            set
            {
                SetValue(TransformInfoItemSourceProperty, value);
            }
        }

        #endregion

        public TransformInfoUserControl()
        {
            InitializeComponent();
        }

        public static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs Args)
        {
            var view = sender as TransformInfoUserControl;
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
