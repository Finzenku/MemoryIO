using System.Diagnostics;

namespace MemoryManagement.Managers
{
    public interface IProcessMemoryManager : IMemoryManager
    {
        public Process Process { get; }
        public bool Is64BitProcess { get; }
    }
}
