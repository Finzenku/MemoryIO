using System.Text;

namespace MemoryIO.Managers
{
    public interface IMemoryIO
    {
        public byte[] ReadData(IntPtr address, int dataLength);
        public T Read<T>(IntPtr address) where T : unmanaged;
        public T[] ReadArray<T>(IntPtr address, int arrayLength) where T : unmanaged;
        public string ReadString(IntPtr address, Encoding encoding, int maxLength = 512);
        public string[] ReadStringArray(IntPtr address, Encoding encoding, int maxLength = 512);
        public void WriteData(IntPtr address, byte[] data);
        public void Write<T>(IntPtr address, T value) where T : unmanaged;
        public void WriteArray<T>(IntPtr address, T[] value) where T : unmanaged;
        public void WriteString(IntPtr address, string text, Encoding encoding);
        public void WriteStringArray(IntPtr address, string[] text, Encoding encoding);
    }
}