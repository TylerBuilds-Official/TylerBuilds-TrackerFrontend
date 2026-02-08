using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace JobTrackerFrontend.Services;

public class ThemeService
{
    private readonly PaletteHelper _paletteHelper = new();

    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TylerBuilds", "JobTracker"
    );
    private static readonly string SettingsPath = Path.Combine(SettingsDir, "theme.json");

    // BlueGrey-tinted dark palette
    private static readonly Color DarkPaper          = (Color)ColorConverter.ConvertFromString("#1A2327")!;
    private static readonly Color DarkCard           = (Color)ColorConverter.ConvertFromString("#212D33")!;
    private static readonly Color DarkToolBar        = (Color)ColorConverter.ConvertFromString("#253238")!;
    private static readonly Color DarkDivider        = (Color)ColorConverter.ConvertFromString("#37474F")!;
    private static readonly Color DarkBodyText       = (Color)ColorConverter.ConvertFromString("#ECEFF1")!;
    private static readonly Color DarkBodyTextLight  = (Color)ColorConverter.ConvertFromString("#90A4AE")!;

    // BlueGrey-tinted light palette
    private static readonly Color LightPaper         = (Color)ColorConverter.ConvertFromString("#EFF2F6")!;

    public bool IsDarkMode { get; private set; }

    public void Initialize()
    {
        IsDarkMode = LoadPreference();
        ApplyTheme(IsDarkMode);
    }

    public void Toggle()
    {
        IsDarkMode = !IsDarkMode;
        ApplyTheme(IsDarkMode);
        SavePreference(IsDarkMode);
    }

    public void SetDarkMode(bool isDark)
    {
        IsDarkMode = isDark;
        ApplyTheme(isDark);
        SavePreference(isDark);
    }

    private void ApplyTheme(bool isDark)
    {
        var theme = _paletteHelper.GetTheme();
        theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
        _paletteHelper.SetTheme(theme);

        // Re-read what MaterialDesign just set as defaults, then override for dark
        var res = Application.Current.Resources;

        if (isDark)
        {
            res["MaterialDesignPaper"]             = new SolidColorBrush(DarkPaper);
            res["MaterialDesignCardBackground"]    = new SolidColorBrush(DarkCard);
            res["MaterialDesignToolBarBackground"] = new SolidColorBrush(DarkToolBar);
            res["MaterialDesignBody"]              = new SolidColorBrush(DarkBodyText);
            res["MaterialDesignBodyLight"]         = new SolidColorBrush(DarkBodyTextLight);
            res["MaterialDesignDivider"]           = new SolidColorBrush(DarkDivider);
        }
        else
        {
            // Remove dark overrides so MaterialDesign's light theme values take effect
            res.Remove("MaterialDesignCardBackground");
            res.Remove("MaterialDesignToolBarBackground");
            res.Remove("MaterialDesignBody");
            res.Remove("MaterialDesignBodyLight");
            res.Remove("MaterialDesignDivider");

            // Re-apply base light theme so merged dictionaries re-resolve
            _paletteHelper.SetTheme(theme);

            // Override paper with blue-grey tinted background
            res["MaterialDesignPaper"] = new SolidColorBrush(LightPaper);
        }
    }

    private static bool LoadPreference()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return false;
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<ThemeSettings>(json);
            return settings?.DarkMode ?? false;
        }
        catch
        {
            return false;
        }
    }

    private static void SavePreference(bool isDark)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(new ThemeSettings { DarkMode = isDark });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Non-critical — silently ignore
        }
    }

    private class ThemeSettings
    {
        public bool DarkMode { get; set; }
    }
}
