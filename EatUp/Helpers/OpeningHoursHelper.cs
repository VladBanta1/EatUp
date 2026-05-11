using System.Text.Json;

namespace EatUp.Helpers;

public static class OpeningHoursHelper
{
    private static readonly string[] Days =
        { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    public static bool IsOpenNow(string? json)
    {
        if (string.IsNullOrEmpty(json)) return true;
        try
        {
            var now = DateTime.Now;
            var dayName = Days[(int)now.DayOfWeek];
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(dayName, out var dayEl)) return false;
            if (dayEl.TryGetProperty("IsClosed", out var cl) && cl.GetBoolean()) return false;
            if (!dayEl.TryGetProperty("Open", out var opEl) ||
                !dayEl.TryGetProperty("Close", out var clEl)) return true;
            var open = TimeSpan.Parse(opEl.GetString()!);
            var close = TimeSpan.Parse(clEl.GetString()!);
            return now.TimeOfDay >= open && now.TimeOfDay <= close;
        }
        catch { return true; }
    }

    public static string GetScheduleSummary(string? json)
    {
        if (string.IsNullOrEmpty(json)) return "Program nedefinit";
        try
        {
            using var doc = JsonDocument.Parse(json);
            var today = Days[(int)DateTime.Now.DayOfWeek];
            if (!doc.RootElement.TryGetProperty(today, out var dayEl)) return "Închis azi";
            if (dayEl.TryGetProperty("IsClosed", out var cl) && cl.GetBoolean()) return "Închis azi";
            if (!dayEl.TryGetProperty("Open", out var opEl) ||
                !dayEl.TryGetProperty("Close", out var clEl)) return "Program nedefinit";
            return $"{opEl.GetString()} – {clEl.GetString()}";
        }
        catch { return ""; }
    }
}
