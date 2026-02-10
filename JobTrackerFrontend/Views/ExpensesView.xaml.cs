using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JobTrackerFrontend.ViewModels;

namespace JobTrackerFrontend.Views;

public partial class ExpensesView : UserControl
{
    public ExpensesView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ExpensesViewModel vm)
        {
            await vm.LoadDataCommand.ExecuteAsync(null);
        }
    }

    private async void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ExpensesViewModel { SelectedExpense: not null } vm)
        {
            await vm.EditExpenseCommand.ExecuteAsync(null);
        }
    }
}
