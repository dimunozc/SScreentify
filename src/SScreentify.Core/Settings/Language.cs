namespace SScreentify.Core.Settings;

/// <summary>What the user picked in Options; Auto follows the Windows display language.</summary>
public enum LanguagePreference { Auto, Spanish, English }

/// <summary>A language the interface is written in.</summary>
public enum Language { Spanish, English }

public static class Languages
{
    /// <summary>Auto gives Spanish when Windows is in Spanish and English for every other Windows language.</summary>
    /// <param name="windowsLanguage">The two-letter ISO code of the Windows display language, such as "es".</param>
    public static Language Resolve(LanguagePreference preference, string windowsLanguage) => preference switch
    {
        LanguagePreference.Spanish => Language.Spanish,
        LanguagePreference.English => Language.English,
        _ => string.Equals(windowsLanguage, "es", StringComparison.OrdinalIgnoreCase) ? Language.Spanish : Language.English,
    };
}
