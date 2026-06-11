using PortGuard.Core.Models;

namespace PortGuard.Core.Abstractions;

public interface IPortMonitor
{
    TimeSpan RefreshInterval { get; set; }
    event EventHandler<IReadOnlyList<PortEntry>>? PortsChanged;
    Task StartAsync(CancellationToken ct);
    void Stop();
}
