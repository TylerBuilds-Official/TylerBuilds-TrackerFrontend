using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;

namespace JobTrackerFrontend.ViewModels;

public partial class LineItemsManagerViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private readonly int _invoiceId;

    [ObservableProperty] private string _windowTitle = "Line Items";
    [ObservableProperty] private ObservableCollection<LineItemModel> _lineItems = [];
    [ObservableProperty] private LineItemModel? _selectedLineItem;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    // Form fields
    [ObservableProperty] private string? _description;
    [ObservableProperty] private decimal? _quantity = 1;
    [ObservableProperty] private decimal? _unitPrice;

    // Edit state
    [ObservableProperty] private int? _editingId;
    [ObservableProperty] private string _saveButtonText = "Add Item";

    // Summary
    [ObservableProperty] private decimal _grandTotal;

    public bool IsEditing => EditingId.HasValue;
    public bool HasChanges { get; private set; }

    public LineItemsManagerViewModel(ApiClient apiClient, int invoiceId, string invoiceDisplayNumber)
    {
        _apiClient = apiClient;
        _invoiceId = invoiceId;
        WindowTitle = $"Line Items — {invoiceDisplayNumber}";
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var data = await _apiClient.GetAsync<List<LineItemModel>>($"/invoices/{_invoiceId}/line-items");
            LineItems = new ObservableCollection<LineItemModel>(data ?? []);
            RecalculateTotal();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load line items: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveItemAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Description))
        {
            ErrorMessage = "Description is required.";
            return;
        }

        if (!Quantity.HasValue || Quantity.Value <= 0)
        {
            ErrorMessage = "Quantity must be greater than zero.";
            return;
        }

        if (!UnitPrice.HasValue || UnitPrice.Value < 0)
        {
            ErrorMessage = "Unit price is required.";
            return;
        }

        try
        {
            if (IsEditing)
            {
                var request = new UpdateLineItemRequest
                {
                    Description = Description!.Trim(),
                    Quantity = Quantity.Value,
                    UnitPrice = UnitPrice.Value
                };
                await _apiClient.PutAsync(
                    $"/invoices/line-items/{EditingId}", request);
            }
            else
            {
                var request = new CreateLineItemRequest
                {
                    InvoiceId = _invoiceId,
                    Description = Description!.Trim(),
                    Quantity = Quantity.Value,
                    UnitPrice = UnitPrice.Value
                };
                await _apiClient.PostAsync<CreateLineItemRequest, LineItemModel>(
                    "/invoices/line-items", request);
            }

            HasChanges = true;
            ClearForm();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Save failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteItemAsync()
    {
        if (SelectedLineItem is null) return;

        var result = MessageBox.Show(
            $"Delete line item \"{SelectedLineItem.Description}\"?",
            "Delete Line Item",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _apiClient.DeleteAsync($"/invoices/line-items/{SelectedLineItem.Id}");
            HasChanges = true;

            // If we were editing this item, clear the form
            if (EditingId == SelectedLineItem.Id)
                ClearForm();

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Delete failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void EditItem()
    {
        if (SelectedLineItem is null) return;

        EditingId = SelectedLineItem.Id;
        Description = SelectedLineItem.Description;
        Quantity = SelectedLineItem.Quantity;
        UnitPrice = SelectedLineItem.UnitPrice;
        SaveButtonText = "Update Item";
        OnPropertyChanged(nameof(IsEditing));
    }

    [RelayCommand]
    private void CancelEdit()
    {
        ClearForm();
    }

    [RelayCommand]
    private static void Close(Window window)
    {
        window.DialogResult = true;
        window.Close();
    }

    private void ClearForm()
    {
        EditingId = null;
        Description = null;
        Quantity = 1;
        UnitPrice = null;
        ErrorMessage = null;
        SaveButtonText = "Add Item";
        OnPropertyChanged(nameof(IsEditing));
    }

    private void RecalculateTotal()
    {
        GrandTotal = LineItems.Sum(li => li.LineTotal);
    }
}
