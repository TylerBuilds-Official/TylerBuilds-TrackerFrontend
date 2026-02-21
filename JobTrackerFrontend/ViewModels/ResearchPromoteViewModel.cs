using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;

namespace JobTrackerFrontend.ViewModels;

public partial class ResearchPromoteViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private readonly int _leadId;

    [ObservableProperty] private string _windowTitle = "Promote to Client";

    // Client fields
    [ObservableProperty] private string _clientType = "Company";
    [ObservableProperty] private string? _clientName;
    [ObservableProperty] private string? _website;
    [ObservableProperty] private string? _addressLine1;
    [ObservableProperty] private string? _addressLine2;
    [ObservableProperty] private string? _city;
    [ObservableProperty] private string? _state;
    [ObservableProperty] private string? _zip;
    [ObservableProperty] private string? _clientNotes;

    // Contact fields
    [ObservableProperty] private string? _contactFirstName;
    [ObservableProperty] private string? _contactLastName;
    [ObservableProperty] private string? _contactEmail;
    [ObservableProperty] private string? _contactPhone;
    [ObservableProperty] private string? _contactJobTitle;

    // State
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string? _errorMessage;

    public bool Saved { get; private set; }
    public List<string> ClientTypes { get; } = ["Company", "Individual"];

    public bool IsIndividual => ClientType == "Individual";

    partial void OnClientTypeChanged(string value) => OnPropertyChanged(nameof(IsIndividual));

    /// <summary>Pre-fill from lead data.</summary>
    public ResearchPromoteViewModel(ApiClient apiClient, ResearchLeadModel lead)
    {
        _apiClient = apiClient;
        _leadId = lead.ResearchLeadId;
        WindowTitle = $"Promote — {lead.BusinessName}";

        ClientName = lead.BusinessName;
        ContactFirstName = lead.ContactFirstName;
        ContactLastName = lead.ContactLastName;
        ContactEmail = lead.ContactEmail;
        ContactPhone = lead.ContactPhone;
    }

    [RelayCommand]
    private async Task SaveAsync(System.Windows.Window window)
    {
        if (string.IsNullOrWhiteSpace(ClientName))
        {
            ErrorMessage = "Client name is required.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var request = new PromoteLeadRequest
            {
                ClientType = ClientType,
                ClientName = ClientName!.Trim(),
                Website = Website?.Trim(),
                AddressLine1 = AddressLine1?.Trim(),
                AddressLine2 = AddressLine2?.Trim(),
                City = City?.Trim(),
                State = State?.Trim(),
                Zip = Zip?.Trim(),
                ClientNotes = ClientNotes?.Trim(),
                ContactFirstName = ContactFirstName?.Trim(),
                ContactLastName = ContactLastName?.Trim(),
                ContactEmail = ContactEmail?.Trim(),
                ContactPhone = ContactPhone?.Trim(),
                ContactJobTitle = ContactJobTitle?.Trim()
            };

            await _apiClient.PostAsync<PromoteLeadRequest, ResearchLeadModel>(
                $"/research/{_leadId}/promote", request);

            Saved = true;
            window.DialogResult = true;
            window.Close();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Promote failed: {ex.Message}";
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
