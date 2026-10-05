using System.Runtime.InteropServices;

namespace Flicker.Mac;

// CGEvent requires the user to approve Accessibility access for Flicker in macOS settings.
public static class MacInput
{
    private const uint HidEventTap = 0;
    private const uint LeftDown = 1, LeftUp = 2, RightDown = 3, RightUp = 4, OtherDown = 25, OtherUp = 26;

    [StructLayout(LayoutKind.Sequential)] private struct CGPoint { public double X, Y; public CGPoint(double x, double y) { X = x; Y = y; } }
    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")] private static extern IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort keyCode, bool keyDown);
    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")] private static extern IntPtr CGEventCreateMouseEvent(IntPtr source, uint type, CGPoint point, uint button);
    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")] private static extern void CGEventPost(uint tap, IntPtr @event);
    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")] private static extern void CFRelease(IntPtr value);
    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")] private static extern bool AXIsProcessTrusted();

    private static readonly Dictionary<string, ushort> KeyCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["A"] = 0, ["S"] = 1, ["D"] = 2, ["F"] = 3, ["H"] = 4, ["G"] = 5, ["Z"] = 6, ["X"] = 7, ["C"] = 8, ["V"] = 9,
        ["B"] = 11, ["Q"] = 12, ["W"] = 13, ["E"] = 14, ["R"] = 15, ["Y"] = 16, ["T"] = 17, ["D1"] = 18, ["D2"] = 19,
        ["D3"] = 20, ["D4"] = 21, ["D6"] = 22, ["D5"] = 23, ["Oemplus"] = 24, ["D9"] = 25, ["D7"] = 26, ["OemMinus"] = 27,
        ["D8"] = 28, ["D0"] = 29, ["Oem4"] = 33, ["Oem6"] = 30, ["Oem7"] = 39, ["Oem5"] = 42, ["Oemcomma"] = 43, ["OemPeriod"] = 47,
        ["Oem2"] = 44, ["Oem1"] = 41, ["Oemtilde"] = 50, ["Space"] = 49, ["Enter"] = 36, ["Tab"] = 48, ["Escape"] = 53,
        ["Back"] = 51, ["Delete"] = 117, ["Left"] = 123, ["Right"] = 124, ["Down"] = 125, ["Up"] = 126,
        ["F1"] = 122, ["F2"] = 120, ["F3"] = 99, ["F4"] = 118, ["F5"] = 96, ["F6"] = 97, ["F7"] = 98, ["F8"] = 100, ["F9"] = 101,
        ["F10"] = 109, ["F11"] = 103, ["F12"] = 111
    };

    public static bool HasAccessibilityPermission() => OperatingSystem.IsMacOS() && AXIsProcessTrusted();

    public static void Press(Rule rule)
    {
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException("Wysyłanie wejścia jest dostępne wyłącznie na macOS.");
        if (!HasAccessibilityPermission()) throw new InvalidOperationException("Nadaj Flickerowi uprawnienie Dostępność w Ustawieniach systemowych macOS.");
        if (rule.Key.StartsWith("Mysz:", StringComparison.Ordinal)) Mouse(rule); else Keyboard(rule.Key, rule.HoldMs);
    }

    private static void Keyboard(string key, int holdMs)
    {
        key = key switch
        {
            "=" => "Oemplus", "-" => "OemMinus", "," => "Oemcomma", "." => "OemPeriod", "/" => "Oem2",
            "[" => "Oem4", "]" => "Oem6", ";" => "Oem1", "'" => "Oem7", "\\" => "Oem5", "`" => "Oemtilde",
            _ => key
        };
        if (!KeyCodes.TryGetValue(key, out var code)) throw new InvalidOperationException($"Nieznany klawisz macOS: {key}");
        Post(CGEventCreateKeyboardEvent(IntPtr.Zero, code, true));
        Thread.Sleep(Math.Clamp(holdMs, 1, 5000));
        Post(CGEventCreateKeyboardEvent(IntPtr.Zero, code, false));
    }

    private static void Mouse(Rule rule)
    {
        var (down, up, button) = rule.Key switch { "Mysz: prawy" => (RightDown, RightUp, 1u), "Mysz: środkowy" => (OtherDown, OtherUp, 2u), _ => (LeftDown, LeftUp, 0u) };
        var point = new CGPoint(rule.MouseX, rule.MouseY);
        Post(CGEventCreateMouseEvent(IntPtr.Zero, down, point, button));
        Thread.Sleep(Math.Clamp(rule.HoldMs, 1, 5000));
        Post(CGEventCreateMouseEvent(IntPtr.Zero, up, point, button));
    }

    private static void Post(IntPtr @event) { if (@event == IntPtr.Zero) throw new InvalidOperationException("macOS nie utworzył zdarzenia wejścia."); try { CGEventPost(HidEventTap, @event); } finally { CFRelease(@event); } }
}
