using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Threading;
using ExplorerTabUtility.Managers;

namespace ExplorerTabUtility.Localization;

public sealed class LocalizationManager : INotifyPropertyChanged
{
    private static readonly ResourceManager ResourceManager = new("ExplorerTabUtility.Resources.Strings", typeof(LocalizationManager).Assembly);
    public static LocalizationManager Instance { get; } = new();
    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => ResourceManager.GetString(key, Culture) ?? ResourceManager.GetString(key, CultureInfo.GetCultureInfo("en")) ?? key;
    public CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("en");
    public string Language => SettingsManager.Language;

    public void Initialize() => SetLanguage(SettingsManager.Language, persist: false);

    public void SetLanguage(string language, bool persist = true)
    {
        var normalized = language is "en" or "zh-CN" ? language : "auto";
        var culture = normalized == "auto" ? CultureInfo.CurrentUICulture : CultureInfo.GetCultureInfo(normalized);
        Culture = culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? CultureInfo.GetCultureInfo("zh-CN")
            : CultureInfo.GetCultureInfo("en");
        Thread.CurrentThread.CurrentCulture = Culture;
        Thread.CurrentThread.CurrentUICulture = Culture;

        if (persist && SettingsManager.Language != normalized)
            SettingsManager.Language = normalized;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
