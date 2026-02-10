using System.Windows;
using JobTrackerFrontend.ViewModels;

namespace JobTrackerFrontend.Views;

public partial class LineItemsManagerDialog : Window
{
    public LineItemsManagerDialog()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LineItemsManagerViewModel vm)
        {
            await vm.LoadDataCommand.ExecuteAsync(null);
        }
    }
}
