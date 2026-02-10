using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;

namespace JobTrackerFrontend.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private readonly ThemeService _themeService;

    [ObservableProperty] private RevenueSummaryModel? _revenueSummary;
    [ObservableProperty] private ObservableCollection<JobPipelineModel> _pipeline = [];
    [ObservableProperty] private ObservableCollection<RecentActivityModel> _recentActivity = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    // Chart
    [ObservableProperty] private ISeries[] _incomeChartSeries = [];
    [ObservableProperty] private Axis[] _incomeXAxes = [];
    [ObservableProperty] private Axis[] _incomeYAxes = [];
    [ObservableProperty] private bool _isMonthView = true;
    [ObservableProperty] private string _chartTitle = "";
    [ObservableProperty] private bool _hasChartData;

    // Weekly Hours chart
    [ObservableProperty] private ISeries[] _weeklyHoursSeries = [];
    [ObservableProperty] private Axis[] _weeklyHoursXAxes = [];
    [ObservableProperty] private Axis[] _weeklyHoursYAxes = [];
    [ObservableProperty] private bool _hasWeeklyHoursData;

    public DashboardViewModel(ApiClient apiClient, ThemeService themeService)
    {
        _apiClient = apiClient;
        _themeService = themeService;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            RevenueSummary = await _apiClient.GetAsync<RevenueSummaryModel>("/dashboard/revenue-summary");

            var pipelineData = await _apiClient.GetAsync<List<JobPipelineModel>>("/dashboard/job-pipeline");
            Pipeline = new ObservableCollection<JobPipelineModel>(pipelineData ?? []);

            var activityData = await _apiClient.GetAsync<List<RecentActivityModel>>("/dashboard/recent-activity?limit=10");
            RecentActivity = new ObservableCollection<RecentActivityModel>(activityData ?? []);

            await LoadIncomeChartAsync();
            await LoadWeeklyHoursAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load dashboard: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SetMonthViewAsync()
    {
        IsMonthView = true;
        await LoadIncomeChartAsync();
    }

    [RelayCommand]
    private async Task SetYearViewAsync()
    {
        IsMonthView = false;
        await LoadIncomeChartAsync();
    }

    private async Task LoadIncomeChartAsync()
    {
        try
        {
            var now = DateTime.Now;
            var url = IsMonthView
                ? $"/dashboard/income-by-job?year={now.Year}&month={now.Month}"
                : $"/dashboard/income-by-job?year={now.Year}";

            ChartTitle = IsMonthView
                ? $"Income by Job — {now:MMMM yyyy}"
                : $"Income by Job — {now.Year}";

            var data = await _apiClient.GetAsync<List<JobIncomeModel>>(url) ?? [];

            if (data.Count == 0)
            {
                IncomeChartSeries = [];
                HasChartData = false;
                return;
            }

            var labels = data.Select(d => TruncateLabel(d.JobTitle, 18)).ToArray();
            var paid = data.Select(d => (double)d.PaidAmount).ToArray();
            var invoiced = data.Select(d => (double)d.InvoicedAmount).ToArray();

            var isDark = _themeService.IsDarkMode;

            var textColor = isDark ? new SKColor(176, 190, 197) : new SKColor(84, 110, 122);
            var mutedColor = isDark ? new SKColor(120, 144, 156) : new SKColor(144, 164, 174);
            var gridColor = isDark ? new SKColor(55, 71, 79, 100) : new SKColor(176, 190, 197, 80);

            IncomeChartSeries =
            [
                new StackedColumnSeries<double>
                {
                    Name = "Paid",
                    Values = paid,
                    Fill = new SolidColorPaint(new SKColor(56, 142, 60)),
                    MaxBarWidth = 36,
                },
                new StackedColumnSeries<double>
                {
                    Name = "Invoiced",
                    Values = invoiced,
                    Fill = new SolidColorPaint(new SKColor(255, 160, 0)),
                    MaxBarWidth = 36,
                },
            ];

            IncomeXAxes =
            [
                new Axis
                {
                    Labels = labels,
                    LabelsRotation = labels.Length > 5 ? 25 : 0,
                    TextSize = 12,
                    LabelsPaint = new SolidColorPaint(textColor),
                    SeparatorsPaint = null,
                },
            ];

            IncomeYAxes =
            [
                new Axis
                {
                    Labeler = value => value.ToString("C0"),
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(mutedColor),
                    SeparatorsPaint = new SolidColorPaint(gridColor)
                    {
                        StrokeThickness = 1,
                        PathEffect = new DashEffect([4, 4]),
                    },
                    MinLimit = 0,
                },
            ];

            // Set last so chart is visible before data binds
            HasChartData = true;
        }
        catch
        {
            HasChartData = false;
        }
    }

    private async Task LoadWeeklyHoursAsync()
    {
        try
        {
            var today = DateTime.Today;
            var monday = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            if (today.DayOfWeek == DayOfWeek.Sunday) monday = monday.AddDays(-7);
            var sunday = monday.AddDays(6);

            var url = $"/timeclock/summary?from_date={monday:yyyy-MM-dd}&to_date={sunday:yyyy-MM-dd}";
            var data = await _apiClient.GetAsync<List<JobCodeSummaryModel>>(url) ?? [];

            if (data.Count == 0)
            {
                WeeklyHoursSeries = [];
                HasWeeklyHoursData = false;
                return;
            }

            var labels = data.Select(d => TruncateLabel(d.DisplayName, 20)).ToArray();
            var hours = data.Select(d => d.TotalHours).ToArray();

            var isDark = _themeService.IsDarkMode;
            var textColor = isDark ? new SKColor(176, 190, 197) : new SKColor(84, 110, 122);
            var mutedColor = isDark ? new SKColor(120, 144, 156) : new SKColor(144, 164, 174);
            var gridColor = isDark ? new SKColor(55, 71, 79, 100) : new SKColor(176, 190, 197, 80);

            // Color palette for bars
            var colors = new SKColor[]
            {
                new(38, 166, 154),   // Teal
                new(66, 165, 245),   // Blue
                new(255, 167, 38),   // Orange
                new(126, 87, 194),   // Purple
                new(239, 83, 80),    // Red
                new(102, 187, 106),  // Green
                new(255, 202, 40),   // Yellow
            };

            WeeklyHoursSeries =
            [
                new RowSeries<double>
                {
                    Values = hours,
                    Fill = new SolidColorPaint(colors[0]),
                    MaxBarWidth = 28,
                },
            ];

            WeeklyHoursXAxes =
            [
                new Axis
                {
                    Labeler = value => $"{value:F1}h",
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(mutedColor),
                    SeparatorsPaint = new SolidColorPaint(gridColor)
                    {
                        StrokeThickness = 1,
                        PathEffect = new DashEffect([4, 4]),
                    },
                    MinLimit = 0,
                },
            ];

            WeeklyHoursYAxes =
            [
                new Axis
                {
                    Labels = labels,
                    TextSize = 12,
                    LabelsPaint = new SolidColorPaint(textColor),
                    SeparatorsPaint = null,
                },
            ];

            HasWeeklyHoursData = true;
        }
        catch
        {
            HasWeeklyHoursData = false;
        }
    }

    private static string TruncateLabel(string text, int max)
    {
        return text.Length <= max ? text : text[..(max - 1)] + "…";
    }
}
