using Zorro.Settings;

namespace BingBox.Settings;

public class EnableDebuggingSetting : BoolSetting, IExposedSetting
{
    public static EnableDebuggingSetting? Instance { get; private set; }

    public EnableDebuggingSetting()
    {
        Instance = this;
        Value = Plugin.DebugConfig.Value;
    }

    public void SetToDefault()
    {
        Plugin.DebugConfig.Value = (bool)Plugin.DebugConfig.DefaultValue;
        Value = Plugin.DebugConfig.Value;
        ApplyValue();
        RefreshUI();
    }

    public void RefreshUI()
    {
        var cell = GetSettingUICell();
        if (cell != null)
        {
            var dropdown = cell.GetComponentInChildren<TMPro.TMP_Dropdown>();
            if (dropdown != null)
            {
                dropdown.value = Value ? 0 : 1;
                dropdown.RefreshShownValue();
            }
        }
    }

    public override void ApplyValue()
    {
        Plugin.DebugConfig.Value = Value;
        if (Plugin.DebugConfig.Value)
        {
            Plugin.Log.LogInfo($"Debugging Enabled: {Value}");
        }
    }

    public string GetCategory() => BingBoxSettings.CategoryId.ToString();
    public string GetDisplayName() => "Enable Debugging";

    protected override bool GetDefaultValue() => true;

    public override UnityEngine.Localization.LocalizedString OnString => new UnityEngine.Localization.LocalizedString("Settings", "On");
    public override UnityEngine.Localization.LocalizedString OffString => new UnityEngine.Localization.LocalizedString("Settings", "Off");
}
