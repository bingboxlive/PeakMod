using System.Collections.Generic;
using Zorro.Settings;

namespace BingBox.Settings;

public enum DopplerOption
{
    Yes,
    No
}

public class DopplerSetting : EnumSetting<DopplerOption>, IExposedSetting
{
    public static DopplerSetting? Instance { get; private set; }

    public DopplerSetting()
    {
        Instance = this;
        Value = Plugin.DopplerConfig.Value ? DopplerOption.Yes : DopplerOption.No;
    }

    public void SetToDefault()
    {
        Plugin.DopplerConfig.Value = (bool)Plugin.DopplerConfig.DefaultValue;
        Value = Plugin.DopplerConfig.Value ? DopplerOption.Yes : DopplerOption.No;
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
                dropdown.value = (Value == DopplerOption.Yes) ? 0 : 1;
                dropdown.RefreshShownValue();
            }
        }
    }

    public override void ApplyValue()
    {
        Plugin.DopplerConfig.Value = (Value == DopplerOption.Yes);
        Plugin.Log.LogInfo($"Doppler Setting Changed: {Value}");
    }

    public string GetCategory() => BingBoxSettings.CategoryId.ToString();
    public string GetDisplayName() => "Doppler Effect";

    public override List<string> GetUnlocalizedChoices()
    {
        return new List<string> { "Yes", "No" };
    }

    public override List<UnityEngine.Localization.LocalizedString>? GetLocalizedChoices() => null;

    protected override DopplerOption GetDefaultValue() => DopplerOption.Yes;
}
