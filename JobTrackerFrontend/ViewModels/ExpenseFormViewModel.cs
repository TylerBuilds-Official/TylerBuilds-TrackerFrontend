using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;

namespace JobTrackerFrontend.ViewModels;

public partial class ExpenseFormViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private readonly int? _editingId;

    [ObservableProperty] private string _windowTitle = "New Expense";

    // Form fields
    [ObservableProperty] private ExpenseCategoryModel? _selectedCategory;
    [ObservableProperty] private JobModel? _selectedJob;
    [ObservableProperty] private string? _vendor;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private decimal? _amount;
    [ObservableProperty] private DateTime? _expenseDate = DateTime.Today;
    [ObservableProperty] private bool _isReimbursable;
    [ObservableProperty] private string? _receiptFilePath;
    [ObservableProperty] private string? _notes;

    // Dropdowns
    [ObservableProperty] private List<ExpenseCategoryModel> _categories = [];
    [ObservableProperty] private List<JobModel> _jobs = [];

    // State
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _hasReceiptFile;

    public bool IsEdit => _editingId.HasValue;
    public bool Saved { get; private set; }

    // Track IDs for matching after async load
    private int? _initialCategoryId;
    private int? _initialJobId;

    /// <summary>Create mode.</summary>
    public ExpenseFormViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        _editingId = null;
    }

    /// <summary>Edit mode — populate from existing expense.</summary>
    public ExpenseFormViewModel(ApiClient apiClient, ExpenseModel expense)
    {
        _apiClient = apiClient;
        _editingId = expense.Id;
        WindowTitle = $"Edit Expense — {expense.Vendor}";

        _initialCategoryId = expense.CategoryId;
        _initialJobId = expense.JobId;

        Vendor = expense.Vendor;
        Description = expense.Description;
        Amount = expense.Amount;
        ExpenseDate = expense.ExpenseDate;
        IsReimbursable = expense.IsReimbursable;
        ReceiptFilePath = expense.ReceiptFilePath;
        HasReceiptFile = !string.IsNullOrEmpty(expense.ReceiptFilePath);
        Notes = expense.Notes;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            var cats = await _apiClient.GetAsync<List<ExpenseCategoryModel>>("/expenses/categories");
            Categories = (cats ?? []).Where(c => c.IsActive).ToList();

            var jobs = await _apiClient.GetAsync<List<JobModel>>("/jobs");
            Jobs = jobs ?? [];

            // Match selections after load
            if (_initialCategoryId.HasValue)
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == _initialCategoryId.Value);

            if (_initialJobId.HasValue)
                SelectedJob = Jobs.FirstOrDefault(j => j.Id == _initialJobId.Value);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load data: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAsync(System.Windows.Window window)
    {
        if (SelectedCategory is null)
        {
            ErrorMessage = "Category is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Vendor))
        {
            ErrorMessage = "Vendor is required.";
            return;
        }

        if (!Amount.HasValue || Amount.Value <= 0)
        {
            ErrorMessage = "Amount must be greater than zero.";
            return;
        }

        if (!ExpenseDate.HasValue)
        {
            ErrorMessage = "Date is required.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            if (IsEdit)
            {
                var request = new UpdateExpenseRequest
                {
                    JobId = SelectedJob?.Id,
                    CategoryId = SelectedCategory.Id,
                    Vendor = Vendor!.Trim(),
                    Description = Description?.Trim(),
                    Amount = Amount.Value,
                    ExpenseDate = ExpenseDate.Value,
                    IsReimbursable = IsReimbursable,
                    ReceiptFilePath = ReceiptFilePath?.Trim(),
                    Notes = Notes?.Trim()
                };
                await _apiClient.PutAsync<UpdateExpenseRequest, ExpenseModel>($"/expenses/{_editingId}", request);
            }
            else
            {
                var request = new CreateExpenseRequest
                {
                    JobId = SelectedJob?.Id,
                    CategoryId = SelectedCategory.Id,
                    Vendor = Vendor!.Trim(),
                    Description = Description?.Trim(),
                    Amount = Amount.Value,
                    ExpenseDate = ExpenseDate.Value,
                    IsReimbursable = IsReimbursable,
                    ReceiptFilePath = ReceiptFilePath?.Trim(),
                    Notes = Notes?.Trim()
                };
                await _apiClient.PostAsync<CreateExpenseRequest, ExpenseModel>("/expenses", request);
            }

            Saved = true;
            window.DialogResult = true;
            window.Close();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Save failed: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void BrowseReceipt()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "All Files (*.*)|*.*|PDF Files (*.pdf)|*.pdf|Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            Title = "Select Receipt File"
        };

        if (dialog.ShowDialog() == true)
        {
            ReceiptFilePath = dialog.FileName;
            HasReceiptFile = true;
        }
    }

    [RelayCommand]
    private static void Cancel(System.Windows.Window window)
    {
        window.DialogResult = false;
        window.Close();
    }
}
