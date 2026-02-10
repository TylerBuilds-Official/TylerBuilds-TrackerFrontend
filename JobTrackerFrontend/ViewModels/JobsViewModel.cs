using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
    [ObservableProperty] private ObservableCollection<ExpenseModel> _jobExpenses = [];
    [ObservableProperty] private ExpenseModel? _selectedJobExpense;

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
        await LoadJobDetailDataAsync();
    }

    [RelayCommand]
    private async Task BackToListAsync()
    {
        IsDetailMode = false;
        DetailJob = null;
        JobInvoices = [];
        JobExpenses = [];
        await LoadDataAsync();
    }

    private async Task LoadJobDetailDataAsync()
    {
        if (DetailJob is null) return;
        try
        {
            var invoices = await _apiClient.GetAsync<List<InvoiceModel>>($"/invoices/by-job/{DetailJob.Id}");
            JobInvoices = new ObservableCollection<InvoiceModel>(invoices ?? []);

            var expenses = await _apiClient.GetAsync<List<ExpenseModel>>($"/expenses/by-job/{DetailJob.Id}");
            JobExpenses = new ObservableCollection<ExpenseModel>(expenses ?? []);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load detail data: {ex.Message}";
        }
    }

    // ── Invoice Tab Actions ────────────────────────────────

    [RelayCommand]
    private async Task NewJobInvoiceAsync()
    {
        if (DetailJob is null) return;

        var vm = new InvoiceFormViewModel(_apiClient, DetailJob.Id, isJobPreselect: true);
        var dialog = new InvoiceFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
            await LoadJobDetailDataAsync();
    }

    [RelayCommand]
    private void OpenJobInvoiceFile()
    {
        if (SelectedJobInvoice is null || string.IsNullOrEmpty(SelectedJobInvoice.NetworkFilePath))
            return;

        if (!File.Exists(SelectedJobInvoice.NetworkFilePath))
        {
            MessageBox.Show("Invoice file not found.", "Error");
            return;
        }

        Process.Start(new ProcessStartInfo(SelectedJobInvoice.NetworkFilePath) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task DeleteJobInvoiceAsync()
    {
        if (SelectedJobInvoice is null) return;

        var result = MessageBox.Show(
            $"Delete invoice {SelectedJobInvoice.DisplayNumber}?\n\nThis action cannot be undone.",
            "Delete Invoice",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _apiClient.DeleteAsync($"/invoices/{SelectedJobInvoice.Id}");
            await LoadJobDetailDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete invoice: {ex.Message}", "Error");
        }
    }

    // ── Expense Tab Actions ────────────────────────────────

    [RelayCommand]
    private async Task NewJobExpenseAsync()
    {
        if (DetailJob is null) return;

        var vm = new ExpenseFormViewModel(_apiClient, DetailJob.Id);
        var dialog = new ExpenseFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
            await LoadJobDetailDataAsync();
    }

    [RelayCommand]
    private async Task EditJobExpenseAsync()
    {
        if (SelectedJobExpense is null) return;

        var fresh = await _apiClient.GetAsync<ExpenseModel>($"/expenses/{SelectedJobExpense.Id}");
        if (fresh is null) return;

        var vm = new ExpenseFormViewModel(_apiClient, fresh);
        var dialog = new ExpenseFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
            await LoadJobDetailDataAsync();
    }

    [RelayCommand]
    private async Task DeleteJobExpenseAsync()
    {
        if (SelectedJobExpense is null) return;

        var result = MessageBox.Show(
            $"Delete expense?\n\n{SelectedJobExpense.Vendor} — {SelectedJobExpense.Amount:C}\n\nThis action cannot be undone.",
            "Delete Expense",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _apiClient.DeleteAsync($"/expenses/{SelectedJobExpense.Id}");
            await LoadJobDetailDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete expense: {ex.Message}", "Error");
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
                await LoadJobDetailDataAsync();
            }
            else
            {
                await LoadDataAsync();
            }
        }
    }
}
