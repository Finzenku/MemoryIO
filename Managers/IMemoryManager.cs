using System.Text;

namespace MemoryManagement.Managers
{
    public interface IMemoryManager
    {
        public byte[] ReadData(IntPtr address, int dataLength);
        public T Read<T>(IntPtr address);
        public T[] ReadArray<T>(IntPtr address, int arrayLength);
        public string ReadString(IntPtr address, Encoding encoding, int maxLength = 512);
        public string[] ReadStringArray(IntPtr address, Encoding encoding, int maxLength = 512);
        public void WriteData(IntPtr address, byte[] data);
        public void Write<T>(IntPtr address, T value);
        public void WriteArray<T>(IntPtr address, T[] value);
        public void WriteString(IntPtr address, string text, Encoding encoding);
        public void WriteStringArray(IntPtr address, string[] text, Encoding encoding);
    }
}