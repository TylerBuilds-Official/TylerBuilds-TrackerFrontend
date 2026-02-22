using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;

namespace JobTrackerFrontend.ViewModels;

public partial class NotesViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;

    [ObservableProperty] private ObservableCollection<NoteModel> _notes = [];
    [ObservableProperty] private NoteModel? _selectedNote;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    // Editor state
    [ObservableProperty] private string _editingTitle = "";
    [ObservableProperty] private bool _isGlobal;
    [ObservableProperty] private bool _hasActiveNote;
    [ObservableProperty] private bool _isSaving;

    // Drawer state
    [ObservableProperty] private bool _isDrawerOpen;

    // Filter
    [ObservableProperty] private string _scopeFilter = "All";
    public List<string> ScopeFilterOptions { get; } = ["All", "Private", "Global"];

    // Dirty tracking — managed via code-behind pushing RTF content
    private string _savedRtfContent = "";
    private string _currentRtfContent = "";
    private int? _activeNoteId;

    private List<NoteModel> _allNotes = [];

    /// <summary>Raised when the view should load RTF content into the RichTextBox.</summary>
    public event Action<string>? LoadContentRequested;

    /// <summary>Raised when the view should extract RTF content from the RichTextBox.</summary>
    public event Func<string>? ExtractContentRequested;

    /// <summary>Raised when the drawer should open with animation.</summary>
    public event Action? DrawerOpenRequested;

    /// <summary>Raised when the drawer should close with animation.</summary>
    public event Action? DrawerCloseRequested;

    public bool IsDirty
    {
        get
        {
            if (!HasActiveNote) return false;

            SyncCurrentContent();

            if (_activeNoteId.HasValue)
            {
                var original = _allNotes.FirstOrDefault(n => n.NoteId == _activeNoteId.Value);
                if (original is null) return false;

                return EditingTitle != original.Title
                    || IsGlobal != original.IsGlobal
                    || _currentRtfContent != _savedRtfContent;
            }

            return !string.IsNullOrWhiteSpace(EditingTitle)
                || !string.IsNullOrEmpty(_currentRtfContent);
        }
    }

    public NotesViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>Called by code-behind on RichTextBox.TextChanged.</summary>
    public void NotifyContentChanged()
    {
        OnPropertyChanged(nameof(IsDirty));
    }

    private void SyncCurrentContent()
    {
        if (ExtractContentRequested is not null)
            _currentRtfContent = ExtractContentRequested.Invoke();
    }

    // ──────────────────────────────────────────
    // Drawer
    // ──────────────────────────────────────────

    [RelayCommand]
    private void ToggleDrawer()
    {
        if (IsDrawerOpen)
            CloseDrawer();
        else
            OpenDrawer();
    }

    [RelayCommand]
    private void CloseDrawer()
    {
        if (!IsDrawerOpen) return;
        IsDrawerOpen = false;
        DrawerCloseRequested?.Invoke();
    }

    private void OpenDrawer()
    {
        IsDrawerOpen = true;
        DrawerOpenRequested?.Invoke();
    }

    // ──────────────────────────────────────────
    // Data loading
    // ──────────────────────────────────────────

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var data = await _apiClient.GetAsync<List<NoteModel>>("/notes");
            _allNotes = data ?? [];
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load notes: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var filtered = ScopeFilter switch
        {
            "Private" => _allNotes.Where(n => !n.IsGlobal),
            "Global" => _allNotes.Where(n => n.IsGlobal),
            _ => _allNotes.AsEnumerable()
        };

        Notes = new ObservableCollection<NoteModel>(filtered);
    }

    partial void OnScopeFilterChanged(string value) => ApplyFilter();

    partial void OnEditingTitleChanged(string value) => OnPropertyChanged(nameof(IsDirty));
    partial void OnIsGlobalChanged(bool value) => OnPropertyChanged(nameof(IsDirty));

    // ──────────────────────────────────────────
    // Note selection with dirty guard
    // ──────────────────────────────────────────

    /// <summary>Called by code-behind before actually switching notes. Returns false if cancelled.</summary>
    public async Task<bool> TrySelectNoteAsync(NoteModel? note)
    {
        if (note?.NoteId == _activeNoteId) return true;

        if (!await PromptSaveIfDirtyAsync()) return false;

        LoadNoteIntoEditor(note);

        return true;
    }

    private void LoadNoteIntoEditor(NoteModel? note)
    {
        if (note is not null)
        {
            _activeNoteId = note.NoteId;
            EditingTitle = note.Title;
            IsGlobal = note.IsGlobal;
            _savedRtfContent = note.Content ?? "";
            _currentRtfContent = _savedRtfContent;
            HasActiveNote = true;
            LoadContentRequested?.Invoke(_savedRtfContent);
        }
        else
        {
            ClearEditor();
        }

        OnPropertyChanged(nameof(IsDirty));
    }

    private void ClearEditor()
    {
        _activeNoteId = null;
        EditingTitle = "";
        IsGlobal = false;
        _savedRtfContent = "";
        _currentRtfContent = "";
        HasActiveNote = false;
        LoadContentRequested?.Invoke("");
    }

    // ──────────────────────────────────────────
    // Dirty guard
    // ──────────────────────────────────────────

    /// <summary>Returns true if safe to proceed (saved, discarded, or not dirty). False if cancelled.</summary>
    public async Task<bool> PromptSaveIfDirtyAsync()
    {
        if (!IsDirty) return true;

        var result = MessageBox.Show(
            $"Save changes to \"{EditingTitle}\"?",
            "Unsaved Changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        return result switch
        {
            MessageBoxResult.Yes => await SaveCurrentNoteAsync(),
            MessageBoxResult.No => true,
            _ => false
        };
    }

    // ──────────────────────────────────────────
    // Commands
    // ──────────────────────────────────────────

    [RelayCommand]
    private async Task NewNoteAsync()
    {
        if (!await PromptSaveIfDirtyAsync()) return;

        SelectedNote = null;

        _activeNoteId = null;
        EditingTitle = "Untitled Note";
        IsGlobal = false;
        _savedRtfContent = "";
        _currentRtfContent = "";
        HasActiveNote = true;
        LoadContentRequested?.Invoke("");
        OnPropertyChanged(nameof(IsDirty));
    }

    [RelayCommand]
    private async Task SaveNoteAsync()
    {
        if (await SaveCurrentNoteAsync())
            await LoadDataAsync();
    }

    /// <summary>Async save. Returns true on success.</summary>
    private async Task<bool> SaveCurrentNoteAsync()
    {
        if (!HasActiveNote) return false;

        if (string.IsNullOrWhiteSpace(EditingTitle))
        {
            ErrorMessage = "Title is required.";
            return false;
        }

        SyncCurrentContent();
        ErrorMessage = null;
        IsSaving = true;

        try
        {
            if (_activeNoteId.HasValue)
            {
                var request = new UpdateNoteRequest
                {
                    Title = EditingTitle.Trim(),
                    Content = _currentRtfContent,
                    IsGlobal = IsGlobal
                };
                await _apiClient.PutAsync<UpdateNoteRequest, NoteModel>(
                    $"/notes/{_activeNoteId}", request);

                _savedRtfContent = _currentRtfContent;
            }
            else
            {
                var request = new CreateNoteRequest
                {
                    Title = EditingTitle.Trim(),
                    Content = _currentRtfContent,
                    IsGlobal = IsGlobal
                };
                var created = await _apiClient.PostAsync<CreateNoteRequest, NoteModel>(
                    "/notes", request);

                if (created is not null)
                    _activeNoteId = created.NoteId;

                _savedRtfContent = _currentRtfContent;
            }

            OnPropertyChanged(nameof(IsDirty));

            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Save failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task DeleteNoteAsync()
    {
        if (!_activeNoteId.HasValue) return;

        var result = MessageBox.Show(
            $"Delete \"{EditingTitle}\"?\n\nThis action cannot be undone.",
            "Delete Note",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _apiClient.DeleteAsync($"/notes/{_activeNoteId}");
            ClearEditor();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete note: {ex.Message}", "Error");
        }
    }
}
