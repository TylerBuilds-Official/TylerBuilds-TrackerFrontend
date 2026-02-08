using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;

namespace JobTrackerFrontend.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;

    [ObservableProperty] private RevenueSummaryModel? _revenueSummary;
    [ObservableProperty] private ObservableCollection<JobPipelineModel> _pipeline = [];
    [ObservableProperty] private ObservableCollection<RecentActivityModel> _recentActivity = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    public DashboardViewModel(ApiClient apiClient)
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
            RevenueSummary = await _apiClient.GetAsync<RevenueSummaryModel>("/dashboard/revenue-summary");

            var pipelineData = await _apiClient.GetAsync<List<JobPipelineModel>>("/dashboard/job-pipeline");
            Pipeline = new ObservableCollection<JobPipelineModel>(pipelineData ?? []);

            var activityData = await _apiClient.GetAsync<List<RecentActivityModel>>("/dashboard/recent-activity?limit=10");
            RecentActivity = new ObservableCollection<RecentActivityModel>(activityData ?? []);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load dashboard: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
