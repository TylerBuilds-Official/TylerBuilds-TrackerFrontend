using System.Windows;
using JobTrackerFrontend.ViewModels;

namespace JobTrackerFrontend.Views;

public partial class InvoiceFormDialog : Window
{
    public InvoiceFormDialog()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InvoiceFormViewModel vm)
        {
            await vm.LoadJobsCommand.ExecuteAsync(null);
        }
    }
}
