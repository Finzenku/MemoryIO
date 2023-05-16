namespace MemoryManagement.Monitors
{
    public class MemoryArrayChangedEventArgs<T> : EventArgs
    {
        public IntPtr Address { get; }
        public T Value { get; }
        public int Index { get; }

        public MemoryArrayChangedEventArgs(IntPtr address, T value, int index)
        {
            Address = address;
            Value = value;
            Index = index;
        }
    }
}
