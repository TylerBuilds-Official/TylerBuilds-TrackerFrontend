using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using JobTrackerFrontend.ViewModels;

namespace JobTrackerFrontend.Views;

public partial class NotesView : UserControl
{
    private NotesViewModel? _vm;
    private bool _suppressSelectionChanged;

    private static readonly Duration AnimDuration = new(TimeSpan.FromMilliseconds(200));
    private static readonly IEasingFunction AnimEase = new QuadraticEase { EasingMode = EasingMode.EaseOut };

    public NotesView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not NotesViewModel vm) return;

        _vm = vm;

        _vm.LoadContentRequested += OnLoadContentRequested;
        _vm.ExtractContentRequested += OnExtractContentRequested;
        _vm.DrawerOpenRequested += OpenDrawer;
        _vm.DrawerCloseRequested += CloseDrawer;

        await _vm.LoadDataCommand.ExecuteAsync(null);
    }

    // ──────────────────────────────────────────
    // Drawer Animation
    // ──────────────────────────────────────────

    private void OpenDrawer()
    {
        DrawerOverlay.Visibility = Visibility.Visible;

        var slideIn = new DoubleAnimation(-280, 0, AnimDuration) { EasingFunction = AnimEase };
        var fadeIn = new DoubleAnimation(0, 1, AnimDuration) { EasingFunction = AnimEase };

        DrawerTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideIn);
        DrawerBackdrop.BeginAnimation(OpacityProperty, fadeIn);
    }

    private void CloseDrawer()
    {
        var slideOut = new DoubleAnimation(0, -280, AnimDuration) { EasingFunction = AnimEase };
        var fadeOut = new DoubleAnimation(1, 0, AnimDuration) { EasingFunction = AnimEase };

        slideOut.Completed += (_, _) => DrawerOverlay.Visibility = Visibility.Collapsed;

        DrawerTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideOut);
        DrawerBackdrop.BeginAnimation(OpacityProperty, fadeOut);
    }

    private void DrawerBackdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _vm?.CloseDrawerCommand.Execute(null);
    }

    // ──────────────────────────────────────────
    // RTF Bridge
    // ──────────────────────────────────────────

    private void OnLoadContentRequested(string rtf)
    {
        var range = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd);

        if (string.IsNullOrEmpty(rtf))
        {
            range.Text = "";
            return;
        }

        try
        {
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(rtf));
            range.Load(stream, DataFormats.Rtf);
        }
        catch
        {
            range.Text = rtf;
        }
    }

    private string OnExtractContentRequested()
    {
        var range = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd);

        if (string.IsNullOrWhiteSpace(range.Text))
            return "";

        try
        {
            using var stream = new MemoryStream();
            range.Save(stream, DataFormats.Rtf);
            stream.Position = 0;
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
        catch
        {
            return range.Text;
        }
    }

    // ──────────────────────────────────────────
    // Event Handlers
    // ──────────────────────────────────────────

    private void Editor_TextChanged(object sender, TextChangedEventArgs e)
    {
        _vm?.NotifyContentChanged();
    }

    private async void NoteListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChanged || _vm is null) return;

        var newSelection = NoteListBox.SelectedItem as Models.NoteModel;

        if (!await _vm.TrySelectNoteAsync(newSelection))
        {
            _suppressSelectionChanged = true;
            NoteListBox.SelectedItem = e.RemovedItems.Count > 0 ? e.RemovedItems[0] : null;
            _suppressSelectionChanged = false;
        }
        else
        {
            _vm.CloseDrawerCommand.Execute(null);
        }
    }

    // ──────────────────────────────────────────
    // Formatting Commands
    // ──────────────────────────────────────────

    private void Bold_Click(object sender, RoutedEventArgs e)
    {
        EditingCommands.ToggleBold.Execute(null, Editor);
        Editor.Focus();
    }

    private void Italic_Click(object sender, RoutedEventArgs e)
    {
        EditingCommands.ToggleItalic.Execute(null, Editor);
        Editor.Focus();
    }

    private void Underline_Click(object sender, RoutedEventArgs e)
    {
        EditingCommands.ToggleUnderline.Execute(null, Editor);
        Editor.Focus();
    }

    private void Bullet_Click(object sender, RoutedEventArgs e)
    {
        EditingCommands.ToggleBullets.Execute(null, Editor);
        Editor.Focus();
    }
}
