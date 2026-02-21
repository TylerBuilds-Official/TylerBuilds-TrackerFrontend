using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;
using JobTrackerFrontend.Views;

namespace JobTrackerFrontend.ViewModels;

public partial class ResearchViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;

    [ObservableProperty] private ObservableCollection<ResearchLeadModel> _leads = [];
    [ObservableProperty] private ResearchLeadModel? _selectedLead;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    // Filter state
    [ObservableProperty] private string _statusFilter = "";
    [ObservableProperty] private string _wouldPayFilter = "";
    [ObservableProperty] private ResearchThemeModel? _themeFilter;

    // Filter dropdowns
    [ObservableProperty] private List<ResearchThemeModel> _themes = [];

    public List<string> StatusOptions { get; } = ["", "New", "Contacted", "Following Up", "Converted", "Closed"];
    public List<string> WouldPayOptions { get; } = ["", "No", "Maybe", "Yes"];

    // All leads cached for client-side filtering
    private List<ResearchLeadModel> _allLeads = [];

    public ResearchViewModel(ApiClient apiClient)
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
            var themesTask = _apiClient.GetAsync<List<ResearchThemeModel>>("/research/themes");
            var leadsTask = _apiClient.GetAsync<List<ResearchLeadModel>>("/research");

            await Task.WhenAll(themesTask, leadsTask);

            Themes = themesTask.Result ?? [];
            _allLeads = leadsTask.Result ?? [];

            ApplyFilters();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load research data: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allLeads.AsEnumerable();

        if (!string.IsNullOrEmpty(StatusFilter))
            filtered = filtered.Where(l => l.StatusDisplay == StatusFilter);

        if (!string.IsNullOrEmpty(WouldPayFilter))
            filtered = filtered.Where(l => l.WouldPayDisplay == WouldPayFilter);

        if (ThemeFilter is not null)
            filtered = filtered.Where(l => l.ResearchThemeId == ThemeFilter.ResearchThemeId);

        Leads = new ObservableCollection<ResearchLeadModel>(filtered);
    }

    // Re-apply filters when any filter changes
    partial void OnStatusFilterChanged(string value) => ApplyFilters();
    partial void OnWouldPayFilterChanged(string value) => ApplyFilters();
    partial void OnThemeFilterChanged(ResearchThemeModel? value) => ApplyFilters();

    [RelayCommand]
    private async Task NewLeadAsync()
    {
        var vm = new ResearchFormViewModel(_apiClient);
        var dialog = new ResearchFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
            await LoadDataAsync();
    }

    [RelayCommand]
    private async Task EditLeadAsync()
    {
        if (SelectedLead is null) return;

        var lead = await _apiClient.GetAsync<ResearchLeadModel>($"/research/{SelectedLead.ResearchLeadId}");
        if (lead is null) return;

        var vm = new ResearchFormViewModel(_apiClient, lead);
        var dialog = new ResearchFormDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
            await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DeleteLeadAsync()
    {
        if (SelectedLead is null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete this research lead?\n\n" +
            $"{SelectedLead.BusinessName}\n\n" +
            "This action cannot be undone.",
            "Delete Research Lead",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _apiClient.DeleteAsync($"/research/{SelectedLead.ResearchLeadId}");
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete lead: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private async Task PromoteLeadAsync()
    {
        if (SelectedLead is null) return;

        if (SelectedLead.IsLinkedToClient)
        {
            MessageBox.Show("This lead is already linked to a client.", "Already Promoted",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var vm = new ResearchPromoteViewModel(_apiClient, SelectedLead);
        var dialog = new ResearchPromoteDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
            await LoadDataAsync();
    }

    [RelayCommand]
    private async Task ManageThemesAsync()
    {
        var vm = new ResearchThemeManagerViewModel(_apiClient);
        var dialog = new ResearchThemeManagerDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        dialog.ShowDialog();

        if (vm.HasChanges)
            await LoadDataAsync();
    }
}
