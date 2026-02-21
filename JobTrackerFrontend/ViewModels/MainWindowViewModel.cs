using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Services;
using Velopack;

namespace JobTrackerFrontend.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;
    private readonly AuthService _authService;
    private readonly ThemeService _themeService;
    private readonly UpdateManager? _updateManager;

    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private bool _isNavExpanded = true;
    [ObservableProperty] private bool _isAuthenticated;
    [ObservableProperty] private bool _isCheckingAuth = true;
    [ObservableProperty] private string _authStatusMessage = "Checking for previous session...";
    [ObservableProperty] private string _selectedNavItem = "Dashboard";
    [ObservableProperty] private bool _staySignedIn = true;
    [ObservableProperty] private bool _isDarkMode;

    // Update state
    [ObservableProperty] private bool _isUpdateAvailable;
    [ObservableProperty] private string _updateVersion = "";
    [ObservableProperty] private bool _isDownloadingUpdate;

    private UpdateInfo? _pendingUpdate;

    public NavigationService Navigation => _navigationService;
    public string AppVersion => _updateManager?.CurrentVersion?.ToString()
        ?? System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? "dev";

    public MainWindowViewModel(
        NavigationService navigationService,
        AuthService authService,
        ThemeService themeService,
        UpdateManager? updateManager)
    {
        _navigationService = navigationService;
        _authService = authService;
        _themeService = themeService;
        _updateManager = updateManager;
        _isDarkMode = themeService.IsDarkMode;
    }

    /// <summary>
    /// Called once at app startup — tries to silently restore a previous session.
    /// </summary>
    public async Task TryAutoLoginAsync()
    {
        IsCheckingAuth = true;
        AuthStatusMessage = "Checking for previous session...";

        try
        {
            await _authService.EnablePersistentCacheAsync();

            AuthStatusMessage = "Restoring account...";
            if (await _authService.TrySilentLoginAsync())
            {
                AuthStatusMessage = $"Welcome back, {_authService.UserDisplayName ?? "User"}";
                await Task.Delay(600);

                UserName = _authService.UserDisplayName ?? "User";
                IsAuthenticated = true;
                _navigationService.NavigateTo("Dashboard");
                SelectedNavItem = "Dashboard";
            }
        }
        catch
        {
            // Silent login failed — fall through to login screen
        }
        finally
        {
            IsCheckingAuth = false;
        }

        // Check for updates after auth resolves (fire and forget)
        _ = CheckForUpdatesAsync();
    }

    /// <summary>
    /// Checks the network share for a newer version. Downloads in background if found.
    /// </summary>
    private async Task CheckForUpdatesAsync()
    {
        if (_updateManager is null) return;

        try
        {
            var updateInfo = await _updateManager.CheckForUpdatesAsync();
            if (updateInfo is null) return;

            _pendingUpdate = updateInfo;
            UpdateVersion = updateInfo.TargetFullRelease.Version.ToString();
            IsDownloadingUpdate = true;

            await _updateManager.DownloadUpdatesAsync(updateInfo);

            IsDownloadingUpdate = false;
            IsUpdateAvailable = true;
        }
        catch
        {
            // Update check failed silently — network share unavailable, etc.
            IsDownloadingUpdate = false;
        }
    }

    [RelayCommand]
    private void ApplyUpdate()
    {
        if (_updateManager is null || _pendingUpdate is null) return;
        _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
    }

    [RelayCommand]
    private void DismissUpdate()
    {
        IsUpdateAvailable = false;
    }

    [RelayCommand]
    private void ToggleDarkMode()
    {
        _themeService.Toggle();
        IsDarkMode = _themeService.IsDarkMode;
    }

    [RelayCommand]
    private void ToggleNav()
    {
        IsNavExpanded = !IsNavExpanded;
    }

    [RelayCommand]
    private void NavigateTo(string destination)
    {
        SelectedNavItem = destination;
        _navigationService.NavigateTo(destination);
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            if (StaySignedIn)
                await _authService.EnablePersistentCacheAsync();
            else
                _authService.DisablePersistentCache();

            await _authService.LoginAsync();
            UserName = _authService.UserDisplayName ?? "User";
            IsAuthenticated = true;
            _navigationService.NavigateTo("Dashboard");
            SelectedNavItem = "Dashboard";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Login failed: {ex.Message}", "Authentication Error");
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        UserName = "";
        IsAuthenticated = false;
    }
}
