using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;

namespace JobTrackerFrontend.ViewModels;

public partial class ResearchThemeManagerViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;

    [ObservableProperty] private ObservableCollection<ResearchThemeModel> _themes = [];
    [ObservableProperty] private ResearchThemeModel? _selectedTheme;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    // Form fields
    [ObservableProperty] private string? _name;
    [ObservableProperty] private string? _description;

    // Edit state
    [ObservableProperty] private int? _editingId;
    [ObservableProperty] private string _saveButtonText = "Add Theme";

    public bool IsEditing => EditingId.HasValue;
    public bool HasChanges { get; private set; }

    public ResearchThemeManagerViewModel(ApiClient apiClient)
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
            var data = await _apiClient.GetAsync<List<ResearchThemeModel>>("/research/themes");
            Themes = new ObservableCollection<ResearchThemeModel>(data ?? []);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load themes: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveThemeAsync()
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
                var request = new UpdateResearchThemeRequest
                {
                    Name = Name!.Trim(),
                    Description = Description?.Trim()
                };
                await _apiClient.PutAsync<UpdateResearchThemeRequest, ResearchThemeModel>(
                    $"/research/themes/{EditingId}", request);
            }
            else
            {
                var request = new CreateResearchThemeRequest
                {
                    Name = Name!.Trim(),
                    Description = Description?.Trim()
                };
                await _apiClient.PostAsync<CreateResearchThemeRequest, ResearchThemeModel>(
                    "/research/themes", request);
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
    private async Task DeleteThemeAsync()
    {
        if (SelectedTheme is null) return;

        var result = MessageBox.Show(
            $"Delete theme \"{SelectedTheme.Name}\"?\n\n" +
            "This will fail if any research leads are using this theme.",
            "Delete Theme",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _apiClient.DeleteAsync($"/research/themes/{SelectedTheme.ResearchThemeId}");
            HasChanges = true;

            if (EditingId == SelectedTheme.ResearchThemeId)
                ClearForm();

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Delete failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void EditTheme()
    {
        if (SelectedTheme is null) return;

        EditingId = SelectedTheme.ResearchThemeId;
        Name = SelectedTheme.Name;
        Description = SelectedTheme.Description;
        SaveButtonText = "Update Theme";
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
        ErrorMessage = null;
        SaveButtonText = "Add Theme";
        OnPropertyChanged(nameof(IsEditing));
    }
}
