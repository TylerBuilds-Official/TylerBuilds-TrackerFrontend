using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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

    // Payment fields
    [ObservableProperty] private decimal? _paymentAmount;
    [ObservableProperty] private DateTime? _paymentDate = DateTime.Today;
    [ObservableProperty] private string? _paymentNotes;
    [ObservableProperty] private decimal _balanceRemaining;
    [ObservableProperty] private decimal _totalPaid;
    [ObservableProperty] private ObservableCollection<PaymentModel> _payments = [];
    [ObservableProperty] private bool _isRecordingPayment;
    [ObservableProperty] private string? _paymentErrorMessage;

    [ObservableProperty] private string? _networkFilePath;
    [ObservableProperty] private bool _hasInvoiceFile;

    public bool IsEdit => _editingId.HasValue;
    public bool Saved { get; private set; }
    public bool IsFullyPaid => BalanceRemaining <= 0;

    public List<string> Statuses { get; } = ["Draft", "Sent", "Partially Paid", "Paid", "Overdue", "Cancelled"];

    // Track JobId for matching after async load
    private int? _initialJobId;
    private int? _preSelectedClientId;
    private decimal _invoiceAmount;

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

    /// <summary>Create mode with pre-selected job.</summary>
    public InvoiceFormViewModel(ApiClient apiClient, int jobId, bool isJobPreselect)
    {
        _apiClient = apiClient;
        _editingId = null;
        _initialJobId = jobId;
    }

    /// <summary>Edit mode — populate from existing invoice.</summary>
    public InvoiceFormViewModel(ApiClient apiClient, InvoiceModel invoice)
    {
        _apiClient = apiClient;
        _editingId = invoice.Id;
        WindowTitle = $"Edit Invoice — {invoice.DisplayNumber}";

        _initialJobId = invoice.JobId;
        _invoiceAmount = invoice.Amount;

        Amount = invoice.Amount;
        IssuedDate = invoice.IssuedDate;
        DueDate = invoice.DueDate;
        Notes = invoice.Notes;
        SelectedStatus = invoice.Status;
        TotalPaid = invoice.TotalPaid;
        BalanceRemaining = invoice.BalanceRemaining;
        NetworkFilePath = invoice.NetworkFilePath;
        HasInvoiceFile = !string.IsNullOrEmpty(invoice.NetworkFilePath);
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

            // Load payments in edit mode
            if (IsEdit)
            {
                await LoadPaymentsAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load jobs: {ex.Message}";
        }
    }

    private async Task LoadPaymentsAsync()
    {
        try
        {
            var data = await _apiClient.GetAsync<List<PaymentModel>>($"/invoices/{_editingId}/payments");
            Payments = new ObservableCollection<PaymentModel>(data ?? []);
        }
        catch
        {
            // Non-critical
        }
    }

    [RelayCommand]
    private async Task RecordPaymentAsync()
    {
        PaymentErrorMessage = null;

        if (!PaymentAmount.HasValue || PaymentAmount.Value <= 0)
        {
            PaymentErrorMessage = "Payment amount must be greater than zero.";
            return;
        }

        if (PaymentAmount.Value > BalanceRemaining)
        {
            PaymentErrorMessage = $"Payment cannot exceed remaining balance of {BalanceRemaining:C}.";
            return;
        }

        if (!PaymentDate.HasValue)
        {
            PaymentErrorMessage = "Payment date is required.";
            return;
        }

        IsRecordingPayment = true;
        try
        {
            var request = new CreatePaymentRequest
            {
                Amount = PaymentAmount.Value,
                PaidDate = PaymentDate.Value,
                Notes = PaymentNotes?.Trim()
            };
            await _apiClient.PostAsync<CreatePaymentRequest, PaymentModel>(
                $"/invoices/{_editingId}/payments", request);

            // Refresh state
            TotalPaid += PaymentAmount.Value;
            BalanceRemaining = _invoiceAmount - TotalPaid;
            OnPropertyChanged(nameof(IsFullyPaid));

            // Auto-update displayed status
            SelectedStatus = BalanceRemaining <= 0 ? "Paid" : "Partially Paid";

            // Reset payment fields
            PaymentAmount = null;
            PaymentNotes = null;
            PaymentDate = DateTime.Today;

            // Reload payment history
            await LoadPaymentsAsync();

            // Mark as saved so parent refreshes
            Saved = true;
        }
        catch (Exception ex)
        {
            PaymentErrorMessage = $"Payment failed: {ex.Message}";
        }
        finally
        {
            IsRecordingPayment = false;
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
                var created = await _apiClient.PostAsync<CreateInvoiceRequest, InvoiceModel>("/invoices", request);

                // Copy template and save file path
                if (created != null)
                    await CopyInvoiceTemplateAsync(created.Id, created.DisplayNumber);
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

    private async Task CopyInvoiceTemplateAsync(int invoiceId, string displayNumber)
    {
        try
        {
            var templatePath = AppConfig.InvoiceTemplatePath;
            var outputFolder = AppConfig.InvoiceOutputFolder;

            if (string.IsNullOrEmpty(templatePath) || string.IsNullOrEmpty(outputFolder))
                return;

            // Find the template file (try with and without extension)
            var sourcePath = File.Exists(templatePath) ? templatePath : templatePath + ".docx";
            if (!File.Exists(sourcePath))
                return;

            var fileName = $"{displayNumber}.docx";
            var destPath = Path.Combine(outputFolder, fileName);

            File.Copy(sourcePath, destPath, overwrite: false);

            // Save file path to DB
            await _apiClient.PatchAsync<SetFilePathRequest, InvoiceModel>(
                $"/invoices/{invoiceId}/file-path",
                new SetFilePathRequest { NetworkFilePath = destPath });
        }
        catch
        {
            // Non-critical — invoice was still created
        }
    }

    [RelayCommand]
    private void OpenInvoiceFile()
    {
        if (string.IsNullOrEmpty(NetworkFilePath) || !File.Exists(NetworkFilePath))
            return;

        Process.Start(new ProcessStartInfo(NetworkFilePath) { UseShellExecute = true });
    }

    [RelayCommand]
    private static void Cancel(System.Windows.Window window)
    {
        window.DialogResult = false;
        window.Close();
    }
}
