using System.Runtime.InteropServices;

namespace PortGuard.Windows.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct MibTcpRowOwnerPid
{
    public uint state;
    public uint localAddr;
    public uint localPort;
    public uint remoteAddr;
    public uint remotePort;
    public uint owningPid;
}
