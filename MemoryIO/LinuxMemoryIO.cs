using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MemoryIO
{
    public class LinuxMemoryIO : BaseMemoryIO, IProcessMemoryIO
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

        public override byte[] ReadData(IntPtr address, int dataLength)
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

        public override void WriteData(IntPtr address, byte[] data)
        {
            if (data.Length == 0)
                return;

            unsafe
            {
                fixed (byte* valuePtr = data)
                {
                    Write(address, valuePtr, data.Length);
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