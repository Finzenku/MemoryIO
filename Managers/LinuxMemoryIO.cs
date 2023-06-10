using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MemoryIO.Managers
{
    public class LinuxMemoryIO : IProcessMemoryIO
    {
        public Process Process { get; }

        private bool is64BitProcess;
        public bool Is64BitProcess => is64BitProcess;
        public PlatformID Platform => PlatformID.Unix;

        [DllImport("libc")]
        private static extern unsafe int process_vm_readv(int pid, iovec* local_iov, ulong liovcnt, iovec* remote_iov, ulong riovcnt, ulong flags);
        [DllImport("libc")]
        private static extern unsafe int process_vm_writev(int pid, iovec* local_iov, ulong iiovcnt, iovec* remote_iov, ulong riovcnt, ulong flags);
        
        [DllImport("libc")]
        public static extern uint getuid ();
        
        public LinuxMemoryIO(Process process)
        {
            Process = process;
            try
            {
                is64BitProcess = CheckIfProcess64Bit(process);
            }
            catch (Exception)
            {
                is64BitProcess = Environment.Is64BitProcess;
            }
        }

        private static bool CheckIfProcess64Bit(Process process)
        {
            try
            {
                string? filePath = process.MainModule?.FileName;
                if (filePath is not null)
                {
                    // This was unauthorized in all of my testing, even with sudo, but ChatGPT and Bard assure me this is correct
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] buffer = new byte[5];
                        if (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
                        {
                            // The 5th byte of the ELF header indicate the architecture
                            // 0x01: 32-bit, 0x02: 64-bit
                            return buffer[4] == 0x02;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Unable to read ELF header from target process {0}: {1}", process.Id, e.Message));
            }

            // Default to 64bit because it's 2023 and 64bit should be the majority at this point
            return true;
        }

        #region Read
        private unsafe bool Read(IntPtr address, void* dataPointer, int dataLength)
        {
            var localIo = new iovec
            {
                iov_base = dataPointer,
                iov_len = dataLength
            };

            var remoteIo = new iovec
            {
                iov_base = address.ToPointer(),
                iov_len = dataLength
            };

            return process_vm_readv(Process.Id, &localIo, 1, &remoteIo, 1, 0) != -1;
        }

        public byte[] ReadData(IntPtr address, int dataLength)
        {
            byte[] dataBuffer = new byte[dataLength];
            unsafe
            {
                fixed (byte* bufferPtr = dataBuffer)
                {
                    Read(address, bufferPtr, dataLength);
                }
            }
            return dataBuffer;
        }

        public T Read<T>(IntPtr address)
        {
            T result = default!;
            int tSize = Marshal.SizeOf<T>();
            unsafe
            {
                IntPtr pDataPointer = Marshal.AllocHGlobal(tSize);
                if (Read(address, pDataPointer.ToPointer(), tSize))
                {
                    result = Marshal.PtrToStructure<T>(pDataPointer)!;
                }
            }
            return result;
        }
        public T[] ReadArray<T>(IntPtr address, int arrayLength)
        {
            int tSize = Marshal.SizeOf<T>();
            int dataLength = tSize * arrayLength;
            T[] array = new T[arrayLength];

            IntPtr pDataPointer = Marshal.AllocHGlobal(dataLength);
            try
            {
                unsafe
                {
                    if (Read(address, pDataPointer.ToPointer(), dataLength))
                    {
                        for (int i = 0; i < arrayLength; i++)
                        {
                            IntPtr tAddress = IntPtr.Add(pDataPointer, i * tSize);
                            array[i] = Marshal.PtrToStructure<T>(tAddress)!;
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pDataPointer);
            }

            return array;
        }

        public string ReadString(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            byte[] byteValue = ReadData(address, maxLength);
            string encoded = encoding.GetString(byteValue);

            // Search the end of the string
            var endOfStringPosition = encoded.IndexOf('\0');

            // Crop the string with this end if found, return the string otherwise
            return endOfStringPosition == -1 ? encoded : encoded.Substring(0, endOfStringPosition);            
        }
        public string[] ReadStringArray(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            byte[] byteValue = ReadData(address, maxLength);
            string encoded = encoding.GetString(byteValue);

            string[] split = encoded.Split('\0');
            for (int i = 0; i < split.Length; i++)
                if (split[i] == string.Empty)
                    return split[0..i];
            return split;
        }
        #endregion

        #region Write
        private unsafe bool Write(IntPtr address, void* dataPointer, int dataLength)
        {
            var localIo = new iovec
            {
                iov_base = dataPointer,
                iov_len = dataLength
            };

            var remoteIo = new iovec
            {
                iov_base = address.ToPointer(),
                iov_len = dataLength
            };

            return process_vm_writev(Process.Id, &localIo, 1, &remoteIo, 1, 0) != -1;
        }

        public void WriteData(IntPtr address, byte[] data)
        {
            if (data.Length == 0)
                return;

            int dataLength = 4 * data.Length;

            unsafe
            {
                fixed (byte* valuePtr = data)
                {
                    Write(address, valuePtr, dataLength);
                }
            }
        }

        public void Write<T>(IntPtr address, T value)
        {
            if (value is null)
                return;
            int tSize = Marshal.SizeOf<T>();
            IntPtr dataPointer = Marshal.AllocHGlobal(tSize);
            try
            {
                Marshal.StructureToPtr(value, dataPointer, false);
                unsafe
                {
                    Write(address, dataPointer.ToPointer(), tSize);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(dataPointer);
            }
        }
        public void WriteArray<T>(IntPtr address, T[] value)
        {
            if (value is null || value.Length == 0)
                return;

            if (value is byte[] byteArray)
            {
                WriteData(address, byteArray);
                return;
            }

            int dataLength = Marshal.SizeOf<T>() * value.Length;
            IntPtr dataPointer = Marshal.AllocHGlobal(dataLength);
            try
            {
                byte[] buffer = new byte[dataLength];
                Buffer.BlockCopy(value, 0, buffer, 0, dataLength);

                unsafe
                {
                    fixed (byte* bufferPtr = buffer)
                    {
                        Write(address, bufferPtr, dataLength);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(dataPointer);
            }
        }

        public void WriteString(IntPtr address, string text, Encoding encoding)
        {
            byte[] buffer = encoding.GetBytes(text+'\0');
            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                {
                    Write(address, bufferPtr, buffer.Length);
                }
            }
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

            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                {
                    Write(address, bufferPtr, buffer.Length);
                }
            }
        }
        #endregion

        public static uint CheckPrivileges()
        {
            return getuid();
        }

        private unsafe struct iovec
        {
            public void* iov_base;
            public int iov_len;
        }
    }
}