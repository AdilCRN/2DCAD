using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace STLSlicer.Utils
{
    public class MLinkedList<T> : ICollection<T>
    {
        public long Count { get; private set; }
        public MLinkedListNode<T> First { get; private set; }
        public MLinkedListNode<T> Last { get; private set; }

        int ICollection<T>.Count => (int)Count;
        public bool IsReadOnly => false;

        public MLinkedList()
        {
            First = null;
            Last = null;
            Count = 0;
        }

        public MLinkedList(T value)
            : this()
        {
            Add(value);
        }

        public MLinkedList(params T[] values)
            : this()
        {
            foreach (var value in values)
                AddLast(value);
        }

        public void AddLast(T item)
        {
            if (Last == null)
            {
                Last = new MLinkedListNode<T>(item);
                First = Last;
            }
            else
            {
                Last.Next = new MLinkedListNode<T>(item)
                {
                    Previous = Last
                };

                Last = Last.Next;
            }

            Count += 1;
        }

        public void AddFirst(T item)
        {
            if (First == null)
            {
                First = new MLinkedListNode<T>(item);
                Last = First;
            }
            else
            {
                First.Previous = new MLinkedListNode<T>(item)
                {
                    Next = First
                };

                First = First.Previous;
            }

            Count += 1;
        }

        public void AddFirst(MLinkedList<T> mLinkedList)
        {
            mLinkedList.Last.Next = First;
            First = mLinkedList.First;

            Count += mLinkedList.Count;
        }

        public void AddLast(MLinkedList<T> mLinkedList)
        {
            mLinkedList.First.Previous = Last;
            Last.Next = mLinkedList.First;
            Last = mLinkedList.Last;

            Count += mLinkedList.Count;
        }

        public void Add(T item)
        {
            AddLast(item);
        }

        public void Clear()
        {
            First = null;
            Last = null;
            Count = 0;
        }

        public bool Contains(T item)
        {
            var node = First;

            while (node != null)
            {
                if (Comparer<T>.Default.Compare(item, node.Value) == 0)
                    return true;

                node = node.Next;
            }
            
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            int index = 0;
            var node = First;

            while (node != null)
            {
                array[arrayIndex + index++] = node.Value;
                node = node.Next;
            }

        }

        public bool Remove(T item)
        {
            var node = First;

            while (node != null)
            {
                if (Comparer<T>.Default.Compare(item, node.Value) == 0)
                {
                    Count--;
                    (node.Previous.Next, node.Next.Previous) = (node.Next, node.Previous);
                    return true;
                }

                node = node.Next;
            }

            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new MLinkedListEnum<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new MLinkedListEnum<T>(this);
        }
    }
}
