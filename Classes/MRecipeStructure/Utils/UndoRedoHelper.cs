using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MRecipeStructure.Classes.MRecipeStructure.Utils
{
    public class UndoRedoHelper<T> : IDisposable where T : ICloneable, INotifyPropertyChanged
    {
        private bool _paused;
        private T _reference;
        private List<T> _buffer;
        private List<INotifyPropertyChanged> _subscriptions;

        public int Count => Head + 1;
        public bool IsPaused => _paused;
        public int Head { get; set; } = -1;
        public int Capacity { get; set; } = 1;

        public UndoRedoHelper(ref T reference, int capacity=100)
        {
            _paused = false;
            Capacity = capacity;
            _reference = reference;
            _buffer = new List<T>(capacity);
            _subscriptions = new List<INotifyPropertyChanged>();

            // track changes to reference
            SaveStateOnPropertyChange(reference);
        }

        public void SaveStateOnPropertyChange(params INotifyPropertyChanged[] items)
        {
            foreach (var item in items)
            {
                item.PropertyChanged += Item_PropertyChanged;
                _subscriptions.Add(item);
            }
        }

        public void SaveStateOnPropertiesChange(IEnumerable<INotifyPropertyChanged> items)
        {
            foreach (var item in items)
            {
                item.PropertyChanged += Item_PropertyChanged;
                _subscriptions.Add(item);
            }
        }

        public void SaveState()
        {
            // discard redos
            _buffer.RemoveRange(Head + 1, _buffer.Count - Count);

            // maintain capacity
            if (_buffer.Count > Capacity)
                _buffer.RemoveAt(0);

            // track reference
            _buffer.Add((T)_reference.Clone());

            // update head indexs
            Head = _buffer.Count - 1;
        }

        public T Undo()
        {
            if (Head <= 0)
                return default(T);

            return _buffer[--Head];
        }

        public T Redo()
        {
            if (Head >= (_buffer.Count - 1))
                return default(T);

            return _buffer[++Head];
        }

        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!_paused)
                SaveState();
        }

        public void DisposeSubscriptions()
        {
            foreach (var sub in _subscriptions)
                if (sub != null)
                    sub.PropertyChanged -= Item_PropertyChanged;

            _subscriptions.Clear();
        }

        public void Dispose()
        {
            DisposeSubscriptions();
        }
    }
}
