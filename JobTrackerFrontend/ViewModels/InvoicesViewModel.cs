using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;
using JobTrackerFrontend.Views;

namespace JobTrackerFrontend.ViewModels;

public partial class InvoicesViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;

    [ObservableProperty] private ObservableCollection<InvoiceModel> _invoices = [];
    [ObservableProperty] private InvoiceModel? _selectedInvoice;
    [ObservableProperty] private string? _statusFilter;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    public InvoicesViewModel(ApiClient apiClient)
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
            var endpoint = string.IsNullOrEmpty(StatusFilter)
                ? "/invoices"
                : $"/invoices?status={StatusFilter}";

            var data = await _apiClient.GetAsync<List<InvoiceModel>>(endpoint);
            Invoices = new ObservableCollection<InvoiceModel>(data ?? []);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load invoices: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NewInvoiceAsync()
    {
        var vm = new InvoiceFormViewModel(_apiClient);
        var dialog = new InvoiceFormDialog
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
    private async Task EditInvoiceAsync()
    {
        if (SelectedInvoice is null) return;

        var invoice = await _apiClient.GetAsync<InvoiceModel>($"/invoices/{SelectedInvoice.Id}");
        if (invoice is null) return;

        var vm = new InvoiceFormViewModel(_apiClient, invoice);
        var dialog = new InvoiceFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadDataAsync();
        }
    }
}
