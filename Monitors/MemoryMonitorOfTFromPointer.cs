using MemoryIO.Internals;

namespace MemoryIO.Monitors
{
    /// <summary>
    /// Monitors a specific region of memory pointed to by a pointer and captures the changes as the specified type T.
    /// </summary>
    /// <typeparam name="T">The type of the memory value.</typeparam>
    public class MemoryMonitorFromPointer<T> : IMemoryMonitor<MemoryChangedEventArgs<T>>, IDisposable where T : unmanaged
    {
        /// <summary>
        /// Event that is raised when the monitored memory location changes.
        /// </summary>
        public event EventHandler<MemoryChangedEventArgs<T>>? MemoryChanged;

        private IntPtr pointerAddress;
        private IProcessMemoryIO memoryManager;
        private int pointerOffset;
        private int dataSize;
        private byte[] previousData;
        private int pollingRate;
        private bool isMonitoring;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryMonitorFromPointer{T}"/> class.
        /// </summary>
        /// <param name="memoryManager">The <see cref="IProcessMemoryIO"/> used to read memory.</param>
        /// <param name="pointerAddress">The address of the pointer that points to the monitored memory location.</param>
        /// <param name="pointerOffset">The offset from the pointer value to the monitored memory location.</param>
        /// <param name="pollingRateInMilliseconds">The interval between memory checks in milliseconds.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="memoryManager"/> is null.</exception>
        public MemoryMonitorFromPointer(IProcessMemoryIO memoryManager, IntPtr pointerAddress, int pointerOffset = 0, int pollingRateInMilliseconds = 10)
        {
            if (memoryManager is null)
                throw new ArgumentException("MemoryManager must not be null.", nameof(memoryManager));

            this.pointerAddress = pointerAddress;
            this.memoryManager = memoryManager;
            this.pointerOffset = pointerOffset;
            dataSize = MarshalType<T>.Size;
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

            while (pointerAddress != IntPtr.Zero && isMonitoring)
            {
                IntPtr currentPointerValue = memoryManager.Read<IntPtr>(pointerAddress);
                if (currentPointerValue != IntPtr.Zero)
                {
                    byte[] currentData = memoryManager.ReadData(currentPointerValue + pointerOffset, dataSize);
                    if (!previousData.SequenceEqual(currentData))
                    {
                        OnMemoryChanged(currentPointerValue + pointerOffset, MarshalType<T>.ByteArrayToObject(currentData));
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
                        byte[] currentData = memoryManager.ReadData(currentPointerValue + pointerOffset, dataSize);
                        if (!previousData.SequenceEqual(currentData))
                        {
                            OnMemoryChanged(currentPointerValue + pointerOffset, MarshalType<T>.ByteArrayToObject(currentData));
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
