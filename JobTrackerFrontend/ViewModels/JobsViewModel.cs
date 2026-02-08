using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;
using JobTrackerFrontend.Views;

namespace JobTrackerFrontend.ViewModels;

public partial class JobsViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;

    [ObservableProperty] private ObservableCollection<JobModel> _jobs = [];
    [ObservableProperty] private JobModel? _selectedJob;
    [ObservableProperty] private string? _statusFilter;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    public JobsViewModel(ApiClient apiClient)
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
            var endpoint = string.IsNullOrEmpty(StatusFilter)
                ? "/jobs"
                : $"/jobs?status={StatusFilter}";

            var data = await _apiClient.GetAsync<List<JobModel>>(endpoint);
            Jobs = new ObservableCollection<JobModel>(data ?? []);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load jobs: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NewJobAsync()
    {
        var vm = new JobFormViewModel(_apiClient);
        var dialog = new JobFormDialog
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
    private async Task EditJobAsync()
    {
        if (SelectedJob is null) return;

        var job = await _apiClient.GetAsync<JobModel>($"/jobs/{SelectedJob.Id}");
        if (job is null) return;

        var vm = new JobFormViewModel(_apiClient, job);
        var dialog = new JobFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadDataAsync();
        }
    }
}
