using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;

namespace JobTrackerFrontend.ViewModels;

public partial class ResearchFormViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private readonly int? _editingId;

    [ObservableProperty] private string _windowTitle = "New Research Lead";

    // Form fields
    [ObservableProperty] private ClientModel? _selectedClient;
    [ObservableProperty] private ContactModel? _selectedContact;
    [ObservableProperty] private ResearchThemeModel? _selectedTheme;
    [ObservableProperty] private string? _businessName;
    [ObservableProperty] private DateTime? _dateContacted = DateTime.Today;
    [ObservableProperty] private string? _painPoints;
    [ObservableProperty] private int _interestLevel = 3;
    [ObservableProperty] private DateTime? _followUpDate;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private int _wouldPay;
    [ObservableProperty] private int _status;

    // Dropdowns
    [ObservableProperty] private List<ClientModel> _clients = [];
    [ObservableProperty] private List<ContactModel> _contacts = [];
    [ObservableProperty] private List<ResearchThemeModel> _themes = [];

    // State
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string? _errorMessage;

    public bool IsEdit => _editingId.HasValue;
    public bool Saved { get; private set; }

    public List<string> WouldPayOptions { get; } = ["No", "Maybe", "Yes"];
    public List<string> StatusOptions { get; } = ["New", "Contacted", "Following Up", "Converted", "Closed"];
    public List<int> InterestLevelOptions { get; } = [1, 2, 3, 4, 5];

    public string SelectedWouldPayDisplay
    {
        get => WouldPayOptions[WouldPay];
        set => WouldPay = WouldPayOptions.IndexOf(value);
    }

    public string SelectedStatusDisplay
    {
        get => StatusOptions[Status];
        set => Status = StatusOptions.IndexOf(value);
    }

    // Track IDs for matching after async load
    private int? _initialClientId;
    private int? _initialContactId;
    private int? _initialThemeId;

    /// <summary>Create mode.</summary>
    public ResearchFormViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        _editingId = null;
    }

    /// <summary>Edit mode — populate from existing lead.</summary>
    public ResearchFormViewModel(ApiClient apiClient, ResearchLeadModel lead)
    {
        _apiClient = apiClient;
        _editingId = lead.ResearchLeadId;
        WindowTitle = $"Edit Lead — {lead.BusinessName}";

        _initialClientId = lead.ClientId;
        _initialContactId = lead.ContactId;
        _initialThemeId = lead.ResearchThemeId;

        BusinessName = lead.BusinessName;
        DateContacted = lead.DateContacted;
        PainPoints = lead.PainPoints;
        InterestLevel = lead.InterestLevel;
        FollowUpDate = lead.FollowUpDate;
        Notes = lead.Notes;
        WouldPay = lead.WouldPay;
        Status = lead.Status;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            var clientsTask = _apiClient.GetAsync<List<ClientModel>>("/clients");
            var themesTask = _apiClient.GetAsync<List<ResearchThemeModel>>("/research/themes");

            await Task.WhenAll(clientsTask, themesTask);

            Clients = clientsTask.Result ?? [];
            Themes = themesTask.Result ?? [];

            // Match selections after load
            if (_initialClientId.HasValue)
            {
                SelectedClient = Clients.FirstOrDefault(c => c.Id == _initialClientId.Value);
                // Contacts will load via OnSelectedClientChanged
            }

            if (_initialThemeId.HasValue)
                SelectedTheme = Themes.FirstOrDefault(t => t.ResearchThemeId == _initialThemeId.Value);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load data: {ex.Message}";
        }
    }

    /// <summary>When a client is selected, sync businessName and load their contacts.</summary>
    async partial void OnSelectedClientChanged(ClientModel? value)
    {
        if (value is not null)
        {
            BusinessName = value.Name;

            try
            {
                var contacts = await _apiClient.GetAsync<List<ContactModel>>($"/contacts/by-client/{value.Id}");
                Contacts = contacts ?? [];

                // Match initial contact if loading for edit
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
        else
        {
            Contacts = [];
            SelectedContact = null;
        }
    }

    [RelayCommand]
    private async Task SaveAsync(System.Windows.Window window)
    {
        if (string.IsNullOrWhiteSpace(BusinessName))
        {
            ErrorMessage = "Business name is required.";
            return;
        }

        if (!DateContacted.HasValue)
        {
            ErrorMessage = "Date contacted is required.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            if (IsEdit)
            {
                var request = new UpdateResearchLeadRequest
                {
                    ClientId = SelectedClient?.Id,
                    ContactId = SelectedContact?.Id,
                    ResearchThemeId = SelectedTheme?.ResearchThemeId,
                    BusinessName = BusinessName!.Trim(),
                    DateContacted = DateContacted.Value,
                    PainPoints = PainPoints?.Trim(),
                    InterestLevel = InterestLevel,
                    FollowUpDate = FollowUpDate,
                    Notes = Notes?.Trim(),
                    WouldPay = WouldPay,
                    Status = Status
                };
                await _apiClient.PutAsync<UpdateResearchLeadRequest, ResearchLeadModel>(
                    $"/research/{_editingId}", request);
            }
            else
            {
                var request = new CreateResearchLeadRequest
                {
                    ClientId = SelectedClient?.Id,
                    ContactId = SelectedContact?.Id,
                    ResearchThemeId = SelectedTheme?.ResearchThemeId,
                    BusinessName = BusinessName!.Trim(),
                    DateContacted = DateContacted.Value,
                    PainPoints = PainPoints?.Trim(),
                    InterestLevel = InterestLevel,
                    FollowUpDate = FollowUpDate,
                    Notes = Notes?.Trim(),
                    WouldPay = WouldPay,
                    Status = Status
                };
                await _apiClient.PostAsync<CreateResearchLeadRequest, ResearchLeadModel>("/research", request);
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
