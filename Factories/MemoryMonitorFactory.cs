using MemoryIO.Monitors;

namespace MemoryIO.Factories
{
    public class MemoryMonitorFactory
    {
        private readonly IProcessMemoryIO memoryManager;
        private readonly int pollingRate;

        public MemoryMonitorFactory(IProcessMemoryIO memoryManager, int pollingRateInMilliseconds = 10)
        {
            this.memoryManager = memoryManager;
            pollingRate = pollingRateInMilliseconds;
        }

        // Region Monitors
        public IMemoryMonitor<MemoryRegionChangedEventArgs>? GetRegionMonitor(IntPtr address, int regionSize)
        {
            return new MemoryMonitor(memoryManager, address, regionSize, pollingRate);
        }
        public IMemoryMonitor<MemoryRegionChangedEventArgs>? GetRegionMonitorFromPointer(IntPtr pointerAddress, int regionSize, int pointerOffset = 0)
        {
            return new MemoryMonitorFromPointer(memoryManager, pointerAddress, regionSize, pointerOffset, pollingRate);
        }

        // T Monitors
        public IMemoryMonitor<MemoryChangedEventArgs<T>> GetMonitor<T>(IntPtr address) where T : unmanaged
        {
            return new MemoryMonitor<T>(memoryManager, address, pollingRate);
        }
        public IMemoryMonitor<MemoryChangedEventArgs<T>> GetMonitorFromPointer<T>(IntPtr pointerAddress, int pointerOffset = 0) where T : unmanaged
        {
            return new MemoryMonitorFromPointer<T>(memoryManager, pointerAddress, pointerOffset, pollingRate);
        }


        // T Array Monitors
        public IMemoryMonitor<MemoryArrayChangedEventArgs<T>>? GetArrayMonitor<T>(IntPtr address, int arrayLength) where T : unmanaged
        {
            return new MemoryMonitorTArray<T>(memoryManager, address, arrayLength, pollingRate);
        }
        public IMemoryMonitor<MemoryArrayChangedEventArgs<T>>? GetArrayMonitorFromPointer<T>(IntPtr address, int arrayLength, int pointerOffset = 0) where T : unmanaged
        {
            return new MemoryMonitorTArrayFromPointer<T>(memoryManager, address, arrayLength, pointerOffset, pollingRate);
        }
    }
}
