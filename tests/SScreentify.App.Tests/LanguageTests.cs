using System.Globalization;
using System.Text.RegularExpressions;
using SScreentify.App.Overlay;
using SScreentify.App.Ui;
using SScreentify.Core.Settings;

namespace SScreentify.App.Tests;

/// <summary>These tests switch the app-wide interface language, so they never run alongside the others.</summary>
[CollectionDefinition(nameof(LanguageTests), DisableParallelization = true)]
public class LanguageCollection;

[Collection(nameof(LanguageTests))]
public class LanguageTests
{
    private static readonly string UiFolder = FindUiFolder();

    private static string FindUiFolder()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var ui = Path.Combine(dir.FullName, "src", "SScreentify.App", "Ui");
            if (Directory.Exists(ui)) return ui;
        }
        throw new DirectoryNotFoundException("src/SScreentify.App/Ui not found above the test output.");
    }

    private static Dictionary<string, string> Resx(string file) =>
        Regex.Matches(File.ReadAllText(Path.Combine(UiFolder, file)), "<data name=\"([^\"]+)\"[^>]*><value>([^<]*)</value>")
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);

    private static void WithLanguage(LanguagePreference preference, Action test)
    {
        var previous = Strings.Culture;
        try
        {
            UiLanguage.Apply(preference);
            test();
        }
        finally
        {
            Strings.Culture = previous;
        }
    }

    [Fact]
    public void EnglishResources_HaveEveryKeyAndKeepPlaceholders()
    {
        var spanish = Resx("Strings.resx");
        var english = Resx("Strings.en.resx");
        Assert.Equal(spanish.Keys.Order(), english.Keys.Order());
        foreach (var (key, text) in spanish)
        {
            Assert.False(string.IsNullOrWhiteSpace(english[key]), key);
            Assert.Equal(Regex.Matches(text, @"\{\d\}").Select(m => m.Value), Regex.Matches(english[key], @"\{\d\}").Select(m => m.Value));
        }
    }

    [Theory]
    [InlineData(LanguagePreference.English, "Capture", Language.English)]
    [InlineData(LanguagePreference.Spanish, "Capturar", Language.Spanish)]
    public void Apply_SwitchesStrings(LanguagePreference preference, string capture, Language expected)
    {
        WithLanguage(preference, () =>
        {
            Assert.Equal(capture, Strings.MenuCapture);
            Assert.Equal(expected, UiLanguage.Current);
            foreach (var p in typeof(Strings).GetProperties().Where(p => p.PropertyType == typeof(string)))
                Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(null)), p.Name);
        });
    }

    [Fact]
    public void Toolbars_FollowALanguageChange()
    {
        StaRunner.Run(() =>
        {
            WithLanguage(LanguagePreference.Spanish, () =>
            {
                var chrome = new ChromeController();
                Assert.Equal("Copiar (Ctrl+C)", chrome.Actions.CopyButton.ToolTip);

                UiLanguage.Apply(LanguagePreference.English);
                chrome.RefreshTexts();
                Assert.Equal("Copy (Ctrl+C)", chrome.Actions.CopyButton.ToolTip);
                Assert.Equal("Select (V)", chrome.Tools.ButtonFor(ToolKind.Select).ToolTip);
                Assert.StartsWith("Pixelation", chrome.Censor.Note);
            });
        });
    }

    [Fact]
    public void Options_ReturnsTheChosenLanguage()
    {
        StaRunner.Run(() =>
        {
            var options = new OptionsWindow(new AppSettings { Language = LanguagePreference.English });
            var accept = typeof(OptionsWindow).GetMethod("Accept", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            try { accept.Invoke(options, null); }
            catch (System.Reflection.TargetInvocationException e) when (e.InnerException is InvalidOperationException) { } // DialogResult on a window never shown
            Assert.Equal(LanguagePreference.English, options.Result?.Language);
        });
    }

    [Fact]
    public void Auto_FollowsWindowsLanguage()
    {
        var windows = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es" ? Language.Spanish : Language.English;
        WithLanguage(LanguagePreference.Auto, () => Assert.Equal(windows, UiLanguage.Current));
    }
}
