using MemoryManagement.Internals;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MemoryManagement.Managers
{
    internal class WindowsMemoryManager : IMemoryManager
    {
        private Process p;

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

        public WindowsMemoryManager(Process process)
        {
            p = process;
        }

        public T Read<T>(IntPtr address)
        {
            byte[] dataBuffer = new byte[MarshalType<T>.Size];
            ReadProcessMemory(p.Handle, address, dataBuffer, dataBuffer.Length, out IntPtr bytesRead);
            return MarshalType<T>.ByteArrayToObject(dataBuffer);
        }

        public T[] ReadArray<T>(IntPtr address, int arrayLength)
        {
            int typeSize = MarshalType<T>.Size;
            byte[] dataBuffer = new byte[typeSize * arrayLength];
            T[] tArray = new T[arrayLength];
            ReadProcessMemory(p.Handle, address, dataBuffer, dataBuffer.Length, out IntPtr bytesRead);
            if (MarshalType<T>.TypeCode == TypeCode.Byte)
                return (T[])(object)dataBuffer;
            for (int i = 0; i < arrayLength; i++)
                tArray[i] = MarshalType<T>.ByteArrayToObject(dataBuffer[(typeSize*i)..(typeSize*(i+1))]);
            return tArray;
        }

        public string ReadString(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            byte[] dataBuffer = new byte[maxLength];
            ReadProcessMemory(p.Handle, address, dataBuffer, dataBuffer.Length, out IntPtr bytesRead);
            string encoded = encoding.GetString(dataBuffer);
            int endOfStringPos = encoded.IndexOf('\0');
            return endOfStringPos == -1 ? encoded : encoded.Substring(0, endOfStringPos);
        }

        public string[] ReadStringArray(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            byte[] dataBuffer = new byte[maxLength];
            ReadProcessMemory(p.Handle, address, dataBuffer, dataBuffer.Length, out IntPtr bytesRead);
            string encoded = encoding.GetString(dataBuffer);
            string[] split = encoded.Split('\0');
            for (int i = 0; i < split.Length; i++)
                if (split[i] == string.Empty)
                    return split[0..i];
            return split;
        }

        public void Write<T>(IntPtr address, T value)
        {
            byte[] data = MarshalType<T>.ObjectToByteArray(value);
            WriteProcessMemory(p.Handle, address, data, data.Length, out IntPtr intPtr);
        }

        public void WriteArray<T>(IntPtr address, T[] value)
        {
            int typeSize = MarshalType<T>.Size;
            byte[] data = new byte[value.Length*typeSize];
            for (int i = 0; i < value.Length; i++)
            {
                Array.Copy(MarshalType<T>.ObjectToByteArray(value[i]),0,data,i*typeSize,typeSize);
            }
            WriteProcessMemory(p.Handle, address, data, data.Length, out IntPtr intPtr);
        }

        public void WriteString(IntPtr address, string text, Encoding encoding)
        {
            byte[] data = encoding.GetBytes(text+'\0');
            WriteProcessMemory(p.Handle, address, data, data.Length, out IntPtr intPtr);
        }
        public void WriteStringArray(IntPtr address, string[] text, Encoding encoding)
        {
            List<byte> data = new();
            foreach(string s in text)
                data.AddRange(encoding.GetBytes(s+'\0'));
            WriteProcessMemory(p.Handle, address, data.ToArray(), data.Count, out IntPtr intPtr);
        }
    }
}
