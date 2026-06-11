using System.Runtime.InteropServices;

namespace PortGuard.Windows.Interop;

internal static class NativeMethods
{
    public const int AfInet = 2;
    public const int AfInet6 = 23;

    public const int TcpTableOwnerPidAll = 5;
    public const int UdpTableOwnerPid = 1;

    public const int ErrorInsufficientBuffer = 122;

    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int dwOutBufLen,
        bool sort,
        int ipVersion,
        int tableClass,
        int reserved = 0);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern uint GetExtendedUdpTable(
        IntPtr pUdpTable,
        ref int dwOutBufLen,
        bool sort,
        int ipVersion,
        int tableClass,
        int reserved = 0);
}
