using System.Windows.Input;
using System.Windows.Media;
using PortGuard.Core.Models;

namespace PortGuard.App.ViewModels;

public class PortEntryViewModel
{
    private readonly PortEntry _entry;

    public PortEntry Entry => _entry;

    public int Port => _entry.Port;
    public string Protocol => _entry.Proto switch
    {
        ProtocolType.Tcp => "TCP",
        ProtocolType.Udp => "UDP",
        ProtocolType.Tcp6 => "TCPv6",
        ProtocolType.Udp6 => "UDPv6",
        _ => "?"
    };

    public string StateText => _entry.State.ToString().Replace("Wait", " WAIT");

    public SolidColorBrush StateColor => _entry.State switch
    {
        PortState.Listen => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
        PortState.Established => new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)),
        PortState.TimeWait => new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)),
        PortState.CloseWait => new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)),
        PortState.SynSent => new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)),
        PortState.Bound => new SolidColorBrush(Color.FromRgb(0x00, 0x96, 0x88)),
        _ => new SolidColorBrush(Color.FromRgb(0x75, 0x75, 0x75))
    };

    public SolidColorBrush ProtocolColor => _entry.Proto switch
    {
        ProtocolType.Tcp => new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)),
        ProtocolType.Udp => new SolidColorBrush(Color.FromRgb(0x9C, 0x27, 0xB0)),
        ProtocolType.Tcp6 => new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0)),
        ProtocolType.Udp6 => new SolidColorBrush(Color.FromRgb(0x7B, 0x1F, 0xA2)),
        _ => new SolidColorBrush(Colors.Gray)
    };

    public string LocalAddress => _entry.LocalAddress.ToString();
    public string RemoteAddress => _entry.State == PortState.Listen || _entry.State == PortState.Bound
        ? "*:*"
        : $"{_entry.RemoteAddress}";

    public int ProcessId => _entry.ProcessId;
    public string ProcessName => _entry.ProcessName;
    public string ProcessPath => _entry.ProcessPath;

    public string TooltipText =>
        $"{_entry.LocalAddress}:{_entry.Port} → {_entry.RemoteAddress}\nPID: {_entry.ProcessId} ({_entry.ProcessName})";

    public ICommand KillCommand { get; set; } = null!;
    public ICommand InvestigateCommand { get; set; } = null!;

    public PortEntryViewModel(PortEntry entry)
    {
        _entry = entry;
    }
}
