using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using MemoryManagement.Internals;

namespace MemoryManagement.Managers
{
    internal class LinuxMemoryManager : IMemoryManager
    {
        private Process p;
        
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
            p = process;          
        }

        public string ReadString(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            
            IntPtr value = ReadMemory(p.Id, address,maxLength);
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
            IntPtr value = ReadMemory(p.Id, address, maxLength);
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
            IntPtr value = ReadMemory(p.Id, address,MarshalType<T>.Size);           
            T temp = Marshal.PtrToStructure<T>(value)!;

            freeingMemory(value);
            return temp;
        }

        public byte[] ReadData(IntPtr address, int dataLength)
        {
            IntPtr value = ReadMemory(p.Id, address, dataLength);
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
            WriteMemory(p.Id,address,value,value.Length);
        }
        public void WriteStringArray(IntPtr address, string[] text, Encoding encoding)
        {
            List<byte> value = new();
            foreach (string s in text)
                value.AddRange(encoding.GetBytes(s+'\0'));
            WriteMemory(p.Id, address, value.ToArray(), value.Count);
        }

        public void Write<T>(IntPtr address, T value)
        {
            byte[] byteValue = MarshalType<T>.ObjectToByteArray(value);
            WriteMemory(p.Id,address,byteValue,byteValue.Length);
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