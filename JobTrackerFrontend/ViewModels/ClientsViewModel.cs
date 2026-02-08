using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;
using JobTrackerFrontend.Views;

namespace JobTrackerFrontend.ViewModels;

public partial class ClientsViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;

    // List mode
    [ObservableProperty] private ObservableCollection<ClientModel> _clients = [];
    [ObservableProperty] private ClientModel? _selectedClient;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    // Detail mode
    [ObservableProperty] private bool _isDetailMode;
    [ObservableProperty] private ClientModel? _detailClient;
    [ObservableProperty] private ObservableCollection<ContactModel> _contacts = [];
    [ObservableProperty] private ObservableCollection<JobModel> _clientJobs = [];
    [ObservableProperty] private ObservableCollection<InvoiceModel> _clientInvoices = [];
    [ObservableProperty] private ContactModel? _selectedContact;
    [ObservableProperty] private JobModel? _selectedClientJob;
    [ObservableProperty] private InvoiceModel? _selectedClientInvoice;

    public ClientsViewModel(ApiClient apiClient)
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
            var data = await _apiClient.GetAsync<List<ClientModel>>("/clients?active_only=true");
            Clients = new ObservableCollection<ClientModel>(data ?? []);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load clients: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ViewDetailAsync()
    {
        if (SelectedClient is null) return;

        var client = await _apiClient.GetAsync<ClientModel>($"/clients/{SelectedClient.Id}");
        if (client is null) return;

        DetailClient = client;
        IsDetailMode = true;
        await LoadDetailDataAsync();
    }

    [RelayCommand]
    private async Task BackToListAsync()
    {
        IsDetailMode = false;
        DetailClient = null;
        Contacts = [];
        ClientJobs = [];
        ClientInvoices = [];
        await LoadDataAsync();
    }

    // ── Detail Mode ────────────────────────────────────────

    private async Task LoadDetailDataAsync()
    {
        if (DetailClient is null) return;
        IsLoading = true;
        try
        {
            var contactsTask = _apiClient.GetAsync<List<ContactModel>>($"/contacts/by-client/{DetailClient.Id}");
            var jobsTask = _apiClient.GetAsync<List<JobModel>>($"/jobs/by-client/{DetailClient.Id}");
            var invoicesTask = _apiClient.GetAsync<List<InvoiceModel>>($"/invoices/by-client/{DetailClient.Id}");

            await Task.WhenAll(contactsTask, jobsTask, invoicesTask);

            Contacts = new ObservableCollection<ContactModel>(contactsTask.Result ?? []);
            ClientJobs = new ObservableCollection<JobModel>(jobsTask.Result ?? []);
            ClientInvoices = new ObservableCollection<InvoiceModel>(invoicesTask.Result ?? []);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load details: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Client CRUD ────────────────────────────────────────

    [RelayCommand]
    private async Task NewClientAsync()
    {
        var vm = new ClientFormViewModel(_apiClient);
        var dialog = new ClientFormDialog
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
    private async Task EditClientAsync()
    {
        var client = IsDetailMode ? DetailClient : SelectedClient;
        if (client is null) return;

        var fresh = await _apiClient.GetAsync<ClientModel>($"/clients/{client.Id}");
        if (fresh is null) return;

        var vm = new ClientFormViewModel(_apiClient, fresh);
        var dialog = new ClientFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            if (IsDetailMode)
            {
                DetailClient = await _apiClient.GetAsync<ClientModel>($"/clients/{client.Id}");
                await LoadDetailDataAsync();
            }
            else
            {
                await LoadDataAsync();
            }
        }
    }

    [RelayCommand]
    private async Task DeactivateClientAsync()
    {
        var client = IsDetailMode ? DetailClient : SelectedClient;
        if (client is null) return;

        var result = MessageBox.Show(
            $"Deactivate client \"{client.Name}\"?\n\nThis will hide the client from active lists.",
            "Confirm Deactivate",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _apiClient.PatchAsync($"/clients/{client.Id}/deactivate");
                if (IsDetailMode)
                    await BackToListAsync();
                else
                    await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to deactivate: {ex.Message}", "Error");
            }
        }
    }

    // ── Job CRUD (from detail) ──────────────────────────────

    [RelayCommand]
    private async Task NewJobAsync()
    {
        if (DetailClient is null) return;

        var vm = new JobFormViewModel(_apiClient, DetailClient);
        var dialog = new JobFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadDetailDataAsync();
        }
    }

    // ── Invoice CRUD (from detail) ─────────────────────────────

    [RelayCommand]
    private async Task NewInvoiceAsync()
    {
        if (DetailClient is null) return;

        var vm = new InvoiceFormViewModel(_apiClient, DetailClient.Id);
        var dialog = new InvoiceFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadDetailDataAsync();
        }
    }

    // ── Contact CRUD ───────────────────────────────────────

    [RelayCommand]
    private async Task NewContactAsync()
    {
        if (DetailClient is null) return;

        var vm = new ContactFormViewModel(_apiClient, DetailClient.Id);
        var dialog = new ContactFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadDetailDataAsync();
        }
    }

    [RelayCommand]
    private async Task EditContactAsync()
    {
        if (SelectedContact is null) return;

        var vm = new ContactFormViewModel(_apiClient, SelectedContact);
        var dialog = new ContactFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadDetailDataAsync();
        }
    }

    [RelayCommand]
    private async Task RemoveContactAsync()
    {
        if (SelectedContact is null) return;

        var result = MessageBox.Show(
            $"Remove contact \"{SelectedContact.FirstName} {SelectedContact.LastName}\"?",
            "Confirm Remove",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _apiClient.PatchAsync($"/contacts/{SelectedContact.Id}/remove");
                await LoadDetailDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to remove: {ex.Message}", "Error");
            }
        }
    }
}
