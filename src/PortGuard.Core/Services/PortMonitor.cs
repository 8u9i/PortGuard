using PortGuard.Core.Abstractions;
using PortGuard.Core.Models;

namespace PortGuard.Core.Services;

public class PortMonitor : IPortMonitor, IDisposable
{
    private readonly IPortEnumerator _enumerator;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private IReadOnlyList<PortEntry>? _lastPorts;

    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(2);

    public event EventHandler<IReadOnlyList<PortEntry>>? PortsChanged;

    public PortMonitor(IPortEnumerator enumerator)
    {
        _enumerator = enumerator;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _timer = new PeriodicTimer(RefreshInterval);

        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                var ports = _enumerator.EnumerateAll();
                if (!SequenceEqual(ports, _lastPorts))
                {
                    _lastPorts = ports;
                    PortsChanged?.Invoke(this, ports);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }

    private static bool SequenceEqual(IReadOnlyList<PortEntry>? a, IReadOnlyList<PortEntry>? b)
    {
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }
}
