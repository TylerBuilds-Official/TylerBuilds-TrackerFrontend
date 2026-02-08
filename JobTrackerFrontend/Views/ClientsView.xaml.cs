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
            await vm.ViewDetailCommand.ExecuteAsync(null);
        }
    }

    private void TabRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (DetailTabs is null) return;
        if (sender is FrameworkElement { Tag: string tag } && int.TryParse(tag, out var index))
        {
            DetailTabs.SelectedIndex = index;
        }
    }
}
