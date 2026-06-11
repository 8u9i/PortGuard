using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using PortGuard.Core.Abstractions;
using PortGuard.Core.Models;
using PortGuard.Core.Services;

namespace PortGuard.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IPortEnumerator _enumerator;
    private readonly IProcessKiller _killer;
    private readonly PortMonitor? _monitor;
    private CancellationTokenSource? _monitorCts;

    public ObservableCollection<PortEntryViewModel> AllPorts { get; } = new();

    public ICollectionView FilteredPorts { get; }

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); FilteredPorts.Refresh(); }
    }

    private ProtocolType? _selectedProtocol;
    public ProtocolType? SelectedProtocol
    {
        get => _selectedProtocol;
        set { _selectedProtocol = value; OnPropertyChanged(); FilteredPorts.Refresh(); }
    }

    private PortState? _selectedState;
    public PortState? SelectedState
    {
        get => _selectedState;
        set { _selectedState = value; OnPropertyChanged(); FilteredPorts.Refresh(); }
    }

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set { _isRefreshing = value; OnPropertyChanged(); }
    }

    private bool _autoRefresh = true;
    public bool AutoRefresh
    {
        get => _autoRefresh;
        set
        {
            _autoRefresh = value;
            OnPropertyChanged();
            if (value)
                StartMonitor();
            else
                StopMonitor();
        }
    }

    private TimeSpan _refreshInterval = TimeSpan.FromSeconds(2);
    public TimeSpan RefreshInterval
    {
        get => _refreshInterval;
        set
        {
            _refreshInterval = value;
            OnPropertyChanged();
            RestartMonitor();
        }
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set { _totalCount = value; OnPropertyChanged(); }
    }

    private PortEntryViewModel? _selectedPort;
    public PortEntryViewModel? SelectedPort
    {
        get => _selectedPort;
        set { _selectedPort = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsPortSelected)); }
    }

    public bool IsPortSelected => _selectedPort != null;

    private string _statusText = "Loading...";
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public bool IsAdmin { get; }

    public ICommand RefreshCommand { get; }
    public ICommand KillCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand OpenFileLocationCommand { get; }

    public List<ProtocolType?> ProtocolFilterOptions { get; }
    public List<PortState?> StateFilterOptions { get; }
    public List<int> IntervalOptions { get; } = new() { 1, 2, 5, 10, 30 };

    public MainViewModel(IPortEnumerator enumerator, IProcessKiller killer)
    {
        _enumerator = enumerator;
        _killer = killer;

        ProtocolFilterOptions = new List<ProtocolType?> { null };
        ProtocolFilterOptions.AddRange(Enum.GetValues<ProtocolType>().Cast<ProtocolType?>());
        StateFilterOptions = new List<PortState?> { null };
        StateFilterOptions.AddRange(Enum.GetValues<PortState>().Cast<PortState?>());

        FilteredPorts = CollectionViewSource.GetDefaultView(AllPorts);
        FilteredPorts.Filter = FilterPortEntry;

        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        IsAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

        RefreshCommand = new RelayCommand(_ => _ = DoRefreshAsync());
        KillCommand = new RelayCommand(_ => KillSelected(), _ => IsPortSelected);
        CopyCommand = new RelayCommand(_ => CopySelected());
        OpenFileLocationCommand = new RelayCommand(_ => OpenFileLocation());

        _monitor = new PortMonitor(_enumerator);
        _monitor.PortsChanged += (_, ports) =>
        {
            Application.Current?.Dispatcher.BeginInvoke(() => UpdatePorts(ports));
        };
    }

    public async Task InitializeAsync()
    {
        await DoRefreshAsync();
        StartMonitor();
    }

    private void StartMonitor()
    {
        if (_monitor == null) return;
        _monitorCts?.Cancel();
        _monitorCts = new CancellationTokenSource();
        _monitor.RefreshInterval = RefreshInterval;
        _ = _monitor.StartAsync(_monitorCts.Token);
    }

    private void StopMonitor()
    {
        _monitorCts?.Cancel();
    }

    private void RestartMonitor()
    {
        if (_autoRefresh) StartMonitor();
    }

    private async Task DoRefreshAsync()
    {
        IsRefreshing = true;
        StatusText = "Refreshing...";

        try
        {
            var ports = await Task.Run(() => _enumerator.EnumerateAll());
            UpdatePorts(ports);
            StatusText = $"✅ Ready — {ports.Count} connections";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void UpdatePorts(IReadOnlyList<PortEntry> ports)
    {
        AllPorts.Clear();
        foreach (var port in ports)
        {
            var vm = new PortEntryViewModel(port);
            vm.KillCommand = new RelayCommand(_ => KillProcess(vm));
            vm.InvestigateCommand = new RelayCommand(_ => InvestigateProcess(vm));
            AllPorts.Add(vm);
        }
        TotalCount = ports.Count;
    }

    private void KillProcess(PortEntryViewModel vm)
    {
        if (vm.ProcessId == 0 || vm.ProcessId == 4)
        {
            StatusText = "❌ Cannot kill System process.";
            return;
        }

        var result = _killer.Kill(vm.ProcessId, force: true);
        if (result.Success)
        {
            StatusText = $"✅ Killed {vm.ProcessName} (PID {vm.ProcessId})";
            _ = DoRefreshAsync();
        }
        else
        {
            StatusText = $"❌ {(result.RequiredPrivileges != null ? "Admin required: " : "")}{result.ErrorMessage}";
        }
    }

    private void KillSelected()
    {
        if (SelectedPort != null)
            KillProcess(SelectedPort);
    }

    private void CopySelected()
    {
        if (SelectedPort == null) return;
        var text = $"{SelectedPort.LocalAddress}:{SelectedPort.Port} ({SelectedPort.Protocol}) — {SelectedPort.ProcessName} [PID {SelectedPort.ProcessId}]";
        Clipboard.SetText(text);
        StatusText = "📋 Copied to clipboard.";
    }

    private void OpenFileLocation()
    {
        if (SelectedPort == null || string.IsNullOrEmpty(SelectedPort.ProcessPath)) return;

        try
        {
            Process.Start("explorer.exe", $"/select,\"{SelectedPort.ProcessPath}\"");
        }
        catch (Exception ex)
        {
            StatusText = $"❌ {ex.Message}";
        }
    }

    private void InvestigateProcess(PortEntryViewModel vm)
    {
        if (vm.ProcessId == 0) return;
        try
        {
            Process.Start("explorer.exe", $"/select,\"{vm.ProcessPath}\"");
        }
        catch
        {
            StatusText = $"❌ Cannot open location for PID {vm.ProcessId}";
        }
    }

    private bool FilterPortEntry(object obj)
    {
        if (obj is not PortEntryViewModel vm) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            if (!vm.ProcessName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) &&
                !vm.Port.ToString().Contains(SearchText) &&
                !vm.LocalAddress.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (SelectedProtocol.HasValue && vm.Entry.Proto != SelectedProtocol.Value)
            return false;

        if (SelectedState.HasValue && vm.Entry.State != SelectedState.Value)
            return false;

        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
