using System.Windows;
using System.Windows.Controls;
using JobTrackerFrontend.ViewModels;

namespace JobTrackerFrontend.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            await vm.LoadDataCommand.ExecuteAsync(null);
        }
    }
}
