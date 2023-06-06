using MemoryManagement.Internals;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MemoryManagement.Managers
{
    internal class WindowsMemoryManager : IPlatformMemoryManager
    {
        public Process Process { get; }

        private bool is64BitProcess;
        public bool Is64BitProcess => is64BitProcess;
        public PlatformID Platform => PlatformID.Win32NT;

        //Import ReadProcessMemory
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        [Out] byte[] lpBuffer,
        int dwSize,
        out IntPtr lpNumberOfBytesRead);

        //Import WriteProcessMemory
        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        byte[] lpBuffer,
        int dwSize,
        out IntPtr lpNumberOfBytesWritten);

        //Import IsWow64Process
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool IsWow64Process(IntPtr processHandle, out bool isWow64Process);

        public WindowsMemoryManager(Process process)
        {
            Process = process;
            if (Environment.Is64BitOperatingSystem && IsWow64Process(process.Handle, out bool isWow64))
            {
                is64BitProcess = !isWow64;
            }
            else
            {
                is64BitProcess = false;
            }
        }


        public byte[] ReadData(IntPtr address, int dataLength)
        {
            byte[] buffer = new byte[dataLength];

            ReadProcessMemory(Process.Handle, address, buffer, dataLength, out _);
            return buffer[..dataLength];
        }
        public T Read<T>(IntPtr address)
        {
            int dataSize = MarshalType<T>.Size;
            byte[] buffer = new byte[dataSize];

            ReadProcessMemory(Process.Handle, address, buffer, dataSize, out _);
            return MarshalType<T>.ByteArrayToObject(buffer);
        }

        public T[] ReadArray<T>(IntPtr address, int arrayLength)
        {
            int dataSize = MarshalType<T>.Size;
            byte[] buffer = new byte[dataSize * arrayLength];

            T[] tArray = new T[arrayLength];
            ReadProcessMemory(Process.Handle, address, buffer, dataSize * arrayLength, out _);
            if (MarshalType<T>.TypeCode == TypeCode.Byte)
                return (T[])(object)buffer[..arrayLength];
            for (int i = 0; i < arrayLength; i++)
                tArray[i] = MarshalType<T>.ByteArrayToObject(buffer[(dataSize*i)..(dataSize*(i+1))]);
            return tArray;
        }

        public string ReadString(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            byte[] buffer = new byte[maxLength];

            ReadProcessMemory(Process.Handle, address, buffer, maxLength, out _);
            string encoded = encoding.GetString(buffer);
            int endOfStringPos = encoded.IndexOf('\0');
            return endOfStringPos == -1 ? encoded : encoded.Substring(0, endOfStringPos);
        }

        public string[] ReadStringArray(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            byte[] buffer = new byte[maxLength];

            ReadProcessMemory(Process.Handle, address, buffer, maxLength, out _);
            string encoded = encoding.GetString(buffer);
            string[] split = encoded.Split('\0');
            for (int i = 0; i < split.Length; i++)
                if (split[i] == string.Empty)
                    return split[0..i];
            return split;
        }

        public void WriteData(IntPtr address, byte[] data) => WriteProcessMemory(Process.Handle, address, data, data.Length, out _);

        public void Write<T>(IntPtr address, T value)
        {
            byte[] data = MarshalType<T>.ObjectToByteArray(value);
            WriteProcessMemory(Process.Handle, address, data, data.Length, out _);
        }

        public void WriteArray<T>(IntPtr address, T[] value)
        {
            if (value is byte[] byteArray)
            {
                WriteData(address, byteArray);
                return;
            }
            int typeSize = MarshalType<T>.Size;
            byte[] data = new byte[value.Length*typeSize];
            for (int i = 0; i < value.Length; i++)
            {
                Buffer.BlockCopy(MarshalType<T>.ObjectToByteArray(value[i]),0,data,i*typeSize,typeSize);
            }
            WriteProcessMemory(Process.Handle, address, data, data.Length, out _);
        }

        public void WriteString(IntPtr address, string text, Encoding encoding)
        {
            byte[] data = encoding.GetBytes(text+'\0');
            WriteProcessMemory(Process.Handle, address, data, data.Length, out _);
        }
        public void WriteStringArray(IntPtr address, string[] text, Encoding encoding)
        {
            int totalLength = 0;
            foreach (string s in text)
                totalLength += encoding.GetByteCount(s) + 1; // +1 for null termination

            byte[] buffer = new byte[totalLength];
            int offset = 0;

            foreach (string s in text)
            {
                int byteCount = encoding.GetBytes(s, buffer.AsSpan(offset));
                buffer[offset + byteCount] = 0; // Null termination
                offset += byteCount + 1; // Move the offset to the next string
            }

            WriteProcessMemory(Process.Handle, address, buffer, buffer.Length, out _);
        }
    }
}
