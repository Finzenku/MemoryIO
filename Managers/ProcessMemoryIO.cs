using System.Diagnostics;
using System.Text;
using MemoryIO.Factories;

namespace MemoryIO.Managers
{
    public class ProcessMemoryIO : IProcessMemoryIO
    {
        private IProcessMemoryIO m;

        public Process Process => m.Process;

        public PlatformID Platform => m.Platform;

        public bool Is64BitProcess => m.Is64BitProcess;

        public ProcessMemoryIO(Process process)
        {
            m = MemoryIOFactory.CreateEnvironmentSpecificMemoryIO(process);
        }

        public byte[] ReadData(IntPtr address, int dataLength) => m.ReadData(address, dataLength);

        public T Read<T>(IntPtr address) where T : unmanaged => m.Read<T>(address);
        public T[] ReadArray<T>(IntPtr address, int arrayLength) where T : unmanaged => m.ReadArray<T>(address, arrayLength);

        public string ReadString(IntPtr address, Encoding encoding, int maxLength = 512) => m.ReadString(address, encoding, maxLength);
        public string[] ReadStringArray(IntPtr address, Encoding encoding, int maxLength = 512) => m.ReadStringArray(address, encoding, maxLength);

        public void WriteData(IntPtr address, byte[] data) => m.WriteData(address, data);

        public void Write<T>(IntPtr address, T value) where T : unmanaged => m.Write<T>(address, value);
        public void WriteArray<T>(IntPtr address, T[] value) where T : unmanaged => m.WriteArray(address, value);

        public void WriteString(IntPtr address, string text, Encoding encoding) => m.WriteString(address, text, encoding);
        public void WriteStringArray(IntPtr address, string[] text, Encoding encoding) => m.WriteStringArray(address, text, encoding);
    }
}
