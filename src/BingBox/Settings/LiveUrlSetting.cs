namespace BingBox.Settings;

public class LiveUrlSetting : CustomStringSetting
{
    public static LiveUrlSetting? Instance { get; private set; }

    public LiveUrlSetting()
    {
        Instance = this;
        Value = Plugin.LiveUrl;
    }

    public override void ApplyValue()
    {
        if (Value != Plugin.LiveUrl)
        {
            Plugin.LiveUrl = Value;
            Plugin.Log.LogInfo($"Live URL Setting Applied: {Value}");
        }
    }

    public override string GetCategory() => BingBoxSettings.CategoryId.ToString();
    public override string GetDisplayName() => "BingBox Live URL";

    protected override string GetDefaultValue() => Plugin.LiveUrl;
}
