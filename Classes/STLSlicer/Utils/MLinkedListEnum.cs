using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace STLSlicer.Utils
{
    public class MLinkedListEnum<T> : IEnumerator<T>, IEnumerator
    {
        private MLinkedList<T> _linkedList;

        public T Current
        {
            get
            {
                try
                {
                    return CurrentNode.Value;
                }
                catch(NullReferenceException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        object IEnumerator.Current => Current;

        public MLinkedListNode<T> CurrentNode { get; private set; }


        public MLinkedListEnum(MLinkedList<T> mLinkedList)
        {
            _linkedList = mLinkedList;
            Reset();
        }

        public bool MoveNext()
        {
            CurrentNode = (CurrentNode == null) ? _linkedList?.First : CurrentNode.Next;
            return (CurrentNode != null);
        }

        public void Reset()
        {
            CurrentNode = null;
        }

        public void Dispose()
        {
        }
    }
}
