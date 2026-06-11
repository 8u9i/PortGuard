using System.Windows;
using System.Windows.Threading;
using PortGuard.App.ViewModels;

namespace PortGuard.App;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        // Show window immediately
        this.Show();
        this.Topmost = true;
        this.Focus();
        this.Topmost = false;

        // Load data on background thread
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(async () =>
        {
            var enumerator = new PortGuard.Windows.NativePortEnumerator();
            var killer = new PortGuard.Core.Services.ProcessKiller();
            _viewModel = new MainViewModel(enumerator, killer);
            DataContext = _viewModel;
            await _viewModel.InitializeAsync();
        }));
    }

    private void ToggleAutoRefresh(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.AutoRefresh = !_viewModel.AutoRefresh;
    }
}
