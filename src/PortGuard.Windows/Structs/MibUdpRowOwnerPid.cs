using System.Runtime.InteropServices;

namespace PortGuard.Windows.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct MibUdpRowOwnerPid
{
    public uint localAddr;
    public uint localPort;
    public uint owningPid;
}
