using System.Globalization;
using System.Resources;

namespace SScreentify.App.Ui;

/// <summary>Typed access to Strings.resx (written by hand: WPF markup compilation skips resx code generation).</summary>
public static class Strings
{
    private static readonly ResourceManager Resources = new("SScreentify.App.Ui.Strings", typeof(Strings).Assembly);

    public static CultureInfo? Culture { get; set; }

    public static string TrayTooltip => Get(nameof(TrayTooltip));
    public static string MenuCapture => Get(nameof(MenuCapture));
    public static string MenuOptions => Get(nameof(MenuOptions));
    public static string MenuAbout => Get(nameof(MenuAbout));
    public static string MenuExit => Get(nameof(MenuExit));
    public static string ToastCopied => Get(nameof(ToastCopied));
    public static string ToastSaved => Get(nameof(ToastSaved));
    public static string ToastCopyBusy => Get(nameof(ToastCopyBusy));
    public static string ToastSaveFailed => Get(nameof(ToastSaveFailed));
    public static string ToastHotkeyFailed => Get(nameof(ToastHotkeyFailed));
    public static string ToastSnippingConflict => Get(nameof(ToastSnippingConflict));
    public static string ButtonOpenWindowsSettings => Get(nameof(ButtonOpenWindowsSettings));
    public static string SettingsCorrupt => Get(nameof(SettingsCorrupt));
    public static string HotkeyInvalid => Get(nameof(HotkeyInvalid));
    public static string HotkeyDuplicate => Get(nameof(HotkeyDuplicate));
    public static string CensorNote => Get(nameof(CensorNote));
    public static string ToolSelect => Get(nameof(ToolSelect));
    public static string ToolPen => Get(nameof(ToolPen));
    public static string ToolLine => Get(nameof(ToolLine));
    public static string ToolArrow => Get(nameof(ToolArrow));
    public static string ToolRectangle => Get(nameof(ToolRectangle));
    public static string ToolEllipse => Get(nameof(ToolEllipse));
    public static string ToolMarker => Get(nameof(ToolMarker));
    public static string ToolText => Get(nameof(ToolText));
    public static string ToolCensor => Get(nameof(ToolCensor));
    public static string ToolColor => Get(nameof(ToolColor));
    public static string ToolUndo => Get(nameof(ToolUndo));
    public static string ToolRedo => Get(nameof(ToolRedo));
    public static string ActionCopy => Get(nameof(ActionCopy));
    public static string ActionSave => Get(nameof(ActionSave));
    public static string ActionPrint => Get(nameof(ActionPrint));
    public static string ActionClose => Get(nameof(ActionClose));
    public static string CensorPixelate => Get(nameof(CensorPixelate));
    public static string CensorBlur => Get(nameof(CensorBlur));
    public static string CensorSolid => Get(nameof(CensorSolid));

    private static string Get(string key) =>
        Resources.GetString(key, Culture) ?? throw new MissingManifestResourceException(key);
}
