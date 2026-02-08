using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;

namespace JobTrackerFrontend.ViewModels;

public partial class InvoiceFormViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private readonly int? _editingId;

    [ObservableProperty] private string _windowTitle = "New Invoice";
    [ObservableProperty] private decimal? _amount;
    [ObservableProperty] private DateTime? _issuedDate;
    [ObservableProperty] private DateTime? _dueDate;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string? _selectedStatus;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private JobModel? _selectedJob;
    [ObservableProperty] private List<JobModel> _jobs = [];

    public bool IsEdit => _editingId.HasValue;
    public bool Saved { get; private set; }

    public List<string> Statuses { get; } = ["Draft", "Sent", "Paid", "Overdue", "Cancelled"];

    // Track JobId for matching after async load
    private int? _initialJobId;
    private int? _preSelectedClientId;

    /// <summary>Create mode.</summary>
    public InvoiceFormViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        _editingId = null;
    }

    /// <summary>Create mode with pre-filtered client jobs.</summary>
    public InvoiceFormViewModel(ApiClient apiClient, int clientId)
    {
        _apiClient = apiClient;
        _editingId = null;
        _preSelectedClientId = clientId;
    }

    /// <summary>Edit mode — populate from existing invoice.</summary>
    public InvoiceFormViewModel(ApiClient apiClient, InvoiceModel invoice)
    {
        _apiClient = apiClient;
        _editingId = invoice.Id;
        WindowTitle = $"Edit Invoice — {invoice.DisplayNumber}";

        _initialJobId = invoice.JobId;

        Amount = invoice.Amount;
        IssuedDate = invoice.IssuedDate;
        DueDate = invoice.DueDate;
        Notes = invoice.Notes;
        SelectedStatus = invoice.Status;
    }

    [RelayCommand]
    private async Task LoadJobsAsync()
    {
        try
        {
            var endpoint = _preSelectedClientId.HasValue
                ? $"/jobs/by-client/{_preSelectedClientId.Value}"
                : "/jobs";
            var data = await _apiClient.GetAsync<List<JobModel>>(endpoint);
            Jobs = data ?? [];

            // In edit mode, match the job after loading
            if (_initialJobId.HasValue)
            {
                SelectedJob = Jobs.FirstOrDefault(j => j.Id == _initialJobId.Value);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load jobs: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAsync(System.Windows.Window window)
    {
        if (SelectedJob is null)
        {
            ErrorMessage = "Job is required.";
            return;
        }

        if (!Amount.HasValue || Amount.Value <= 0)
        {
            ErrorMessage = "Amount must be greater than zero.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            if (IsEdit)
            {
                var request = new UpdateInvoiceRequest
                {
                    Amount = Amount.Value,
                    IssuedDate = IssuedDate,
                    DueDate = DueDate,
                    Notes = Notes?.Trim()
                };
                await _apiClient.PutAsync<UpdateInvoiceRequest, InvoiceModel>($"/invoices/{_editingId}", request);

                // If status changed, update via separate endpoint
                if (SelectedStatus is not null)
                {
                    var statusRequest = new UpdateInvoiceStatusRequest { Status = SelectedStatus };
                    await _apiClient.PatchAsync<UpdateInvoiceStatusRequest, InvoiceModel>(
                        $"/invoices/{_editingId}/status", statusRequest);
                }
            }
            else
            {
                var request = new CreateInvoiceRequest
                {
                    JobId = SelectedJob.Id,
                    Amount = Amount.Value,
                    IssuedDate = IssuedDate,
                    DueDate = DueDate,
                    Notes = Notes?.Trim()
                };
                await _apiClient.PostAsync<CreateInvoiceRequest, InvoiceModel>("/invoices", request);
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
    private static void Cancel(System.Windows.Window window)
    {
        window.DialogResult = false;
        window.Close();
    }
}
