using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Services;

namespace JobTrackerFrontend.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;
    private readonly AuthService _authService;

    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private bool _isNavExpanded = true;
    [ObservableProperty] private bool _isAuthenticated;
    [ObservableProperty] private string _selectedNavItem = "Dashboard";
    [ObservableProperty] private bool _staySignedIn = true;

    public NavigationService Navigation => _navigationService;

    public MainWindowViewModel(NavigationService navigationService, AuthService authService)
    {
        _navigationService = navigationService;
        _authService = authService;
    }

    /// <summary>
    /// Called once at app startup — tries to silently restore a previous session.
    /// </summary>
    public async Task TryAutoLoginAsync()
    {
        await _authService.EnablePersistentCacheAsync();

        if (await _authService.TrySilentLoginAsync())
        {
            UserName = _authService.UserDisplayName ?? "User";
            IsAuthenticated = true;
            _navigationService.NavigateTo("Dashboard");
            SelectedNavItem = "Dashboard";
        }
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
