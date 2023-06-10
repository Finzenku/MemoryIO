namespace MemoryIO.Monitors
{
    public class MemoryChangedEventArgs<T> : EventArgs where T : unmanaged
    {
        public IntPtr Address { get; }
        public T Value { get; }

        public MemoryChangedEventArgs(IntPtr address, T value)
        {
            Address = address;
            Value = value;
        }
    }
}
