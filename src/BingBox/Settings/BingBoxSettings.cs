using System;
using System.Linq;

namespace BingBox.Settings;

public static class BingBoxSettings
{
    public const string CategoryName = "BINGBOX";
    public static SettingsCategory CategoryId { get; private set; }
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;

        var values = Enum.GetValues(typeof(SettingsCategory)).Cast<int>().ToList();
        int maxId = values.Any() ? values.Max() : 0;

        CategoryId = (SettingsCategory)999;

        _initialized = true;
    }
}
