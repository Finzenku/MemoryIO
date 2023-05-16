using MemoryManagement.Managers;

namespace MemoryManagement.Monitors
{
    /// <summary>
    /// Monitors a specific region of memory pointed to by a pointer and captures the changes as a byte array.
    /// </summary>
    public class MemoryMonitorFromPointer : IMemoryMonitor<MemoryChangedEventArgs<byte[]>>, IDisposable
    {
        /// <summary>
        /// Event that is raised when the monitored memory region changes.
        /// </summary>
        public event EventHandler<MemoryChangedEventArgs<byte[]>>? MemoryChanged;

        private IntPtr pointerAddress;
        private IMemoryManager memoryManager;
        private int pointerOffset;
        private int regionSize;
        private byte[] previousData;
        private int pollingRate;
        private bool isMonitoring;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryMonitorFromPointer"/> class.
        /// </summary>
        /// <param name="memoryManager">The <see cref="IMemoryManager"/> used to read memory.</param>
        /// <param name="pointerAddress">The address of the pointer pointing to the start of the memory region.</param>
        /// <param name="regionSize">The size of the memory region to monitor.</param>
        /// <param name="pointerOffset">The offset from the pointer value to the start of the memory region.</param>
        /// <param name="pollingRateInMilliseconds">The interval between memory checks in milliseconds.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="memoryManager"/> is null.</exception>
        public MemoryMonitorFromPointer(IMemoryManager memoryManager, IntPtr pointerAddress, int regionSize, int pointerOffset = 0, int pollingRateInMilliseconds = 10)
        {
            if (memoryManager is null)
                throw new ArgumentException("MemoryManager must not be null.", nameof(memoryManager));

            this.pointerAddress = pointerAddress;
            this.memoryManager = memoryManager;
            this.pointerOffset = pointerOffset;
            this.regionSize = regionSize;
            previousData = new byte[regionSize];
            pollingRate = pollingRateInMilliseconds;
            isMonitoring = false;
        }

        void OnMemoryChanged(IntPtr address, byte[] value)
        {
            MemoryChangedEventArgs<byte[]> args = new MemoryChangedEventArgs<byte[]>(address, value);
            MemoryChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Starts monitoring the memory region for changes synchronously.
        /// </summary>
        public void StartMonitoring()
        {
            if (isMonitoring || memoryManager is null) return;
            isMonitoring = true;

            while (pointerAddress != IntPtr.Zero && isMonitoring)
            {
                IntPtr currentPointerValue = memoryManager.Read<IntPtr>(pointerAddress);
                if (currentPointerValue != IntPtr.Zero)
                {
                    byte[] currentData = memoryManager.ReadData(currentPointerValue + pointerOffset, regionSize);
                    if (!previousData.SequenceEqual(currentData))
                    {
                        OnMemoryChanged(currentPointerValue + pointerOffset, currentData);
                        previousData = currentData;
                    }
                }
                Thread.Sleep(pollingRate);
            }
            StopMonitoring();
        }

        /// <summary>
        /// Starts monitoring the memory region for changes asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to stop monitoring.</param>
        public async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            if (isMonitoring) return;
            isMonitoring = true;

            try
            {
                while (pointerAddress != IntPtr.Zero && isMonitoring)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    IntPtr currentPointerValue = memoryManager.Read<IntPtr>(pointerAddress);
                    if (currentPointerValue != IntPtr.Zero)
                    {
                        byte[] currentData = memoryManager.ReadData(currentPointerValue + pointerOffset, regionSize);
                        if (!previousData.SequenceEqual(currentData))
                        {
                            OnMemoryChanged(currentPointerValue + pointerOffset, currentData);
                            previousData = currentData;
                        }
                    }
                    await Task.Delay(pollingRate, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {

            }
            finally
            {
                StopMonitoring();
            }

        }

        /// <summary>
        /// Stops monitoring the memory region.
        /// </summary>
        public void StopMonitoring()
        {
            isMonitoring = false;
        }

        /// <summary>
        /// Sets a new pointer address for the monitored memory region.
        /// </summary>
        /// <param name="newAddress">The new address to set.</param>
        public void SetNewAddress(IntPtr newAddress) => pointerAddress = newAddress;

        /// <summary>
        /// Disposes the memory monitor and stops monitoring the memory region.
        /// </summary>
        public void Dispose()
        {
            StopMonitoring();
            MemoryChanged = null;
        }
    }
}
