using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml.Serialization;

namespace MRecipeStructure.Classes.MRecipeStructure.Dialogs
{
    /// <summary>
    /// Interaction logic for PasteAsArrayDialog.xaml
    /// </summary>
    public partial class PasteAsArrayDialog : Window
    {
        #region Section: Public Properties

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


        #endregion

        public PasteAsArrayDialog()
        {
            InitializeComponent();

            ArrayInfo = new MArrayInfo();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
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
