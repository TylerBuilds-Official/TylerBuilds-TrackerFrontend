using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;
using JobTrackerFrontend.Views;

namespace JobTrackerFrontend.ViewModels;

public partial class ClientsViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;

    [ObservableProperty] private ObservableCollection<ClientModel> _clients = [];
    [ObservableProperty] private ClientModel? _selectedClient;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    public ClientsViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var data = await _apiClient.GetAsync<List<ClientModel>>("/clients?active_only=true");
            Clients = new ObservableCollection<ClientModel>(data ?? []);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load clients: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NewClientAsync()
    {
        var vm = new ClientFormViewModel(_apiClient);
        var dialog = new ClientFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task EditClientAsync()
    {
        if (SelectedClient is null) return;

        // Fetch fresh data for the selected client
        var client = await _apiClient.GetAsync<ClientModel>($"/clients/{SelectedClient.Id}");
        if (client is null) return;

        var vm = new ClientFormViewModel(_apiClient, client);
        var dialog = new ClientFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task DeactivateClientAsync()
    {
        if (SelectedClient is null) return;

        var result = MessageBox.Show(
            $"Deactivate client \"{SelectedClient.Name}\"?\n\nThis will hide the client from active lists.",
            "Confirm Deactivate",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _apiClient.PatchAsync($"/clients/{SelectedClient.Id}/deactivate");
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to deactivate: {ex.Message}", "Error");
            }
        }
    }
}
