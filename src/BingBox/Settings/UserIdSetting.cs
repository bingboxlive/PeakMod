namespace BingBox.Settings;

public class UserIdSetting : CustomStringSetting
{
    public static UserIdSetting? Instance { get; private set; }

    public UserIdSetting()
    {
        Instance = this;
        Value = Plugin.UserId;
    }

    public override void ApplyValue()
    {
    }

    public override string GetCategory() => BingBoxSettings.CategoryId.ToString();
    public override string GetDisplayName() => "User ID";

    public override bool IsReadOnly => true;

    protected override string GetDefaultValue() => Plugin.UserId;
}
