using System.Globalization;
using SScreentify.Core.Settings;

namespace SScreentify.App.Ui;

/// <summary>The interface language: the one place that turns the setting into the culture <see cref="Strings"/> reads.</summary>
public static class UiLanguage
{
    /// <summary>The Windows display language, read once before the app sets any culture of its own.</summary>
    private static readonly string WindowsLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    public static Language Current { get; private set; } = Languages.Resolve(LanguagePreference.Auto, WindowsLanguage);

    /// <summary>Switches every later <see cref="Strings"/> lookup; returns true when the language changed.</summary>
    public static bool Apply(LanguagePreference preference)
    {
        var language = Languages.Resolve(preference, WindowsLanguage);
        var changed = language != Current || Strings.Culture is null;
        Current = language;
        Strings.Culture = CultureInfo.GetCultureInfo(language == Language.English ? "en" : "es");
        return changed;
    }
}

/// <summary>Texts of a long-lived control, re-read from <see cref="Strings"/> when the language changes.</summary>
public sealed class LocalizedTexts
{
    private readonly List<Action> _apply = new();

    /// <summary>Applies <paramref name="apply"/> now and again on every <see cref="Refresh"/>.</summary>
    public void Add(Action apply)
    {
        apply();
        _apply.Add(apply);
    }

    public void Refresh()
    {
        foreach (var apply in _apply) apply();
    }
}
