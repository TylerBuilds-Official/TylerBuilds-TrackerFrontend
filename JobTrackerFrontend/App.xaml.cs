using System.IO;
using System.Windows;
using JobTrackerFrontend.Services;
using JobTrackerFrontend.ViewModels;
using JobTrackerFrontend.Views;

using Velopack;

namespace JobTrackerFrontend;

public partial class App : Application
{
    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Services
        var themeService = new ThemeService();
        themeService.Initialize();
        var authService = new AuthService();
        var apiClient = new ApiClient(authService);
        var navigationService = new NavigationService();

        // Update manager
        UpdateManager? updateManager = null;
        var feedPath = AppConfig.UpdateFeedPath;
        if (!string.IsNullOrEmpty(feedPath) && Directory.Exists(feedPath))
            updateManager = new UpdateManager(feedPath);

        // ViewModels
        var dashboardVm = new DashboardViewModel(apiClient, themeService);
        var clientsVm = new ClientsViewModel(apiClient);
        var jobsVm = new JobsViewModel(apiClient);
        var invoicesVm = new InvoicesViewModel(apiClient);
        var expensesVm = new ExpensesViewModel(apiClient);
        var timeClockVm = new TimeClockViewModel(apiClient);
        var researchVm = new ResearchViewModel(apiClient);
        var notesVm = new NotesViewModel(apiClient);

        // Register navigation routes
        navigationService.Register("Dashboard", () => new DashboardView { DataContext = dashboardVm });
        navigationService.Register("Clients", () => new ClientsView { DataContext = clientsVm });
        navigationService.Register("Jobs", () => new JobsView { DataContext = jobsVm });
        navigationService.Register("Invoices", () => new InvoicesView { DataContext = invoicesVm });
        navigationService.Register("Expenses", () => new ExpensesView { DataContext = expensesVm });
        navigationService.Register("Research", () => new ResearchView { DataContext = researchVm });
        navigationService.Register("Notes", () => new NotesView { DataContext = notesVm });
        navigationService.Register("Time Clock", () => new TimeClockView { DataContext = timeClockVm });

        // Main Window
        var mainVm = new MainWindowViewModel(navigationService, authService, themeService, updateManager);
        var mainWindow = new MainWindow { DataContext = mainVm };
        mainWindow.Show();

        // Try restoring previous session
        await mainVm.TryAutoLoginAsync();
    }
}
