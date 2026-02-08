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

    // List mode
    [ObservableProperty] private ObservableCollection<JobModel> _jobs = [];
    [ObservableProperty] private JobModel? _selectedJob;
    [ObservableProperty] private string? _statusFilter;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    // Detail mode
    [ObservableProperty] private bool _isDetailMode;
    [ObservableProperty] private JobModel? _detailJob;
    [ObservableProperty] private ObservableCollection<InvoiceModel> _jobInvoices = [];
    [ObservableProperty] private InvoiceModel? _selectedJobInvoice;

    public List<string> Statuses { get; } = ["Lead", "Proposal", "Active", "Completed", "Invoiced"];

    public JobsViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // ── List Mode ──────────────────────────────────────────

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
    private async Task ViewDetailAsync()
    {
        if (SelectedJob is null) return;

        var job = await _apiClient.GetAsync<JobModel>($"/jobs/{SelectedJob.Id}");
        if (job is null) return;

        DetailJob = job;
        IsDetailMode = true;
        await LoadJobInvoicesAsync();
    }

    [RelayCommand]
    private async Task BackToListAsync()
    {
        IsDetailMode = false;
        DetailJob = null;
        JobInvoices = [];
        await LoadDataAsync();
    }

    private async Task LoadJobInvoicesAsync()
    {
        if (DetailJob is null) return;
        try
        {
            var data = await _apiClient.GetAsync<List<InvoiceModel>>($"/invoices/by-job/{DetailJob.Id}");
            JobInvoices = new ObservableCollection<InvoiceModel>(data ?? []);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load invoices: {ex.Message}";
        }
    }

    // ── Status Quick Action ────────────────────────────────

    [RelayCommand]
    private async Task SetStatusAsync(string status)
    {
        var job = IsDetailMode ? DetailJob : SelectedJob;
        if (job is null) return;

        try
        {
            var request = new UpdateJobStatusRequest { Status = status };
            await _apiClient.PatchAsync<UpdateJobStatusRequest, JobModel>(
                $"/jobs/{job.Id}/status", request);

            if (IsDetailMode)
            {
                DetailJob = await _apiClient.GetAsync<JobModel>($"/jobs/{job.Id}");
            }
            else
            {
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to update status: {ex.Message}", "Error");
        }
    }

    // ── Job CRUD ───────────────────────────────────────────

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
        var job = IsDetailMode ? DetailJob : SelectedJob;
        if (job is null) return;

        var fresh = await _apiClient.GetAsync<JobModel>($"/jobs/{job.Id}");
        if (fresh is null) return;

        var vm = new JobFormViewModel(_apiClient, fresh);
        var dialog = new JobFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            if (IsDetailMode)
            {
                DetailJob = await _apiClient.GetAsync<JobModel>($"/jobs/{job.Id}");
                await LoadJobInvoicesAsync();
            }
            else
            {
                await LoadDataAsync();
            }
        }
    }
}
