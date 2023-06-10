namespace MemoryIO.Monitors
{
    public class MemoryArrayChangedEventArgs<T> : EventArgs where T : unmanaged
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
