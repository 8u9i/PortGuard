using System.Net;
using System.Runtime.InteropServices;
using PortGuard.Core.Abstractions;
using PortGuard.Core.Models;
using PortGuard.Core.Services;
using PortGuard.Windows.Interop;
using PortGuard.Windows.Structs;

namespace PortGuard.Windows;

public class NativePortEnumerator : IPortEnumerator
{
    private readonly ProcessInfoResolver _resolver = new();

    public IReadOnlyList<PortEntry> EnumerateAll()
    {
        _resolver.ClearCache();
        var entries = new List<PortEntry>();

        EnumerateTcp(NativeMethods.AfInet, entries);
        EnumerateTcp(NativeMethods.AfInet6, entries);
        EnumerateUdp(NativeMethods.AfInet, entries);
        EnumerateUdp(NativeMethods.AfInet6, entries);

        // Sort by port ascending
        entries.Sort((a, b) =>
        {
            int cmp = a.Port.CompareTo(b.Port);
            if (cmp != 0) return cmp;
            return a.Proto.CompareTo(b.Proto);
        });

        return entries;
    }

    public IReadOnlyList<PortEntry> EnumerateFiltered(int? port, ProtocolType? proto, string? processName)
    {
        var all = EnumerateAll();
        IEnumerable<PortEntry> filtered = all;

        if (port.HasValue)
            filtered = filtered.Where(e => e.Port == port.Value);
        if (proto.HasValue)
            filtered = filtered.Where(e => e.Proto == proto.Value);
        if (!string.IsNullOrWhiteSpace(processName))
            filtered = filtered.Where(e => e.ProcessName.Contains(processName, StringComparison.OrdinalIgnoreCase));

        return filtered.ToList();
    }

    private void EnumerateTcp(int addressFamily, List<PortEntry> entries)
    {
        int bufferSize = 0;
        uint result = NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, false, addressFamily,
            NativeMethods.TcpTableOwnerPidAll, 0);

        if (result != NativeMethods.ErrorInsufficientBuffer)
            return;

        IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            result = NativeMethods.GetExtendedTcpTable(buffer, ref bufferSize, false, addressFamily,
                NativeMethods.TcpTableOwnerPidAll, 0);

            if (result != 0)
                return;

            int rowCount = Marshal.ReadInt32(buffer);

            if (addressFamily == NativeMethods.AfInet)
            {
                ReadTcpRows(buffer, rowCount, entries);
            }
            else
            {
                ReadTcp6Rows(buffer, rowCount, entries);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void ReadTcpRows(IntPtr buffer, int rowCount, List<PortEntry> entries)
    {
        int rowSize = Marshal.SizeOf<MibTcpRowOwnerPid>();
        IntPtr rowPtr = IntPtr.Add(buffer, 4); // skip dwNumEntries

        for (int i = 0; i < rowCount; i++)
        {
            var row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(rowPtr);

            int localPort = IPAddress.NetworkToHostOrder((short)(row.localPort & 0xFFFF));
            int remotePort = IPAddress.NetworkToHostOrder((short)(row.remotePort & 0xFFFF));

            var localAddr = new IPAddress(row.localAddr);
            var remoteAddr = new IPAddress(row.remoteAddr);
            var state = (PortState)row.state;

            var procInfo = _resolver.Resolve((int)row.owningPid);

            entries.Add(new PortEntry(
                localPort, ProtocolType.Tcp, state,
                localAddr, remoteAddr,
                (int)row.owningPid,
                procInfo.Name,
                procInfo.Path
            ));

            rowPtr = IntPtr.Add(rowPtr, rowSize);
        }
    }

    private void ReadTcp6Rows(IntPtr buffer, int rowCount, List<PortEntry> entries)
    {
        int rowSize = Marshal.SizeOf<MibTcp6RowOwnerPid>();
        IntPtr rowPtr = IntPtr.Add(buffer, 4);

        for (int i = 0; i < rowCount; i++)
        {
            var row = Marshal.PtrToStructure<MibTcp6RowOwnerPid>(rowPtr);

            int localPort = IPAddress.NetworkToHostOrder((short)(row.localPort & 0xFFFF));
            int remotePort = IPAddress.NetworkToHostOrder((short)(row.remotePort & 0xFFFF));

            var localAddr = new IPAddress(row.localAddr.data);
            var remoteAddr = new IPAddress(row.remoteAddr.data);
            var state = (PortState)row.state;

            var procInfo = _resolver.Resolve((int)row.owningPid);

            entries.Add(new PortEntry(
                localPort, ProtocolType.Tcp6, state,
                localAddr, remoteAddr,
                (int)row.owningPid,
                procInfo.Name,
                procInfo.Path
            ));

            rowPtr = IntPtr.Add(rowPtr, rowSize);
        }
    }

    private void EnumerateUdp(int addressFamily, List<PortEntry> entries)
    {
        int bufferSize = 0;
        uint result = NativeMethods.GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, false, addressFamily,
            NativeMethods.UdpTableOwnerPid, 0);

        if (result != NativeMethods.ErrorInsufficientBuffer)
            return;

        IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            result = NativeMethods.GetExtendedUdpTable(buffer, ref bufferSize, false, addressFamily,
                NativeMethods.UdpTableOwnerPid, 0);

            if (result != 0)
                return;

            int rowCount = Marshal.ReadInt32(buffer);

            if (addressFamily == NativeMethods.AfInet)
            {
                ReadUdpRows(buffer, rowCount, entries);
            }
            else
            {
                ReadUdp6Rows(buffer, rowCount, entries);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void ReadUdpRows(IntPtr buffer, int rowCount, List<PortEntry> entries)
    {
        int rowSize = Marshal.SizeOf<MibUdpRowOwnerPid>();
        IntPtr rowPtr = IntPtr.Add(buffer, 4);

        for (int i = 0; i < rowCount; i++)
        {
            var row = Marshal.PtrToStructure<MibUdpRowOwnerPid>(rowPtr);

            int localPort = IPAddress.NetworkToHostOrder((short)(row.localPort & 0xFFFF));
            var localAddr = new IPAddress(row.localAddr);

            var procInfo = _resolver.Resolve((int)row.owningPid);

            entries.Add(new PortEntry(
                localPort, ProtocolType.Udp, PortState.Bound,
                localAddr, IPAddress.Any,
                (int)row.owningPid,
                procInfo.Name,
                procInfo.Path
            ));

            rowPtr = IntPtr.Add(rowPtr, rowSize);
        }
    }

    private void ReadUdp6Rows(IntPtr buffer, int rowCount, List<PortEntry> entries)
    {
        int rowSize = Marshal.SizeOf<MibUdp6RowOwnerPid>();
        IntPtr rowPtr = IntPtr.Add(buffer, 4);

        for (int i = 0; i < rowCount; i++)
        {
            var row = Marshal.PtrToStructure<MibUdp6RowOwnerPid>(rowPtr);

            int localPort = IPAddress.NetworkToHostOrder((short)(row.localPort & 0xFFFF));
            var localAddr = new IPAddress(row.localAddr.data);

            var procInfo = _resolver.Resolve((int)row.owningPid);

            entries.Add(new PortEntry(
                localPort, ProtocolType.Udp6, PortState.Bound,
                localAddr, IPAddress.IPv6Any,
                (int)row.owningPid,
                procInfo.Name,
                procInfo.Path
            ));

            rowPtr = IntPtr.Add(rowPtr, rowSize);
        }
    }
}
