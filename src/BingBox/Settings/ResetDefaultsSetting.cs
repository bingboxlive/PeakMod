using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zorro.Settings;
using TMPro;

namespace BingBox.Settings;

public enum ResetOption
{
    Reset
}

public class ResetDefaultsSetting : EnumSetting<ResetOption>, IExposedSetting
{
    private GameObject? _instancePrefab;

    public override void ApplyValue()
    {
        Plugin.Log.LogInfo($"Reset Defaults Triggered? Value: {Value}");
    }

    public string GetCategory() => BingBoxSettings.CategoryId.ToString();
    public string GetDisplayName() => "Reset Defaults";

    public override List<string> GetUnlocalizedChoices()
    {
        return new List<string> { "Reset" };
    }

    public override List<UnityEngine.Localization.LocalizedString>? GetLocalizedChoices() => null;

    protected override ResetOption GetDefaultValue() => ResetOption.Reset;

    public override GameObject GetSettingUICell()
    {
        if (_instancePrefab != null) return _instancePrefab;

        var originalPrefab = base.GetSettingUICell();

        if (originalPrefab != null)
        {
            _instancePrefab = Object.Instantiate(originalPrefab);
            _instancePrefab.name = "ResetDefaults_ActionCell";
            Object.DontDestroyOnLoad(_instancePrefab);
            _instancePrefab.hideFlags = HideFlags.HideAndDontSave;

            _instancePrefab.AddComponent<ResetButtonBehaviour>();
        }

        return _instancePrefab ?? originalPrefab!;
    }
}

public class ResetButtonBehaviour : MonoBehaviour
{
    private void Start()
    {
        var allGraphics = GetComponentsInChildren<Graphic>(true);
        foreach (var g in allGraphics)
        {
            if (g is TMP_Text || g is Text)
            {
                g.color = Color.white;
                g.raycastTarget = false;
                continue;
            }

            g.color = Color.clear;
            g.raycastTarget = false;
        }

        var dropdown = GetComponentInChildren<TMP_Dropdown>(true);
        if (dropdown != null)
        {
            dropdown.transition = Selectable.Transition.None;

            var dImg = dropdown.GetComponent<Image>();
            if (dImg != null) dImg.raycastTarget = false;
        }

        var overlayObj = new GameObject("Reset_Click_Overlay");
        overlayObj.transform.SetParent(this.transform, false);

        var rt = overlayObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = overlayObj.AddComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = true;

        var btn = overlayObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(OnButtonClick);

        Plugin.Log.LogInfo("[Reset Defaults] Overlay Button Created & Ready.");
    }

    private void OnButtonClick()
    {
        Plugin.Log.LogInfo("[Reset Defaults] Button Clicked (Overlay)!");
        PerformReset();
    }

    private void PerformReset()
    {
        if (LiveUrlSetting.Instance != null)
        {
            LiveUrlSetting.Instance.SetValue("https://bingbox.live");
            LiveUrlSetting.Instance.ApplyValue();
        }
        else
        {
            Plugin.LiveUrl = "https://bingbox.live";
        }

        if (DopplerSetting.Instance != null) DopplerSetting.Instance.SetToDefault();
        if (EnableDebuggingSetting.Instance != null) EnableDebuggingSetting.Instance.SetToDefault();

        Plugin.Log.LogInfo("Internal Data Restored.");

        ForceRefreshScene();
    }

    private void ForceRefreshScene()
    {
        var customBehaviours = FindObjectsByType<CustomTextSettingBehaviour>(FindObjectsSortMode.None);
        bool urlRefreshed = false;
        foreach (var behaviour in customBehaviours)
        {
            if (behaviour.TargetSettingName == "BingBox Live URL" || behaviour.TargetSettingName == "Live URL")
            {
                behaviour.RefreshUI();
                urlRefreshed = true;
                Plugin.Log.LogInfo("[Reset] Found and Refreshed Live URL UI via Behaviour!");
            }
        }

        var allText = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        foreach (var label in allText)
        {
            if (string.IsNullOrEmpty(label.text)) continue;

            if (!urlRefreshed && (label.text.Contains("BingBox Live URL") || label.text.Contains("Live URL")))
            {
                var parent = label.transform.parent;
                var input = parent.GetComponentInChildren<TMP_InputField>(true);
                if (input == null && parent.parent != null) input = parent.parent.GetComponentInChildren<TMP_InputField>(true);

                if (input != null)
                {
                    input.text = "https://bingbox.live";
                    Plugin.Log.LogInfo("[Reset] Found Live URL via Label Search!");
                    urlRefreshed = true;
                }
            }

            if (label.text.Contains("Doppler Effect"))
            {
                RefreshDropdownSibling(label.transform, 0);
                Plugin.Log.LogInfo("[Reset] Found Doppler Label, attempting refresh...");
            }
            else if (label.text.Contains("Enable Debugging"))
            {
                RefreshDropdownSibling(label.transform, 0);
                Plugin.Log.LogInfo("[Reset] Found Debug Label, attempting refresh...");
            }
        }
    }

    private void RefreshDropdownSibling(Transform labelTransform, int targetValue)
    {
        var parent = labelTransform.parent;

        var dropdown = parent.GetComponentInChildren<TMP_Dropdown>(true);
        if (dropdown == null && parent.parent != null)
        {
            dropdown = parent.parent.GetComponentInChildren<TMP_Dropdown>(true);
        }

        if (dropdown != null)
        {
            dropdown.value = targetValue;
            dropdown.RefreshShownValue();
            Plugin.Log.LogInfo($"[Reset] Success! Reset Dropdown on {parent.name}");
        }
    }
}
