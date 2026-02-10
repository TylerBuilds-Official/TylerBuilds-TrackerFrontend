using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;

namespace JobTrackerFrontend.ViewModels;

public partial class ExpenseCategoryManagerViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;

    [ObservableProperty] private ObservableCollection<ExpenseCategoryModel> _categories = [];
    [ObservableProperty] private ExpenseCategoryModel? _selectedCategory;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    // Form fields
    [ObservableProperty] private string? _name;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private bool _isActive = true;

    // Edit state
    [ObservableProperty] private int? _editingId;
    [ObservableProperty] private string _saveButtonText = "Add Category";

    public bool IsEditing => EditingId.HasValue;
    public bool HasChanges { get; private set; }

    public ExpenseCategoryManagerViewModel(ApiClient apiClient)
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
            var data = await _apiClient.GetAsync<List<ExpenseCategoryModel>>("/expenses/categories");
            Categories = new ObservableCollection<ExpenseCategoryModel>(data ?? []);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load categories: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveCategoryAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Name is required.";
            return;
        }

        try
        {
            if (IsEditing)
            {
                var request = new UpdateExpenseCategoryRequest
                {
                    Name = Name!.Trim(),
                    Description = Description?.Trim(),
                    IsActive = IsActive
                };
                await _apiClient.PutAsync<UpdateExpenseCategoryRequest, ExpenseCategoryModel>(
                    $"/expenses/categories/{EditingId}", request);
            }
            else
            {
                var request = new CreateExpenseCategoryRequest
                {
                    Name = Name!.Trim(),
                    Description = Description?.Trim()
                };
                await _apiClient.PostAsync<CreateExpenseCategoryRequest, ExpenseCategoryModel>(
                    "/expenses/categories", request);
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
    private async Task DeleteCategoryAsync()
    {
        if (SelectedCategory is null) return;

        var result = MessageBox.Show(
            $"Delete category \"{SelectedCategory.Name}\"?\n\n" +
            "This will fail if any expenses are using this category.",
            "Delete Category",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _apiClient.DeleteAsync($"/expenses/categories/{SelectedCategory.Id}");
            HasChanges = true;

            if (EditingId == SelectedCategory.Id)
                ClearForm();

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Delete failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void EditCategory()
    {
        if (SelectedCategory is null) return;

        EditingId = SelectedCategory.Id;
        Name = SelectedCategory.Name;
        Description = SelectedCategory.Description;
        IsActive = SelectedCategory.IsActive;
        SaveButtonText = "Update Category";
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
        Name = null;
        Description = null;
        IsActive = true;
        ErrorMessage = null;
        SaveButtonText = "Add Category";
        OnPropertyChanged(nameof(IsEditing));
    }
}
