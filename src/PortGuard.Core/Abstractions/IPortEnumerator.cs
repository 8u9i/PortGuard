using PortGuard.Core.Models;

namespace PortGuard.Core.Abstractions;

public interface IPortEnumerator
{
    IReadOnlyList<PortEntry> EnumerateAll();
    IReadOnlyList<PortEntry> EnumerateFiltered(int? port, ProtocolType? proto, string? processName);
}
