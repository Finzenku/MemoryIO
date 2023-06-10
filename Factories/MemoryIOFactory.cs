using System.Diagnostics;
using MemoryIO.Managers;

namespace MemoryIO.Factories
{
    public static class MemoryIOFactory
    {
        public static IProcessMemoryIO CreateEnvironmentSpecificMemoryIO(Process process)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix ||
                Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                return new LinuxMemoryIO(process);
            }
            else
            {
                return new WindowsMemoryIO(process);
            }
        }
    }
}
