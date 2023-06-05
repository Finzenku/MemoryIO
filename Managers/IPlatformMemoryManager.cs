using System.Diagnostics;

namespace MemoryManagement.Managers
{
    public interface IPlatformMemoryManager : IMemoryManager
    {
        public Process Process { get; }
        public PlatformID Platform { get; }
        public bool Is64BitProcess { get; }
    }
}
