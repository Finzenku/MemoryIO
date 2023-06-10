using System.Diagnostics;

namespace MemoryIO.Managers
{
    public interface IProcessMemoryIO : IMemoryIO
    {
        public Process Process { get; }
        public PlatformID Platform { get; }
        public bool Is64BitProcess { get; }
    }
}
