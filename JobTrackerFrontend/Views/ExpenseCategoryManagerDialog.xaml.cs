using System.Windows;
using JobTrackerFrontend.ViewModels;

namespace JobTrackerFrontend.Views;

public partial class ExpenseCategoryManagerDialog : Window
{
    public ExpenseCategoryManagerDialog()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ExpenseCategoryManagerViewModel vm)
        {
            await vm.LoadDataCommand.ExecuteAsync(null);
        }
    }
}
