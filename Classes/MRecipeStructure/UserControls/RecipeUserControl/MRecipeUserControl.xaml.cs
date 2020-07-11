using MRecipeStructure.Classes.MRecipeStructure.Dialogs;
using MRecipeStructure.Classes.MRecipeStructure.Utils;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;

namespace MRecipeStructure.Classes.MRecipeStructure.UserControls.RecipeUserControl
{
    /// <summary>
    /// Interaction logic for MRecipeUserControl.xaml
    /// </summary>
    public partial class MRecipeUserControl : UserControl, INotifyPropertyChanged
    {
        #region Section: DependencyProperty

        public static readonly DependencyProperty RecipeItemsSourceProperty = DependencyProperty.Register(
            "RecipeItemsSource", typeof(MRecipe), typeof(MRecipeUserControl),
            new FrameworkPropertyMetadata(
                null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged
            )
        );

        public static readonly DependencyProperty RecipeItemSelectedCommandProperty = DependencyProperty.Register(
            "RecipeItemSelectedCommand", typeof(DelegateCommand<MRecipeBaseNode>), typeof(MRecipeUserControl),
            new FrameworkPropertyMetadata(
                null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
            )
        );

        #endregion

        #region Section: Private Properties

        // for drag and drop
        private Point _lastMouseDown = new Point();
        private MRecipeBaseNode _draggedItem = null;
        private MRecipeBaseNode _target = null;

        // for context menu
        private MRecipeBaseNode _lastSelectedNode;
        private MRecipeBaseNode _copiedItem;

        // for undo redo
        private UndoRedoHelper<MRecipe> _undoRedoHelper;

        #endregion

        #region Section: Public Properties

        public MRecipe RecipeItemsSource
        {
            get 
            { 
                return (MRecipe)GetValue(RecipeItemsSourceProperty); 
            }
            set 
            {
                SetValue(RecipeItemsSourceProperty, value);
            }
        }

        public DelegateCommand<MRecipeBaseNode> RecipeItemSelectedCommand
        {
            get 
            { 
                return (DelegateCommand<MRecipeBaseNode>)GetValue(RecipeItemSelectedCommandProperty); 
            }
            set 
            {
                SetValue(RecipeItemSelectedCommandProperty, value);
            }
        }

        #endregion

        #region Section: Data Binding

        private MRecipe _mRecipe;

        public MRecipe MRecipe
        {
            get { return _mRecipe; }
            set 
            { 
                _mRecipe = value;
                NotifyPropertyChanged();
            }
        }

        public bool CanPasteItem
        {
            get 
            { 
                return (_copiedItem != null) && 
                    (
                        _copiedItem.Parent.GetType() == _lastSelectedNode.GetType() ||
                        _copiedItem.GetType() == _lastSelectedNode.GetType()
                ); 
            }
        }

        public bool CanCreateChild
        {
            get 
            { 
                return !(_lastSelectedNode is MRecipeDeviceLayer); 
            }
        }

        #endregion

        #region Section: Delegate Commands

        public DelegateCommand RedoCommand { get; set; }
        public DelegateCommand UndoCommand { get; set; }
        public DelegateCommand<MRecipeBaseNode> RightClickContextMenuCommand { get; set; }
        public DelegateCommand<MRecipeBaseNode> ItemSelectedCommand { get; set; }
        public DelegateCommand<MRecipeBaseNode> NewItemCommand { get; set; }
        public DelegateCommand<MRecipeBaseNode> CreateChildItemCommand { get; set; }
        public DelegateCommand<MRecipeBaseNode> DuplicateItemCommand { get; set; }
        public DelegateCommand<MRecipeBaseNode> DuplicateItemAsArrayCommand { get; set; }
        public DelegateCommand<MRecipeBaseNode> CopyItemCommand { get; set; }
        public DelegateCommand<MRecipeBaseNode> PasteItemCommand { get; set; }
        public DelegateCommand<MRecipeBaseNode> PasteItemAsArrayCommand { get; set; }
        public DelegateCommand<MRecipeBaseNode> DeleteItemCommand { get; set; }

        #endregion

