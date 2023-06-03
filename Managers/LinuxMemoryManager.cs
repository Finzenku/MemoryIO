using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using MemoryManagement.Internals;

namespace MemoryManagement.Managers
{
    internal class LinuxMemoryManager : IProcessMemoryManager
    {
        public Process Process { get; }

        private bool is64BitProcess;
        public bool Is64BitProcess => is64BitProcess;

        [DllImport(@"Memory/libSimpleLinuxMemoryAccess.so")]
        public static extern IntPtr ReadMemory(int pid, IntPtr address, int maxLenght);
        
        [DllImport(@"Memory/libSimpleLinuxMemoryAccess.so")]
        public static extern void WriteMemory(int pid, IntPtr address, byte[] value, int valueSize);
        
        [DllImport(@"Memory/libSimpleLinuxMemoryAccess.so")]
        public static extern IntPtr freeingMemory(IntPtr address);
        
        [DllImport ("libc")]
        public static extern uint getuid ();
        
        public LinuxMemoryManager(Process process)
        {
            Process = process;
            try
            {
                string? processPath = Process.MainModule?.FileName;

                if (processPath is null)
                    throw new Exception("Process MainModule did not return a valid file path");

                using (var stream = new FileStream(processPath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[4];
                    if (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
                    {
                        // The first 4 bytes of the ELF header indicate the architecture
                        // 0x01: 32-bit, 0x02: 64-bit
                        is64BitProcess = buffer[4] == 0x02;
                    }
                }
            }
            catch (Exception)
            {
                // Handle any exceptions that might occur while reading the file
                // Or don't
            }
        }

        public string ReadString(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            
            IntPtr value = ReadMemory(Process.Id, address,maxLength);
            byte[] byteValue = new byte[maxLength];
            Marshal.Copy(value, byteValue, 0, maxLength);
            string encoded = encoding.GetString(byteValue);

            freeingMemory(value);

            // Search the end of the string
            var endOfStringPosition = encoded.IndexOf('\0');

            // Crop the string with this end if found, return the string otherwise
            return endOfStringPosition == -1 ? encoded : encoded.Substring(0, endOfStringPosition);
            //return encoded;
            
        }

        public string[] ReadStringArray(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            IntPtr value = ReadMemory(Process.Id, address, maxLength);
            byte[] byteValue = new byte[maxLength];
            Marshal.Copy(value, byteValue, 0, maxLength);
            string encoded = encoding.GetString(byteValue);

            freeingMemory(value);

            string[] split = encoded.Split('\0');
            for (int i = 0; i < split.Length; i++)
                if (split[i] == string.Empty)
                    return split[0..i];
            return split;
        }

        public T Read<T>(IntPtr address)
        {
            IntPtr value = ReadMemory(Process.Id, address,MarshalType<T>.Size);           
            T temp = Marshal.PtrToStructure<T>(value)!;

            freeingMemory(value);
            return temp;
        }

        public byte[] ReadData(IntPtr address, int dataLength)
        {
            IntPtr value = ReadMemory(Process.Id, address, dataLength);
            byte[] dataBuffer = new byte[dataLength];
            Marshal.Copy(value, dataBuffer, 0, dataBuffer.Length);

            freeingMemory(value);
            return dataBuffer;
        }

        //Basic implementation, leave for Zackmon someday :)
        public T[] ReadArray<T>(IntPtr address, int arrayLength)
        {
            T[] array = new T[arrayLength];
            for (int i = 0; i < array.Length; i++)
                array[i] = Read<T>(address + i * MarshalType<T>.Size);
            return array;
        }

        public void WriteString(IntPtr address, string text, Encoding encoding)
        {
            byte[] value = encoding.GetBytes(text+ '\0');
            WriteMemory(Process.Id,address,value,value.Length);
        }
        public void WriteStringArray(IntPtr address, string[] text, Encoding encoding)
        {
            List<byte> value = new();
            foreach (string s in text)
                value.AddRange(encoding.GetBytes(s+'\0'));
            WriteMemory(Process.Id, address, value.ToArray(), value.Count);
        }

        public void Write<T>(IntPtr address, T value)
        {
            byte[] byteValue = MarshalType<T>.ObjectToByteArray(value);
            WriteMemory(Process.Id,address,byteValue,byteValue.Length);
        }
        //Basic implementation, leave for Zackmon someday :)
        public void WriteArray<T>(IntPtr address, T[] value)
        {
            for (int i = 0; i < value.Length; i++)
                Write(address + i * MarshalType<T>.Size, value[i]);
        }


        public static uint checkPrivileges()
        {
            return getuid();
        }
    }
}