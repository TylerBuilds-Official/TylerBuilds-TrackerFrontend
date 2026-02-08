using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JobTrackerFrontend.ViewModels;

namespace JobTrackerFrontend.Views;

public partial class ClientsView : UserControl
{
    public ClientsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ClientsViewModel vm)
        {
            await vm.LoadDataCommand.ExecuteAsync(null);
        }
    }

    private async void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ClientsViewModel { SelectedClient: not null } vm)
        {
            await vm.EditClientCommand.ExecuteAsync(null);
        }
    }
}