        public MRecipeUserControl()
        {
            InitializeComponent();

            UndoCommand = new DelegateCommand(
                () =>
                {
                    Undo();
                }
            );

            RedoCommand = new DelegateCommand(
                () =>
                {
                    Redo();
                }
            );

            ItemSelectedCommand = new DelegateCommand<MRecipeBaseNode>(
                (selectedItem)=> {
                    NotifyPropertyChanged(nameof(CanPasteItem));
                    NotifyPropertyChanged(nameof(CanCreateChild));

                    // run receipe selected callback on a different thread
                    SelectNode(selectedItem);
                }
            );

            RightClickContextMenuCommand = new DelegateCommand<MRecipeBaseNode>(
                (selectedItem) => {
                    _lastSelectedNode = selectedItem;
                    NotifyPropertyChanged(nameof(CanPasteItem));
                    NotifyPropertyChanged(nameof(CanCreateChild));
                }
            );

            NewItemCommand = new DelegateCommand<MRecipeBaseNode>(
                (node) => {
                    if (node is MRecipePlate plate)
                    {
                        (node.Parent as MRecipe).Plates.Add(
                            new MRecipePlate() { Parent = node.Parent }
                        );
                    }
                    else if (node is MRecipeDevice device)
                    {
                        (node.Parent as MRecipePlate).Devices.Add(
                            new MRecipeDevice() { Parent = node.Parent }
                        );
                    }
                    else if (node is MRecipeDeviceLayer layer)
                    {
                        (node.Parent as MRecipeDevice).Layers.Add(
                            new MRecipeDeviceLayer() { Parent = node.Parent }
                        );
                    }
                    else
                    {
                        return;
                    }

                    lock (_undoRedoHelper)
                        _undoRedoHelper.SaveState();
                    SelectNode(node);
                }
            );

            CreateChildItemCommand = new DelegateCommand<MRecipeBaseNode>(
                (node) => {
                    if (node is MRecipePlate plate)
                    {
                        plate.Devices.Add(
                            new MRecipeDevice() { Parent = node }
                        );
                    }
                    else if (node is MRecipeDevice device)
                    {
                        device.Layers.Add(
                            new MRecipeDeviceLayer() { Parent = node }
                        );
                    }
                    else
                    {
                        return;
                    }

                    lock (_undoRedoHelper)
                        _undoRedoHelper.SaveState();

                    SelectNode(node);
                }
            );

            DuplicateItemCommand = new DelegateCommand<MRecipeBaseNode>(
                (node) => {

                    CopyItem((MRecipeBaseNode)node.Clone(), (MRecipeBaseNode)node.Parent, false);

                    lock (_undoRedoHelper)
                        _undoRedoHelper.SaveState();

                    SelectNode(node);
                }
            );

            DuplicateItemAsArrayCommand = new DelegateCommand<MRecipeBaseNode>(
                (node) => {

                    var dialog = new PasteAsArrayDialog();
                    dialog.Owner = Application.Current.MainWindow;
                    if (dialog.ShowDialog() != true)
                        return;

                    var targetNode = (MRecipeBaseNode)node.Parent;

                    try
                    {
                        lock (_undoRedoHelper)
                            _undoRedoHelper?.Pause();

                        MArrayInfo.BeginGetAll(dialog.ArrayInfo, (positionInfo)=> {

                            var clone = (MRecipeBaseNode)node.Clone();
                            clone.TransformInfo.OffsetX += positionInfo.XInfo.Offset;
                            clone.TransformInfo.OffsetY += positionInfo.XInfo.Offset;
                            clone.TransformInfo.OffsetZ += positionInfo.XInfo.Offset;

                            CopyItem(clone, targetNode, false);
                        });
                    }
                    finally
                    {
                        lock (_undoRedoHelper)
                        {
                            _undoRedoHelper?.Resume();
                            _undoRedoHelper.SaveState();
                        }
                    }

                    SelectNode(node);
                }
            );

            CopyItemCommand = new DelegateCommand<MRecipeBaseNode>(
                (node) => {
                    if (node != null)
                        _copiedItem = node;
                }
            );

            PasteItemCommand = new DelegateCommand<MRecipeBaseNode>(
                (node) => {

                    if (node == null)
                        return;
                    
                    // update selected node
                    _lastSelectedNode = node;
                    if (CanPasteItem == false)
                        return;

                    CopyItem((MRecipeBaseNode)_copiedItem.Clone(), _lastSelectedNode, false);
                    SelectNode(node);
                }
            );

            PasteItemAsArrayCommand = new DelegateCommand<MRecipeBaseNode>(
                (node) => {
                    if (node == null)
                        return;

                    // update selected node
                    _lastSelectedNode = node;
                    if (CanPasteItem == false)
                        return;

                    var dialog = new PasteAsArrayDialog();
                    dialog.Owner = Application.Current.MainWindow;
                    if (dialog.ShowDialog() != true)
                        return;

                    var selectedNode = _lastSelectedNode;

                    try
                    {
                        lock (_undoRedoHelper)
                            _undoRedoHelper?.Pause();

                        for (int x = 0; x < dialog.ArrayInfo.NumX; x++)
                        {
                            for (int y = 0; y < dialog.ArrayInfo.NumY; y++)
                            {
                                for (int z = 0; z < dialog.ArrayInfo.NumZ; z++)
                                {
                                    var clone = (MRecipeBaseNode)_copiedItem.Clone();
                                    clone.TransformInfo.OffsetX += x * dialog.ArrayInfo.PitchX;
                                    clone.TransformInfo.OffsetY += y * dialog.ArrayInfo.PitchY;
                                    clone.TransformInfo.OffsetZ += z * dialog.ArrayInfo.PitchZ;

                                    CopyItem(clone, selectedNode, false);
                                }
                            }
                        }
                    }
                    finally
                    {
                        lock (_undoRedoHelper)
                        {
                            _undoRedoHelper?.Resume();
                            _undoRedoHelper.SaveState();
                        }
                    }

                    SelectNode(node);
                }
            );

            DeleteItemCommand = new DelegateCommand<MRecipeBaseNode>(
                (node) => {
                    if (node is MRecipePlate plate)
                    {
                        (node.Parent as MRecipe).Plates.Remove(
                            plate
                        );
                    }
                    else if (node is MRecipeDevice device)
                    {
                        (node.Parent as MRecipePlate).Devices.Remove(
                            device
                        );
                    }
                    else if (node is MRecipeDeviceLayer layer)
                    {
                        (node.Parent as MRecipeDevice).Layers.Remove(
                            layer
                        );
                    }
                    else
                    {
                        return;
                    }

                    lock (_undoRedoHelper)
                        _undoRedoHelper.SaveState();

                    SelectNode(node);
                }
            );
        }

