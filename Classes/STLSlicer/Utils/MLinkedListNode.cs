namespace STLSlicer.Utils
{
    public class MLinkedListNode<T>
    {
        public T Value { get; set; }

        public MLinkedListNode<T> Next { get; set; }
        public MLinkedListNode<T> Previous { get; set; }

        public MLinkedListNode()
        {
            Next = null;
            Previous = null;
        }

        public MLinkedListNode(T value)
            : this()
        {
            Value = value;
        }
    }
}
