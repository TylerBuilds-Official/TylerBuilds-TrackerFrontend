using System.Windows;
using JobTrackerFrontend.ViewModels;

namespace JobTrackerFrontend.Views;

public partial class ExpenseFormDialog : Window
{
    public ExpenseFormDialog()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ExpenseFormViewModel vm)
        {
            await vm.LoadDataCommand.ExecuteAsync(null);
        }
    }
}
