using System.Diagnostics;
using System.Text;
using MemoryManagement.Managers;

namespace MemoryManagement
{
    public class MemoryManager : IMemoryManager
    {
        private IMemoryManager m;

        public MemoryManager(Process process)
        {
            if (System.Environment.OSVersion.Platform == PlatformID.Unix ||
                System.Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                m = new LinuxMemoryManager(process);
            }
            else
            {
                m = new WindowsMemoryManager(process);
            }
        }

        public T Read<T>(IntPtr address) => m.Read<T>(address);
        public byte[] ReadData(IntPtr address, int dataLength) => m.ReadData(address, dataLength);
        public T[] ReadArray<T>(IntPtr address, int arrayLength) => m.ReadArray<T>(address, arrayLength);

        public string ReadString(IntPtr address, Encoding encoding, int maxLength = 512) => m.ReadString(address, encoding, maxLength);
        public string[] ReadStringArray(IntPtr address, Encoding encoding, int maxLength = 512) => m.ReadStringArray(address, encoding, maxLength);

        public void Write<T>(IntPtr address, T value) => m.Write<T>(address, value);
        public void WriteArray<T>(IntPtr address, T[] value) => m.WriteArray(address, value);

        public void WriteString(IntPtr address, string text, Encoding encoding) => m.WriteString(address, text, encoding);
        public void WriteStringArray(IntPtr address, string[] text, Encoding encoding) => m.WriteStringArray(address, text, encoding);
    }
}
