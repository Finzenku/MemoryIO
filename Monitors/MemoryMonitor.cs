namespace MemoryIO.Monitors
{
    /// <summary>
    /// Monitors a specific region of memory and captures the changes as a byte array.
    /// </summary>
    public class MemoryMonitor : IMemoryMonitor<MemoryRegionChangedEventArgs>, IDisposable
    {
        /// <summary>
        /// Event that is raised when the monitored memory region changes.
        /// </summary>
        public event EventHandler<MemoryRegionChangedEventArgs>? MemoryChanged;

        private IntPtr address;
        private IMemoryIO memoryManager;
        private int regionSize;
        private byte[] previousData;
        private int pollingRate;
        private bool isMonitoring;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryMonitorFromPointer"/> class.
        /// </summary>
        /// <param name="memoryManager">The <see cref="IMemoryIO"/> used to read memory.</param>
        /// <param name="pointerAddress">The pointer address to the start of the monitored memory region.</param>
        /// <param name="regionSize">The size of the monitored memory region in bytes.</param>
        /// <param name="pointerOffset">The offset from the pointer's value to the start of the monitored memory region.</param>
        /// <param name="pollingRateInMilliseconds">The interval between memory checks in milliseconds.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="memoryManager"/> is null.</exception>
        public MemoryMonitor(IMemoryIO memoryManager, IntPtr address, int regionSize, int pollingRateInMilliseconds = 10)
        {
            if (memoryManager is null)
                throw new ArgumentException("MemoryManager must not be null.", nameof(memoryManager));

            this.address = address;
            this.memoryManager = memoryManager;
            this.regionSize = regionSize;
            previousData = new byte[regionSize];
            pollingRate = pollingRateInMilliseconds;
            isMonitoring = false;
        }

        void OnMemoryChanged(IntPtr address, byte[] value)
        {
            MemoryRegionChangedEventArgs args = new MemoryRegionChangedEventArgs(address, value);
            MemoryChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Starts monitoring the memory region for changes synchronously.
        /// </summary>
        public void StartMonitoring()
        {
            if (isMonitoring || memoryManager is null) return;
            isMonitoring = true;

            while (address != IntPtr.Zero && isMonitoring)
            {
                byte[] currentData = memoryManager.ReadData(address, regionSize);
                if (!previousData.SequenceEqual(currentData))
                {
                    OnMemoryChanged(address, currentData);
                    previousData = currentData;
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
                while (address != IntPtr.Zero && isMonitoring)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    byte[] currentData = memoryManager.ReadData(address, regionSize);
                    if (!previousData.SequenceEqual(currentData))
                    {
                        OnMemoryChanged(address, currentData);
                        previousData = currentData;
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
        /// Sets a new address for the monitored memory region.
        /// </summary>
        /// <param name="newAddress">The new address to set.</param>
        public void SetNewAddress(IntPtr newAddress) => address = newAddress;

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
