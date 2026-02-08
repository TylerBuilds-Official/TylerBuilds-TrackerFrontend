using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using JobTrackerFrontend.ViewModels;

namespace JobTrackerFrontend;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;

        if (e.NewValue is INotifyPropertyChanged newVm)
            newVm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsNavExpanded) && sender is MainWindowViewModel vm)
        {
            var animation = new DoubleAnimation
            {
                To = vm.IsNavExpanded ? 220 : 56,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = (QuadraticEase)FindResource("SidebarEase")
            };
            SidebarBorder.BeginAnimation(WidthProperty, animation);
        }
    }
}
