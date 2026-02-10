using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JobTrackerFrontend.ViewModels;

namespace JobTrackerFrontend.Views;

public partial class TimeClockView : UserControl
{
    public TimeClockView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TimeClockViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(vm.Passcode) && string.IsNullOrEmpty(vm.Passcode))
                    PasscodeBox.Clear();
            };
            await vm.LoadReferenceDataCommand.ExecuteAsync(null);
            await vm.LoadHistoryCommand.ExecuteAsync(null);
        }
    }

    private void OnPasscodeChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is TimeClockViewModel vm && sender is PasswordBox pb)
        {
            vm.Passcode = pb.Password;
        }
    }

    private void OnEmployeeIdKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PasscodeBox.Focus();
        }
    }

    private void OnPasscodeKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is TimeClockViewModel vm)
        {
            vm.CheckPasscodeCommand.ExecuteAsync(null);
        }
    }

    private void OnClearFilters(object sender, RoutedEventArgs e)
    {
        if (DataContext is TimeClockViewModel vm)
        {
            vm.WorkerFilter = null;
            vm.JobCodeFilter = null;
            vm.FromDate = null;
            vm.ToDate = null;
        }
    }
}