        public void SelectNode(MRecipeBaseNode node)
        {
            _lastSelectedNode = node;
            RecipeItemSelectedCommand?.Execute(node);
        }

        public void UpdateItemsSource()
        {
            if (RecipeItemsSource != null)
            {
                _lastSelectedNode = null;
                _mRecipe = RecipeItemsSource;
                
                if (_undoRedoHelper?.IsPaused != true)
                {
                    _undoRedoHelper = new UndoRedoHelper<MRecipe>(ref _mRecipe);
                    MRecipe.BeginGetNodes(_mRecipe, (baseNode) => {
                        _undoRedoHelper.SaveStateOnPropertyChange(baseNode);
                    });

                    _undoRedoHelper.SaveState();
                }

                NotifyPropertyChanged(nameof(MRecipe));
            }
        }

        public static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs Args)
        {
            var view = sender as MRecipeUserControl;
            view.UpdateItemsSource();
        }

        #region Section: Undo and Redo

        public void Undo()
        {
            if (_undoRedoHelper == null)
                return;

            lock(_undoRedoHelper)
            {
                _undoRedoHelper.Pause();

                var data = _undoRedoHelper.Undo();

                if (data != default(MRecipe))
                {
                    _undoRedoHelper.DisposeSubscriptions();

                    RecipeItemsSource = data;

                    _undoRedoHelper.SaveStateOnPropertyChange(_mRecipe);
                    MRecipe.BeginGetNodes(_mRecipe, (baseNode) => {
                        _undoRedoHelper.SaveStateOnPropertyChange(baseNode);
                    });
                }

                _undoRedoHelper.Resume();
            }
        }

