using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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

    public List<string> Statuses { get; } = ["Draft", "Sent", "Partially Paid", "Paid", "Overdue", "Cancelled"];

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
    private async Task SetStatusAsync(string status)
    {
        if (SelectedInvoice is null) return;

        try
        {
            var request = new UpdateInvoiceStatusRequest { Status = status };
            await _apiClient.PatchAsync<UpdateInvoiceStatusRequest, InvoiceModel>(
                $"/invoices/{SelectedInvoice.Id}/status", request);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to update status: {ex.Message}", "Error");
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
    private async Task DeleteInvoiceAsync()
    {
        if (SelectedInvoice is null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to permanently delete invoice \"{SelectedInvoice.DisplayNumber}\"?\n\n" +
            "Consider marking it as Cancelled or Paid instead.\nThis action cannot be undone.",
            "Delete Invoice",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            // Delete the network file if it exists
            if (!string.IsNullOrEmpty(SelectedInvoice.NetworkFilePath) && File.Exists(SelectedInvoice.NetworkFilePath))
                File.Delete(SelectedInvoice.NetworkFilePath);

            await _apiClient.DeleteAsync($"/invoices/{SelectedInvoice.Id}");
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete invoice: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private void OpenInvoiceFile()
    {
        if (SelectedInvoice is null || string.IsNullOrEmpty(SelectedInvoice.NetworkFilePath))
            return;

        if (!File.Exists(SelectedInvoice.NetworkFilePath))
        {
            MessageBox.Show("Invoice file not found.", "Error");
            return;
        }

        Process.Start(new ProcessStartInfo(SelectedInvoice.NetworkFilePath) { UseShellExecute = true });
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

    [RelayCommand]
    private async Task RecordPaymentAsync()
    {
        // Opens the edit dialog — payment section is already built in
        await EditInvoiceAsync();
    }

    public bool CanRecordPayment => SelectedInvoice is not null
        && SelectedInvoice.Status is not ("Paid" or "Cancelled" or "Draft")
        && SelectedInvoice.Amount > 0;

    partial void OnSelectedInvoiceChanged(InvoiceModel? value)
    {
        OnPropertyChanged(nameof(CanRecordPayment));
    }
}
