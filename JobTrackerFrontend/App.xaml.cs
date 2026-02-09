using System.Windows;
using JobTrackerFrontend.Services;
using JobTrackerFrontend.ViewModels;
using JobTrackerFrontend.Views;

namespace JobTrackerFrontend;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Services
        var themeService = new ThemeService();
        themeService.Initialize();
        var authService = new AuthService();
        var apiClient = new ApiClient(authService);
        var navigationService = new NavigationService();

        // ViewModels
        var dashboardVm = new DashboardViewModel(apiClient, themeService);
        var clientsVm = new ClientsViewModel(apiClient);
        var jobsVm = new JobsViewModel(apiClient);
        var invoicesVm = new InvoicesViewModel(apiClient);

        // Register navigation routes
        navigationService.Register("Dashboard", () => new DashboardView { DataContext = dashboardVm });
        navigationService.Register("Clients", () => new ClientsView { DataContext = clientsVm });
        navigationService.Register("Jobs", () => new JobsView { DataContext = jobsVm });
        navigationService.Register("Invoices", () => new InvoicesView { DataContext = invoicesVm });

        // Main Window
        var mainVm = new MainWindowViewModel(navigationService, authService, themeService);
        var mainWindow = new MainWindow { DataContext = mainVm };
        mainWindow.Show();

        // Try restoring previous session
        await mainVm.TryAutoLoginAsync();
    }
}
