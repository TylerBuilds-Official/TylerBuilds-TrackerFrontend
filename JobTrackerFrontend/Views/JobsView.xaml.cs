using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JobTrackerFrontend.ViewModels;

namespace JobTrackerFrontend.Views;

public partial class JobsView : UserControl
{
    public JobsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is JobsViewModel vm)
        {
            await vm.LoadDataCommand.ExecuteAsync(null);
        }
    }

    private async void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is JobsViewModel { SelectedJob: not null } vm)
        {
            await vm.EditJobCommand.ExecuteAsync(null);
        }
    }
}
