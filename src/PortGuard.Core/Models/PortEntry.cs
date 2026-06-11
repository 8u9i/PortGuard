using System.Net;

namespace PortGuard.Core.Models;

public record PortEntry(
    int Port,
    ProtocolType Proto,
    PortState State,
    IPAddress LocalAddress,
    IPAddress RemoteAddress,
    int ProcessId,
    string ProcessName,
    string ProcessPath
);
