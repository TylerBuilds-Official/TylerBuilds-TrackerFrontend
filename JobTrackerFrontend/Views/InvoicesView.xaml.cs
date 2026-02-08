using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JobTrackerFrontend.ViewModels;

namespace JobTrackerFrontend.Views;

public partial class InvoicesView : UserControl
{
    public InvoicesView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InvoicesViewModel vm)
        {
            await vm.LoadDataCommand.ExecuteAsync(null);
        }
    }

    private async void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is InvoicesViewModel { SelectedInvoice: not null } vm)
        {
            await vm.EditInvoiceCommand.ExecuteAsync(null);
        }
    }
}
