using System.Runtime.InteropServices;

namespace PortGuard.Windows.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct MibTcp6RowOwnerPid
{
    public MibUcharArray localAddr;
    public uint localScopeId;
    public uint localPort;
    public MibUcharArray remoteAddr;
    public uint remoteScopeId;
    public uint remotePort;
    public uint state;
    public uint owningPid;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MibUdp6RowOwnerPid
{
    public MibUcharArray localAddr;
    public uint localScopeId;
    public uint localPort;
    public uint owningPid;
}

[StructLayout(LayoutKind.Sequential, Size = 16)]
internal struct MibUcharArray
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] data;

    public MibUcharArray(byte[] bytes)
    {
        data = new byte[16];
        if (bytes.Length <= 16)
            Array.Copy(bytes, data, bytes.Length);
    }
}