        public void Redo()
        {
            if (_undoRedoHelper == null)
                return;

            lock (_undoRedoHelper)
            {
                _undoRedoHelper.Pause();

                var data = _undoRedoHelper.Redo();

                if (data != default(MRecipe))
                {
                    _undoRedoHelper.DisposeSubscriptions();

                    RecipeItemsSource = data;

                    _undoRedoHelper.SaveStateOnPropertyChange(_mRecipe);
                    MRecipe.BeginGetNodes(_mRecipe, (baseNode) => {
                        _undoRedoHelper.SaveStateOnPropertyChange(baseNode);
                    });
                }

                _undoRedoHelper.Resume();
            }
        }

        #endregion

        #region Section: Drag and Drop - Reorder

        // ADPATED  from https://www.codeproject.com/Articles/55168/Drag-and-Drop-Feature-in-WPF-TreeView-Control

        /// <summary>
        ///     This event occurs when any mouse button is down. In this event, we 
        ///     first check the button down, then save mouse position in a variable if left button is down.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _lastMouseDown = e.GetPosition(RecipeTreeView);
            }
        }

        /// <summary>
        ///     This event occurs when mouse is moved.Here first we check whether 
        ///     left mouse button is pressed or not.Then check the distance mouse 
        ///     moved if it moves outside the selected treeview item, then check 
        ///     the drop effect if it's dragged (move) and then dropped over a 
        ///     TreeViewItem (i.e. target is not null) then copy the selected 
        ///     item in dropped item. In this event, you can put your desired 
        ///     condition for dropping treeviewItem.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    Point currentPosition = e.GetPosition(RecipeTreeView);

                    if (
                        (Math.Abs(currentPosition.X - _lastMouseDown.X) > 10.0) ||
                        (Math.Abs(currentPosition.Y - _lastMouseDown.Y) > 10.0)
                    )
                    {
                        _draggedItem = (MRecipeBaseNode)RecipeTreeView.SelectedItem;

                        if (RecipeTreeView != null)
                        {
                            var finalDropEffect = DragDrop.DoDragDrop(
                                RecipeTreeView,
                                RecipeTreeView.SelectedValue,
                                DragDropEffects.Move
                            );

                            //Checking target is not null and item is dragging(moving)
                            if (
                                (finalDropEffect == DragDropEffects.Move) && (_target != null)
                            )
                            {
                                // A Move drop was accepted - prevent drop on itself
                                if (_draggedItem != _target)
                                {
                                    // handle drop
                                    CopyItem(_draggedItem, _target);
                                    _target = null;
                                    _draggedItem = null;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        ///     This event occurs when an object is dragged (moves) within the drop target's boundary. 
        ///     Here, we check whether the pointer is near a TreeViewItem or not; if near, then set Drop effect on it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                Point currentPosition = e.GetPosition(RecipeTreeView);

                if ((Math.Abs(currentPosition.X - _lastMouseDown.X) > 10.0) ||
                   (Math.Abs(currentPosition.Y - _lastMouseDown.Y) > 10.0))
                {
                    // Verify that this is a valid drop and then store the drop target
                    var item = GetNearestContainer(e.OriginalSource);
                    if (CheckDropTarget(_draggedItem, item))
                    {
                        e.Effects = DragDropEffects.Move;
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;
                    }
                }
                e.Handled = true;
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        ///     This event occurs when an object is dropped on the drop target. 
        ///     Here we check whether the dropped item is dropped on a 
        ///     TreeViewItem or not. If yes, then set drop effect to none 
        ///     and the target item into a variable. And then MouseMove 
        ///     event completes the drag and drop operation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            try
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;

                // Verify that this is a valid drop and then store the drop target
                var targetItem = GetNearestContainer(e.OriginalSource);
                if (targetItem != null && _draggedItem != null)
                {
                    _target = targetItem;
                    e.Effects = DragDropEffects.Move;
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        ///     Checking if the source item can be dropped into the target item
        /// </summary>
        /// <param name="sourceItem"></param>
        /// <param name="targetItem"></param>
        /// <returns></returns>
        private bool CheckDropTarget(MRecipeBaseNode sourceItem, MRecipeBaseNode targetItem)
        {
            return (
                sourceItem.GetType() == targetItem.GetType() || 
                sourceItem.Parent?.GetType() == targetItem.GetType()
            );
        }

        /// <summary>
        ///     Insert source item into target
        /// </summary>
        /// <param name="sourceItem"></param>
        /// <param name="targetItem"></param>
        private void CopyItem(MRecipeBaseNode sourceItem, MRecipeBaseNode targetItem, bool deleteSource = true)
        {
            if (
                (sourceItem.GetType() == targetItem.GetType()) && 
                targetItem.Parent != null && 
                sourceItem.Parent != null
            )
            {
                if (targetItem.Parent is MRecipePlate)
                {
                    var data = (sourceItem as MRecipeDevice);
                    var targetParent = (targetItem.Parent as MRecipePlate);
                    var sourceParent = (sourceItem.Parent as MRecipePlate);
                    int targetIndex = targetParent.Devices.IndexOf(targetItem as MRecipeDevice);

                    // remove data from source and insert data into target
                    if (deleteSource)
                        sourceParent.Devices.Remove(data);
                    targetParent.Devices.Insert(targetIndex, data);

                    // update source's parent
                    sourceItem.Parent = targetItem.Parent;
                    lock (_undoRedoHelper)
                        _undoRedoHelper.SaveState();
                }
                else if (sourceItem.Parent is MRecipeDevice)
                {
                    var data = (sourceItem as MRecipeDeviceLayer);
                    var targetParent = (targetItem.Parent as MRecipeDevice);
                    var sourceParent = (sourceItem.Parent as MRecipeDevice);
                    int targetIndex = targetParent.Layers.IndexOf(targetItem as MRecipeDeviceLayer);

                    // remove data from source and insert data into target
                    if (deleteSource)
                        sourceParent.Layers.Remove(data);
                    targetParent.Layers.Insert(targetIndex, data);

                    // update source's parent
                    sourceItem.Parent = targetItem.Parent;
                    lock (_undoRedoHelper)
                        _undoRedoHelper.SaveState();
                }

                //var response = MessageBox.Show(
                //    $"Would you like to move {sourceItem.Tag} into position" +  + "", "", MessageBoxButton.YesNo
                //);
            }
            else if (
                (sourceItem.Parent?.GetType() == targetItem.GetType())
            )
            {
                if (targetItem is MRecipePlate)
                {
                    var data = (sourceItem as MRecipeDevice);
                    var targetParent = (targetItem as MRecipePlate);
                    var sourceParent = (sourceItem.Parent as MRecipePlate);

                    // remove data from source and add data to target
                    if (deleteSource)
                        sourceParent.Devices.Remove(data);
                    targetParent.Devices.Add(data);

                    // update source's parent
                    sourceItem.Parent = targetParent;
                    lock (_undoRedoHelper)
                        _undoRedoHelper.SaveState();
                }
                else if (targetItem is MRecipeDevice)
                {
                    var data = (sourceItem as MRecipeDeviceLayer);
                    var targetParent = (targetItem as MRecipeDevice);
                    var sourceParent = (sourceItem.Parent as MRecipeDevice);

                    // remove data from source and add data to target
                    if (deleteSource)
                        sourceParent.Layers.Remove(data);
                    targetParent.Layers.Add(data);

                    // update source's parent
                    sourceItem.Parent = targetParent;
                    lock (_undoRedoHelper)
                        _undoRedoHelper.SaveState();
                }
            }
        }

        private MRecipeBaseNode GetNearestContainer(object element)
        {
            // Walk up the element tree to the nearest tree view item.
            var container = element as MRecipeBaseNode;
            while ((container == null) && (element != null))
            {
                element = VisualTreeHelper.GetParent(element as DependencyObject);
                container = (element as TreeViewItem)?.DataContext as MRecipeBaseNode;
            }
            return container;
        }

        #endregion

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
