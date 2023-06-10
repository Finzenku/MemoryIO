namespace MemoryIO.Monitors
{
    public interface IMemoryMonitor<TEventArgs> where TEventArgs: EventArgs
    {
        event EventHandler<TEventArgs>? MemoryChanged;
        void StartMonitoring();
        Task StartMonitoringAsync(CancellationToken cancellationToken);
        void StopMonitoring();
        public void SetNewAddress(IntPtr newAddress);
    }
}