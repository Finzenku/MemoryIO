namespace MemoryIO.Monitors
{
    public class MemoryRegionChangedEventArgs : EventArgs
    {
        public IntPtr Address { get; }
        public byte[] Value { get; }

        public MemoryRegionChangedEventArgs(IntPtr address, byte[] value)
        {
            Address = address;
            Value = value;
        }
    }
}
