using System;
using System.Text;

namespace MemoryManagement.Managers
{
    public interface IMemoryManager
    {
        public string ReadString(IntPtr address, Encoding encoding, int maxLength = 512);
        public string[] ReadStringArray(IntPtr address, Encoding encoding, int maxLength = 512);
        public T Read<T>(IntPtr address);
        public T[] ReadArray<T>(IntPtr address, int arrayLength);
        public void WriteString(IntPtr address, string text, Encoding encoding);
        public void Write<T>(IntPtr address, T value);
        public void WriteArray<T>(IntPtr address, T[] value);
        public void WriteStringArray(IntPtr address, string[] text, Encoding encoding);
    }
}