using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MemoryIO
{
    public class WindowsMemoryIO : BaseMemoryIO, IProcessMemoryIO
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

        public WindowsMemoryIO(Process process)
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

        public override byte[] ReadData(IntPtr address, int dataLength)
        {
            byte[] buffer = new byte[dataLength];
            ReadProcessMemory(Process.Handle, address, buffer, dataLength, out _);
            return buffer;
        }

        public override void WriteData(IntPtr address, byte[] data) => WriteProcessMemory(Process.Handle, address, data, data.Length, out _);
    }
}
