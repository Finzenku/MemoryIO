using System.Runtime.InteropServices;

namespace MemoryIO.Monitors
{
    /// <summary>
    /// Monitors a specific region of memory and captures the changes as the specified type T.
    /// </summary>
    /// <typeparam name="T">The type of data to monitor.</typeparam>
    public class MemoryMonitor<T> : IMemoryMonitor<MemoryChangedEventArgs<T>>, IDisposable where T : unmanaged
    {
        /// <summary>
        /// Event that is raised when the monitored memory region changes.
        /// </summary>
        public event EventHandler<MemoryChangedEventArgs<T>>? MemoryChanged;

        private IntPtr address;
        private IMemoryIO memoryManager;
        private int dataSize;
        private byte[] previousData;
        private int pollingRate;
        private bool isMonitoring;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryMonitor{T}"/> class.
        /// </summary>
        /// <param name="memoryManager">The <see cref="IMemoryIO"/> used to read memory.</param>
        /// <param name="address">The starting address of the monitored memory region.</param>
        /// <param name="pollingRateInMilliseconds">The interval between memory checks in milliseconds.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="memoryManager"/> is null.</exception>
        public MemoryMonitor(IMemoryIO memoryManager, IntPtr address, int pollingRateInMilliseconds = 10)
        {
            if (memoryManager is null)
                throw new ArgumentException("MemoryManager must not be null.", nameof(memoryManager));

            this.address = address;
            this.memoryManager = memoryManager;
            dataSize = Marshal.SizeOf<T>();
            previousData = new byte[dataSize];
            pollingRate = pollingRateInMilliseconds;
            isMonitoring = false;
        }

        void OnMemoryChanged(IntPtr address, T value)
        {
            MemoryChangedEventArgs<T> args = new MemoryChangedEventArgs<T>(address, value);
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
                byte[] currentData = memoryManager.ReadData(address, dataSize);
                if (!previousData.SequenceEqual(currentData))
                {
                    OnMemoryChanged(address, MemoryMarshal.Cast<byte, T>(currentData)[0]);
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

                    byte[] currentData = memoryManager.ReadData(address, dataSize);
                    if (!previousData.SequenceEqual(currentData))
                    {
                        OnMemoryChanged(address, MemoryMarshal.Cast<byte, T>(currentData)[0]);
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
