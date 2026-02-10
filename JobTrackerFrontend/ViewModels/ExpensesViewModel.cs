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

public partial class ExpensesViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;

    [ObservableProperty] private ObservableCollection<ExpenseModel> _expenses = [];
    [ObservableProperty] private ExpenseModel? _selectedExpense;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    // Summary cards
    [ObservableProperty] private ExpenseSummaryModel? _summary;

    // Filter state
    [ObservableProperty] private string _dateRangeFilter = "This Month";
    [ObservableProperty] private ExpenseCategoryModel? _categoryFilter;
    [ObservableProperty] private JobFilterItem? _jobFilter;
    [ObservableProperty] private string _reimbursableFilter = "";

    // Filter dropdowns
    [ObservableProperty] private List<ExpenseCategoryModel> _categories = [];
    [ObservableProperty] private List<JobFilterItem> _jobFilterItems = [];

    public List<string> DateRangeOptions { get; } = ["This Month", "This Quarter", "This Year", "All"];
    public List<string> ReimbursableOptions { get; } = ["", "Yes", "No"];

    public ExpensesViewModel(ApiClient apiClient)
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
            // Load filter dropdowns
            var cats = await _apiClient.GetAsync<List<ExpenseCategoryModel>>("/expenses/categories");
            Categories = cats ?? [];

            var jobs = await _apiClient.GetAsync<List<JobModel>>("/jobs");
            var items = new List<JobFilterItem> { new() { DisplayName = "", JobId = null, IsGeneral = false } };
            items.Add(new JobFilterItem { DisplayName = "— General Only —", JobId = null, IsGeneral = true });
            foreach (var j in jobs ?? [])
                items.Add(new JobFilterItem { DisplayName = $"{j.Title} — {j.ClientName}", JobId = j.Id, IsGeneral = false });
            JobFilterItems = items;

            // Load summary
            Summary = await _apiClient.GetAsync<ExpenseSummaryModel>("/expenses/summary");

            // Load expenses with filters
            await ApplyFiltersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load expenses: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ApplyFiltersAsync()
    {
        try
        {
            var query = new List<string>();

            // Date range
            var (from, to) = GetDateRange();
            if (from.HasValue) query.Add($"dateFrom={from.Value:yyyy-MM-dd}");
            if (to.HasValue) query.Add($"dateTo={to.Value:yyyy-MM-dd}");

            // Category
            if (CategoryFilter is not null)
                query.Add($"categoryId={CategoryFilter.Id}");

            // Job
            if (JobFilter is { IsGeneral: true })
            {
                // "General Only" — we need server-side support or client filter
                // For now use jobId=0 which won't match, then filter client-side
                // Actually we'll just load all and filter client-side for this case
            }
            else if (JobFilter is { JobId: not null })
            {
                query.Add($"jobId={JobFilter.JobId}");
            }

            // Reimbursable
            if (ReimbursableFilter == "Yes") query.Add("isReimbursable=true");
            else if (ReimbursableFilter == "No") query.Add("isReimbursable=false");

            var endpoint = query.Count > 0 ? $"/expenses?{string.Join("&", query)}" : "/expenses";
            var data = await _apiClient.GetAsync<List<ExpenseModel>>(endpoint);
            var results = data ?? [];

            // Client-side filter for "General Only"
            if (JobFilter is { IsGeneral: true })
                results = results.Where(e => e.JobId is null).ToList();

            Expenses = new ObservableCollection<ExpenseModel>(results);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load expenses: {ex.Message}";
        }
    }

    private (DateTime? from, DateTime? to) GetDateRange()
    {
        var today = DateTime.Today;
        return DateRangeFilter switch
        {
            "This Month" => (new DateTime(today.Year, today.Month, 1), today),
            "This Quarter" => (new DateTime(today.Year, (today.Month - 1) / 3 * 3 + 1, 1), today),
            "This Year" => (new DateTime(today.Year, 1, 1), today),
            _ => (null, null)
        };
    }

    // Re-apply filters when any filter changes
    partial void OnDateRangeFilterChanged(string value) => _ = ApplyFiltersAsync();
    partial void OnCategoryFilterChanged(ExpenseCategoryModel? value) => _ = ApplyFiltersAsync();
    partial void OnJobFilterChanged(JobFilterItem? value) => _ = ApplyFiltersAsync();
    partial void OnReimbursableFilterChanged(string value) => _ = ApplyFiltersAsync();

    [RelayCommand]
    private async Task NewExpenseAsync()
    {
        var vm = new ExpenseFormViewModel(_apiClient);
        var dialog = new ExpenseFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
            await LoadDataAsync();
    }

    [RelayCommand]
    private async Task EditExpenseAsync()
    {
        if (SelectedExpense is null) return;

        var expense = await _apiClient.GetAsync<ExpenseModel>($"/expenses/{SelectedExpense.Id}");
        if (expense is null) return;

        var vm = new ExpenseFormViewModel(_apiClient, expense);
        var dialog = new ExpenseFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
            await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DeleteExpenseAsync()
    {
        if (SelectedExpense is null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete this expense?\n\n" +
            $"{SelectedExpense.Vendor} — {SelectedExpense.Amount:C}\n\n" +
            "This action cannot be undone.",
            "Delete Expense",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _apiClient.DeleteAsync($"/expenses/{SelectedExpense.Id}");
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete expense: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private void OpenReceipt()
    {
        if (SelectedExpense is null || string.IsNullOrEmpty(SelectedExpense.ReceiptFilePath))
            return;

        if (!File.Exists(SelectedExpense.ReceiptFilePath))
        {
            MessageBox.Show("Receipt file not found.", "Error");
            return;
        }

        Process.Start(new ProcessStartInfo(SelectedExpense.ReceiptFilePath) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task ManageCategoriesAsync()
    {
        var vm = new ExpenseCategoryManagerViewModel(_apiClient);
        var dialog = new ExpenseCategoryManagerDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        dialog.ShowDialog();

        if (vm.HasChanges)
            await LoadDataAsync();
    }
}

/// <summary>Wrapper for the job filter dropdown to support "All" and "General Only" options.</summary>
public class JobFilterItem
{
    public string DisplayName { get; set; } = "";
    public int? JobId { get; set; }
    public bool IsGeneral { get; set; }

    public override string ToString() => DisplayName;
}
