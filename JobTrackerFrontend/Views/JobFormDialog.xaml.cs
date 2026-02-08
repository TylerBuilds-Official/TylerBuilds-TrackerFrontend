using System.Windows;
using JobTrackerFrontend.ViewModels;

namespace JobTrackerFrontend.Views;

public partial class JobFormDialog : Window
{
    public JobFormDialog()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is JobFormViewModel vm)
        {
            await vm.LoadClientsCommand.ExecuteAsync(null);
        }
    }
}
