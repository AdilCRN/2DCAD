using netDxf.Entities;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace MRecipeStructure.Classes.MRecipeStructure.UserControls.EditableTextBlockUserControl
{
    /// <summary>
    /// Interaction logic for EditableTextBlockUserControl.xaml
    /// </summary>
    public partial class EditableTextBlockUserControl : UserControl, INotifyPropertyChanged
    {

        #region Section: DependencyProperty

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(EditableTextBlockUserControl),
            new FrameworkPropertyMetadata(
                null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged
            )
        );

        public static readonly DependencyProperty IsEditableProperty = DependencyProperty.Register(
            "IsEditable", typeof(bool), typeof(EditableTextBlockUserControl),
            new FrameworkPropertyMetadata(
                true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged
            )
        );

        #endregion

        #region Public Properties

        public string Text
        {
            get
            {
                return (string)GetValue(TextProperty);
            }
            set
            {
                SetValue(TextProperty, value);
            }
        }

        public bool IsEditable
        {
            get
            {
                return (bool)GetValue(IsEditableProperty);
            }
            set
            {
                SetValue(IsEditableProperty, value);
            }
        }

        private bool _isEditing = false;

        public bool IsEditing
        {
            get { return _isEditing; }
            protected set 
            { 
                _isEditing = value;
                NotifyPropertyChanged();
            }
        }


        #endregion

        #region Section: Public Data Binding

        private string _mText;

        public string mText
        {
            get { return _mText; }
            set 
            { 
                _mText = value;
                NotifyPropertyChanged();

                Text = _mText;
            }
        }

        #endregion

        #region Section: Delegate Commands

        public DelegateCommand StopEditingCommand { get; set; }
        public DelegateCommand LeftDoubleClickCommand { get; set; }

        #endregion

        public EditableTextBlockUserControl()
        {
            InitializeComponent();

            StopEditingCommand = new DelegateCommand(()=> {
                if (IsEditing)
                    IsEditing = false;
            });

            LeftDoubleClickCommand = new DelegateCommand(()=> {
                if (IsEditable)
                    IsEditing = !IsEditing;

                IsEditing = true;
            });
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            IsEditing = false;
        }

        public void UpdateItemsSource()
        {
            _mText = Text;
            NotifyPropertyChanged(nameof(mText));
        }

        public static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs Args)
        {
            var view = sender as EditableTextBlockUserControl;
            view.UpdateItemsSource();
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
