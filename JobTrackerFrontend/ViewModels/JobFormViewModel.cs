using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;

namespace JobTrackerFrontend.ViewModels;

public partial class JobFormViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private readonly int? _editingId;

    [ObservableProperty] private string _windowTitle = "New Job";
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string _status = "Lead";
    [ObservableProperty] private string? _billingType;
    [ObservableProperty] private decimal? _hourlyRate;
    [ObservableProperty] private decimal? _fixedPrice;
    [ObservableProperty] private decimal? _retainerAmount;
    [ObservableProperty] private decimal? _estimatedValue;
    [ObservableProperty] private DateTime? _startDate;
    [ObservableProperty] private DateTime? _endDate;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private ClientModel? _selectedClient;
    [ObservableProperty] private ContactModel? _selectedContact;
    [ObservableProperty] private List<ClientModel> _clients = [];
    [ObservableProperty] private List<ContactModel> _contacts = [];

    public bool IsEdit => _editingId.HasValue;
    public bool Saved { get; private set; }

    public List<string> Statuses { get; } = ["Lead", "Proposal", "Active", "Completed", "Invoiced"];
    public List<string> BillingTypes { get; } = ["Hourly", "Fixed", "Retainer"];

    // Track IDs for matching after async loads
    private int? _initialClientId;
    private int? _initialContactId;

    /// <summary>Create mode.</summary>
    public JobFormViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        _editingId = null;
    }

    /// <summary>Edit mode — populate from existing job.</summary>
    public JobFormViewModel(ApiClient apiClient, JobModel job)
    {
        _apiClient = apiClient;
        _editingId = job.Id;
        WindowTitle = $"Edit Job — {job.Title}";

        _initialClientId = job.ClientId;
        _initialContactId = job.PrimaryContactId;

        Title = job.Title;
        Description = job.Description;
        Status = job.Status;
        BillingType = job.BillingType;
        HourlyRate = job.HourlyRate;
        FixedPrice = job.FixedPrice;
        RetainerAmount = job.RetainerAmount;
        EstimatedValue = job.EstimatedValue;
        StartDate = job.StartDate;
        EndDate = job.EndDate;
        Notes = job.Notes;
    }

    [RelayCommand]
    private async Task LoadClientsAsync()
    {
        try
        {
            var data = await _apiClient.GetAsync<List<ClientModel>>("/clients?active_only=true");
            Clients = data ?? [];

            // In edit mode, match the client after loading
            if (_initialClientId.HasValue)
            {
                SelectedClient = Clients.FirstOrDefault(c => c.Id == _initialClientId.Value);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load clients: {ex.Message}";
        }
    }

    partial void OnSelectedClientChanged(ClientModel? value)
    {
        // Clear contact selection and reload contacts for the new client
        SelectedContact = null;
        Contacts = [];

        if (value is not null)
        {
            _ = LoadContactsAsync(value.Id);
        }
    }

    private async Task LoadContactsAsync(int clientId)
    {
        try
        {
            var data = await _apiClient.GetAsync<List<ContactModel>>($"/contacts/by-client/{clientId}");
            Contacts = data ?? [];

            // In edit mode, match the contact after loading
            if (_initialContactId.HasValue)
            {
                SelectedContact = Contacts.FirstOrDefault(c => c.Id == _initialContactId.Value);
                _initialContactId = null; // Only match once
            }
        }
        catch
        {
            Contacts = [];
        }
    }

    [RelayCommand]
    private async Task SaveAsync(System.Windows.Window window)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = "Job title is required.";
            return;
        }

        if (SelectedClient is null)
        {
            ErrorMessage = "Client is required.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            if (IsEdit)
            {
                var request = new UpdateJobRequest
                {
                    ClientId = SelectedClient.Id,
                    PrimaryContactId = SelectedContact?.Id,
                    Title = Title.Trim(),
                    Description = Description?.Trim(),
                    BillingType = BillingType,
                    HourlyRate = HourlyRate,
                    FixedPrice = FixedPrice,
                    RetainerAmount = RetainerAmount,
                    EstimatedValue = EstimatedValue,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    Notes = Notes?.Trim()
                };
                await _apiClient.PutAsync<UpdateJobRequest, JobModel>($"/jobs/{_editingId}", request);
            }
            else
            {
                var request = new CreateJobRequest
                {
                    ClientId = SelectedClient.Id,
                    PrimaryContactId = SelectedContact?.Id,
                    Title = Title.Trim(),
                    Description = Description?.Trim(),
                    Status = Status,
                    BillingType = BillingType,
                    HourlyRate = HourlyRate,
                    FixedPrice = FixedPrice,
                    RetainerAmount = RetainerAmount,
                    EstimatedValue = EstimatedValue,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    Notes = Notes?.Trim()
                };
                await _apiClient.PostAsync<CreateJobRequest, JobModel>("/jobs", request);
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
