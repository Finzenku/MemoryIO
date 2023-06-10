using MemoryIO.Internals;
using MemoryIO.Managers;

namespace MemoryIO.Monitors
{
    /// <summary>
    /// Monitors a specific array of T elements in memory and captures the changes as the specified type T.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    public class MemoryMonitorTArray<T> : IMemoryMonitor<MemoryArrayChangedEventArgs<T>>, IDisposable
    {
        /// <summary>
        /// Event that is raised when the monitored memory array changes.
        /// </summary>
        public event EventHandler<MemoryArrayChangedEventArgs<T>>? MemoryChanged;

        private IntPtr address;
        private IProcessMemoryIO memoryManager;
        private int dataSize;
        private int arrayLength;
        private byte[] previousData;
        private int pollingRate;
        private bool isMonitoring;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryMonitorTArray{T}"/> class.
        /// </summary>
        /// <param name="memoryManager">The <see cref="IProcessMemoryIO"/> used to read memory.</param>
        /// <param name="address">The address of the memory array.</param>
        /// <param name="arrayLength">The number of <see cref="T"/> objects in the memory array.</param>
        /// <param name="pollingRateInMilliseconds">The interval between memory checks in milliseconds.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="memoryManager"/> is null or <paramref name="arrayLength"/> is not greater than 0.</exception>
        public MemoryMonitorTArray(IProcessMemoryIO memoryManager, IntPtr address, int arrayLength, int pollingRateInMilliseconds = 10)
        {
            if (memoryManager is null)
                throw new ArgumentException("MemoryManager must not be null.", nameof(memoryManager));

            if (arrayLength <= 0)
                throw new ArgumentException("The array length must be a positive value.", nameof(arrayLength));

            this.address = address;
            this.memoryManager = memoryManager;
            dataSize = MarshalType<T>.Size;
            this.arrayLength = arrayLength;
            previousData = new byte[arrayLength * dataSize];
            pollingRate = pollingRateInMilliseconds;
            isMonitoring = false;
        }

        void OnMemoryChanged(IntPtr address, T value, int index)
        {
            MemoryArrayChangedEventArgs<T> args = new MemoryArrayChangedEventArgs<T>(address, value, index);
            MemoryChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Starts monitoring the memory array for changes synchronously.
        /// </summary>
        public void StartMonitoring()
        {
            if (isMonitoring || memoryManager is null) return;
            isMonitoring = true;

            while (address != IntPtr.Zero && isMonitoring)
            {
                for (int i = 0; i < arrayLength; i++)
                {
                    int startIndex = i * dataSize;

                    byte[] currentData = memoryManager.ReadData(address + startIndex, dataSize);
                    if (!previousData.AsSpan(startIndex, dataSize).SequenceEqual(currentData))
                    {
                        OnMemoryChanged(address, MarshalType<T>.ByteArrayToObject(currentData), i);
                        currentData.CopyTo(previousData, startIndex);
                    }
                }
                Thread.Sleep(pollingRate);
            }
            StopMonitoring();
        }

        /// <summary>
        /// Starts monitoring the memory array for changes asynchronously.
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
                    for (int i = 0; i < arrayLength; i++)
                    {
                        int startIndex = i * dataSize;

                        byte[] currentData = memoryManager.ReadData(address + startIndex, dataSize);
                        if (!previousData.AsSpan(startIndex, dataSize).SequenceEqual(currentData))
                        {
                            OnMemoryChanged(address, MarshalType<T>.ByteArrayToObject(currentData), i);
                            currentData.CopyTo(previousData, startIndex);
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
        /// Stops monitoring the memory array.
        /// </summary>
        public void StopMonitoring()
        {
            isMonitoring = false;
        }

        /// <summary>
        /// Sets a new pointer address for the monitored memory array.
        /// </summary>
        /// <param name="newAddress">The new pointer address to set.</param>
        public void SetNewAddress(IntPtr newAddress) => address = newAddress;

        /// <summary>
        /// Disposes the memory monitor and stops monitoring the memory array.
        /// </summary>
        public void Dispose()
        {
            StopMonitoring();
            MemoryChanged = null;
        }
    }
}
