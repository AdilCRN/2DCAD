using Prism.Commands;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace MRecipeStructure.Classes.MRecipeStructure.UserControls.ArrayInfoUserControl
{
    /// <summary>
    /// Interaction logic for ArrayInfoUserControl.xaml
    /// </summary>
    public partial class ArrayInfoUserControl : UserControl
    {
        #region Section: DependencyProperty

        public static readonly DependencyProperty ArrayInfoItemSourceProperty = DependencyProperty.Register(
            "ArrayInfoItemSource", typeof(MArrayInfo), typeof(ArrayInfoUserControl),
            new FrameworkPropertyMetadata(
                null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged
            )
        );

        #endregion

        #region Section: Public Properties

        public MArrayInfo ArrayInfoItemSource
        {
            get
            {
                return (MArrayInfo)GetValue(ArrayInfoItemSourceProperty);
            }
            set
            {
                SetValue(ArrayInfoItemSourceProperty, value);
            }
        }

        #endregion

        public ArrayInfoUserControl()
        {
            InitializeComponent();
        }

        public static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs Args)
        {
            var view = sender as ArrayInfoUserControl;
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
