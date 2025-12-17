namespace BingBox.Settings;

public class UsernameSetting : CustomStringSetting
{
    public static UsernameSetting? Instance { get; private set; }

    public UsernameSetting()
    {
        Instance = this;
        Value = Plugin.Username;
    }

    public override void ApplyValue()
    {
        if (Value != Plugin.Username)
        {
            Plugin.Username = Value;
            Plugin.Log.LogInfo($"Username Setting Applied: {Value}");
        }
    }

    public override string GetCategory() => BingBoxSettings.CategoryId.ToString();
    public override string GetDisplayName() => "Username";

    protected override string GetDefaultValue() => Plugin.Username;
}
