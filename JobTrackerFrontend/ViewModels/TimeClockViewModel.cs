using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobTrackerFrontend.Models;
using JobTrackerFrontend.Services;

namespace JobTrackerFrontend.ViewModels;

public partial class TimeClockViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private readonly DispatcherTimer _elapsedTimer;
    private DateTime? _clockInTime;
    private string _currentPasscode = "";
    private string _currentEmployeeId = "";

    // ── Kiosk state ──
    // Single state property controls which panel is visible:
    // "PasscodeEntry" | "NotClockedIn" | "ClockedIn" | "Success"
    [ObservableProperty] private string _kioskState = "PasscodeEntry";
    [ObservableProperty] private string _employeeId = "";
    [ObservableProperty] private string _passcode = "";
    [ObservableProperty] private string _workerName = "";
    [ObservableProperty] private string _elapsedTime = "";
    [ObservableProperty] private string _currentJobCodeDisplay = "";
    [ObservableProperty] private string _currentClockInDisplay = "";
    [ObservableProperty] private string? _currentJobTitle;
    [ObservableProperty] private JobCodeModel? _selectedJobCode;
    [ObservableProperty] private JobModel? _selectedJob;
    [ObservableProperty] private string? _kioskNotes;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _isError;
    [ObservableProperty] private bool _isKioskBusy;
    [ObservableProperty] private bool _isSwitching;
    [ObservableProperty] private JobCodeModel? _switchJobCode;
    [ObservableProperty] private JobModel? _switchJob;
    [ObservableProperty] private string? _switchNotes;

    // ── Reference data ──
    [ObservableProperty] private ObservableCollection<JobCodeModel> _jobCodes = [];
    [ObservableProperty] private ObservableCollection<WorkerModel> _workers = [];
    [ObservableProperty] private ObservableCollection<JobModel> _activeJobs = [];

    // ── History ──
    [ObservableProperty] private ObservableCollection<TimePunchModel> _punchHistory = [];
    [ObservableProperty] private WorkerModel? _workerFilter;
    [ObservableProperty] private JobCodeModel? _jobCodeFilter;
    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private bool _isLoading;

    public TimeClockViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;

        _elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _elapsedTimer.Tick += (_, _) => UpdateElapsed();
    }

    // ── Commands ──

    [RelayCommand]
    private async Task CheckPasscodeAsync()
    {
        if (string.IsNullOrWhiteSpace(EmployeeId) || string.IsNullOrWhiteSpace(Passcode)) return;

        IsKioskBusy = true;
        IsError = false;
        StatusMessage = null;

        try
        {
            var statusRequest = new StatusRequest { EmployeeId = EmployeeId, Passcode = Passcode };
            var status = await _apiClient.PostAsync<StatusRequest, PunchStatusModel>("/timeclock/status", statusRequest);
            if (status is null)
            {
                StatusMessage = "Invalid employee ID or passcode.";
                IsError = true;
                return;
            }

            _currentEmployeeId = EmployeeId;
            _currentPasscode = Passcode;
            WorkerName = status.FullName;

            if (status.IsClockedIn)
            {
                _clockInTime = status.ClockIn;
                CurrentJobCodeDisplay = status.JobCodeDisplay ?? "";
                CurrentClockInDisplay = status.ClockIn?.ToString("h:mm tt") ?? "";
                CurrentJobTitle = status.JobTitle;
                UpdateElapsed();
                _elapsedTimer.Start();
                KioskState = "ClockedIn";
            }
            else
            {
                KioskState = "NotClockedIn";
            }
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = await ExtractErrorMessage(ex, "Invalid passcode.");
            IsError = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsError = true;
        }
        finally
        {
            IsKioskBusy = false;
        }
    }

    [RelayCommand]
    private async Task ClockInAsync()
    {
        if (SelectedJobCode is null) return;

        IsKioskBusy = true;
        IsError = false;
        StatusMessage = null;

        try
        {
            var request = new ClockInRequest
            {
                EmployeeId = _currentEmployeeId,
                Passcode = _currentPasscode,
                JobCodeId = SelectedJobCode.Id,
                JobId = SelectedJob?.Id,
                Notes = KioskNotes
            };

            await _apiClient.PostAsync<ClockInRequest, TimePunchModel>("/timeclock/clock-in", request);

            StatusMessage = "Clocked in successfully!";
            KioskState = "Success";
            await ShowSuccessThenReset();
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = await ExtractErrorMessage(ex, "Failed to clock in.");
            IsError = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsError = true;
        }
        finally
        {
            IsKioskBusy = false;
        }
    }

    [RelayCommand]
    private async Task ClockOutAsync()
    {
        IsKioskBusy = true;
        IsError = false;
        StatusMessage = null;

        try
        {
            var request = new ClockOutRequest
            {
                EmployeeId = _currentEmployeeId,
                Passcode = _currentPasscode,
                Notes = KioskNotes
            };

            await _apiClient.PostAsync<ClockOutRequest, TimePunchModel>("/timeclock/clock-out", request);

            _elapsedTimer.Stop();
            StatusMessage = "Clocked out successfully!";
            KioskState = "Success";
            await ShowSuccessThenReset();
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = await ExtractErrorMessage(ex, "Failed to clock out.");
            IsError = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsError = true;
        }
        finally
        {
            IsKioskBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleSwitch()
    {
        IsSwitching = !IsSwitching;
        if (!IsSwitching)
        {
            SwitchJobCode = null;
            SwitchJob = null;
            SwitchNotes = null;
        }
    }

    [RelayCommand]
    private async Task SwitchJobCodeAsync()
    {
        if (SwitchJobCode is null) return;

        IsKioskBusy = true;
        IsError = false;
        StatusMessage = null;

        try
        {
            var request = new SwitchJobCodeRequest
            {
                EmployeeId = _currentEmployeeId,
                Passcode = _currentPasscode,
                JobCodeId = SwitchJobCode.Id,
                JobId = SwitchJob?.Id,
                Notes = SwitchNotes
            };

            var newPunch = await _apiClient.PostAsync<SwitchJobCodeRequest, TimePunchModel>("/timeclock/switch", request);

            // Update clocked-in display with new punch
            _clockInTime = newPunch?.ClockIn;
            CurrentJobCodeDisplay = newPunch?.JobCodeDisplay ?? "";
            CurrentClockInDisplay = newPunch?.ClockIn.ToString("h:mm tt") ?? "";
            CurrentJobTitle = newPunch?.JobTitle;
            UpdateElapsed();

            // Reset switch UI
            IsSwitching = false;
            SwitchJobCode = null;
            SwitchJob = null;
            SwitchNotes = null;
            KioskNotes = null;

            StatusMessage = "Switched job code!";
            await LoadHistoryAsync();

            // Brief flash then clear message
            await Task.Delay(2000);
            StatusMessage = null;
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = await ExtractErrorMessage(ex, "Failed to switch job code.");
            IsError = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsError = true;
        }
        finally
        {
            IsKioskBusy = false;
        }
    }

    [RelayCommand]
    private void ResetKiosk()
    {
        _elapsedTimer.Stop();
        KioskState = "PasscodeEntry";
        EmployeeId = "";
        Passcode = "";
        WorkerName = "";
        IsError = false;
        StatusMessage = null;
        ElapsedTime = "";
        CurrentJobCodeDisplay = "";
        CurrentClockInDisplay = "";
        CurrentJobTitle = null;
        SelectedJobCode = null;
        SelectedJob = null;
        KioskNotes = null;
        _currentEmployeeId = "";
        _currentPasscode = "";
        _clockInTime = null;
        IsSwitching = false;
        SwitchJobCode = null;
        SwitchJob = null;
        SwitchNotes = null;
    }

    [RelayCommand]
    private async Task LoadReferenceDataAsync()
    {
        try
        {
            var jobCodesTask = _apiClient.GetAsync<List<JobCodeModel>>("/timeclock/job-codes?active_only=true");
            var workersTask = _apiClient.GetAsync<List<WorkerModel>>("/timeclock/workers?active_only=true");
            var jobsTask = _apiClient.GetAsync<List<JobModel>>("/jobs?status=Active");

            await Task.WhenAll(jobCodesTask, workersTask, jobsTask);

            JobCodes = new ObservableCollection<JobCodeModel>(await jobCodesTask ?? []);
            Workers = new ObservableCollection<WorkerModel>(await workersTask ?? []);
            ActiveJobs = new ObservableCollection<JobModel>(await jobsTask ?? []);
        }
        catch
        {
            // Silently fail — dropdowns will just be empty
        }
    }

    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        IsLoading = true;
        try
        {
            var parts = new List<string>();
            if (WorkerFilter is not null) parts.Add($"worker_id={WorkerFilter.Id}");
            if (JobCodeFilter is not null) parts.Add($"job_code_id={JobCodeFilter.Id}");
            if (FromDate is not null) parts.Add($"from_date={FromDate:yyyy-MM-dd}");
            if (ToDate is not null) parts.Add($"to_date={ToDate:yyyy-MM-dd}");

            var query = parts.Count > 0 ? "?" + string.Join("&", parts) : "";
            var data = await _apiClient.GetAsync<List<TimePunchModel>>($"/timeclock/history{query}");
            PunchHistory = new ObservableCollection<TimePunchModel>(data ?? []);
        }
        catch
        {
            PunchHistory = [];
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Filter change handlers ──

    partial void OnWorkerFilterChanged(WorkerModel? value) => LoadHistoryCommand.ExecuteAsync(null);
    partial void OnJobCodeFilterChanged(JobCodeModel? value) => LoadHistoryCommand.ExecuteAsync(null);
    partial void OnFromDateChanged(DateTime? value) => LoadHistoryCommand.ExecuteAsync(null);
    partial void OnToDateChanged(DateTime? value) => LoadHistoryCommand.ExecuteAsync(null);

    // ── Helpers ──

    private void UpdateElapsed()
    {
        if (_clockInTime is null) return;
        var elapsed = DateTime.Now - _clockInTime.Value;
        ElapsedTime = elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours}h {elapsed.Minutes:D2}m {elapsed.Seconds:D2}s"
            : $"{elapsed.Minutes}m {elapsed.Seconds:D2}s";
    }

    private async Task ShowSuccessThenReset()
    {
        await LoadHistoryAsync();
        await Task.Delay(2000);
        ResetKiosk();
    }

    private static async Task<string> ExtractErrorMessage(HttpRequestException ex, string fallback)
    {
        // Try to extract API error detail from the response
        if (ex.InnerException is not null)
            return ex.InnerException.Message;

        return ex.Message.Contains("400") || ex.Message.Contains("404")
            ? fallback
            : $"Error: {ex.Message}";
    }
}
