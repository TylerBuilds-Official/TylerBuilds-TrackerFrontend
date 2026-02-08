using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;

namespace JobTrackerFrontend.ViewModels;

public partial class ClientFormViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private readonly int? _editingId;

    [ObservableProperty] private string _windowTitle = "New Client";
    [ObservableProperty] private string _type = "Company";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string? _website;
    [ObservableProperty] private string? _addressLine1;
    [ObservableProperty] private string? _addressLine2;
    [ObservableProperty] private string? _city;
    [ObservableProperty] private string? _state;
    [ObservableProperty] private string? _zip;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private bool _isIndividual;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string? _errorMessage;

    public bool IsEdit => _editingId.HasValue;
    public bool Saved { get; private set; }

    public List<string> ClientTypes { get; } = ["Company", "Individual"];

    partial void OnTypeChanged(string value)
    {
        IsIndividual = value == "Individual";
    }

    /// <summary>Create mode.</summary>
    public ClientFormViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        _editingId = null;
        WindowTitle = "New Client";
    }

    /// <summary>Edit mode — populate from existing client.</summary>
    public ClientFormViewModel(ApiClient apiClient, ClientModel client)
    {
        _apiClient = apiClient;
        _editingId = client.Id;
        WindowTitle = $"Edit Client — {client.Name}";

        Type = client.Type;
        Name = client.Name;
        Website = client.Website;
        AddressLine1 = client.AddressLine1;
        AddressLine2 = client.AddressLine2;
        City = client.City;
        State = client.State;
        Zip = client.Zip;
        Notes = client.Notes;
        Email = client.PrimaryContactEmail;
        Phone = client.PrimaryContactPhone;
        IsIndividual = client.Type == "Individual";
    }

    [RelayCommand]
    private async Task SaveAsync(System.Windows.Window window)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Client name is required.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            if (IsEdit)
            {
                var request = new UpdateClientRequest
                {
                    Type = Type,
                    Name = Name.Trim(),
                    Website = Website?.Trim(),
                    AddressLine1 = AddressLine1?.Trim(),
                    AddressLine2 = AddressLine2?.Trim(),
                    City = City?.Trim(),
                    State = State?.Trim(),
                    Zip = Zip?.Trim(),
                    Notes = Notes?.Trim(),
                    Email = IsIndividual ? Email?.Trim() : null,
                    Phone = IsIndividual ? Phone?.Trim() : null,
                };
                await _apiClient.PutAsync<UpdateClientRequest, ClientModel>($"/clients/{_editingId}", request);
            }
            else
            {
                var request = new CreateClientRequest
                {
                    Type = Type,
                    Name = Name.Trim(),
                    Website = Website?.Trim(),
                    AddressLine1 = AddressLine1?.Trim(),
                    AddressLine2 = AddressLine2?.Trim(),
                    City = City?.Trim(),
                    State = State?.Trim(),
                    Zip = Zip?.Trim(),
                    Notes = Notes?.Trim(),
                    Email = IsIndividual ? Email?.Trim() : null,
                    Phone = IsIndividual ? Phone?.Trim() : null,
                };
                await _apiClient.PostAsync<CreateClientRequest, ClientModel>("/clients", request);
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
