using System.Diagnostics;
using MemoryManagement.Managers;

namespace MemoryManagement.Factories
{
    public static class MemoryManagerFactory
    {
        public static IPlatformMemoryManager CreatePlatformMemoryManager(Process process)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix ||
                Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                return new LinuxMemoryManager(process);
            }
            else
            {
                return new WindowsMemoryManager(process);
            }
        }
    }
}
