using CommunityToolkit.Mvvm.ComponentModel;

namespace JobTrackerFrontend.Services;

public class NavigationService : ObservableObject
{
    private object? _currentView;

    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    private readonly Dictionary<string, Func<object>> _viewFactories = new();

    public void Register(string key, Func<object> factory)
    {
        _viewFactories[key] = factory;
    }

    public void NavigateTo(string key)
    {
        if (_viewFactories.TryGetValue(key, out var factory))
        {
            CurrentView = factory();
        }
    }
}
