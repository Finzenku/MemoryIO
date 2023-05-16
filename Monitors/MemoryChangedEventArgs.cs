namespace MemoryManagement.Monitors
{
    public class MemoryChangedEventArgs<T> : EventArgs
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
