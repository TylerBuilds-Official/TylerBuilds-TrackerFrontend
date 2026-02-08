using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;

namespace JobTrackerFrontend.ViewModels;

public partial class ContactFormViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private readonly int _clientId;
    private readonly int? _editingId;

    [ObservableProperty] private string _windowTitle = "New Contact";
    [ObservableProperty] private string _firstName = "";
    [ObservableProperty] private string _lastName = "";
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _jobTitle;
    [ObservableProperty] private bool _isPrimary;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string? _errorMessage;

    public bool IsEdit => _editingId.HasValue;
    public bool Saved { get; private set; }

    /// <summary>Create mode.</summary>
    public ContactFormViewModel(ApiClient apiClient, int clientId)
    {
        _apiClient = apiClient;
        _clientId = clientId;
        _editingId = null;
        WindowTitle = "New Contact";
    }

    /// <summary>Edit mode.</summary>
    public ContactFormViewModel(ApiClient apiClient, ContactModel contact)
    {
        _apiClient = apiClient;
        _clientId = contact.ClientId;
        _editingId = contact.Id;
        WindowTitle = $"Edit Contact — {contact.FirstName} {contact.LastName}";

        FirstName = contact.FirstName;
        LastName = contact.LastName;
        Email = contact.Email;
        Phone = contact.Phone;
        JobTitle = contact.JobTitle;
        IsPrimary = contact.IsPrimary;
    }

    [RelayCommand]
    private async Task SaveAsync(System.Windows.Window window)
    {
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            ErrorMessage = "First name is required.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            if (IsEdit)
            {
                var request = new UpdateContactRequest
                {
                    FirstName = FirstName.Trim(),
                    LastName = LastName.Trim(),
                    Email = Email?.Trim(),
                    Phone = Phone?.Trim(),
                    JobTitle = JobTitle?.Trim(),
                    IsPrimary = IsPrimary,
                };
                await _apiClient.PutAsync<UpdateContactRequest, ContactModel>($"/contacts/{_editingId}", request);
            }
            else
            {
                var request = new CreateContactRequest
                {
                    ClientId = _clientId,
                    FirstName = FirstName.Trim(),
                    LastName = LastName.Trim(),
                    Email = Email?.Trim(),
                    Phone = Phone?.Trim(),
                    JobTitle = JobTitle?.Trim(),
                    IsPrimary = IsPrimary,
                };
                await _apiClient.PostAsync<CreateContactRequest, ContactModel>("/contacts", request);
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
