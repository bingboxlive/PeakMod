using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zorro.Settings;
using Zorro.Core;
using System.Collections.Generic;

namespace BingBox.Settings;

public abstract class CustomStringSetting : StringSetting, IExposedSetting
{
    private GameObject? _instancePrefab;

    public abstract string GetCategory();
    public abstract string GetDisplayName();
    public virtual bool IsReadOnly => false;

    public override void ApplyValue() { }

    public void SetValue(string newValue)
    {
        Value = newValue;
    }

    public override GameObject GetSettingUICell()
    {
        if (_instancePrefab != null) return _instancePrefab;

        var mapper = SingletonAsset<InputCellMapper>.Instance;
        if (mapper != null && mapper.FloatSettingCell != null)
        {
            _instancePrefab = Object.Instantiate(mapper.FloatSettingCell);
            _instancePrefab.name = $"CustomStringInputCell_{GetDisplayName()}";
            Object.DontDestroyOnLoad(_instancePrefab);
            _instancePrefab.hideFlags = HideFlags.HideAndDontSave;

            var behaviour = _instancePrefab.AddComponent<CustomTextSettingBehaviour>();

            behaviour.TargetSettingName = GetDisplayName();
            behaviour.IsReadOnly = IsReadOnly;
        }
        return _instancePrefab ?? base.GetSettingUICell();
    }

    public void RefreshUI()
    {
        if (_instancePrefab != null)
        {
            var behaviour = _instancePrefab.GetComponent<CustomTextSettingBehaviour>();
            if (behaviour != null)
            {
                behaviour.RefreshUI();
            }
        }
    }
}

public class CustomTextSettingBehaviour : MonoBehaviour
{
    public string TargetSettingName = "";
    public bool IsReadOnly = false;

    private TMP_InputField? _inputField;

    private void Start()
    {
        var sliders = GetComponentsInChildren<Slider>(true);
        foreach (var s in sliders) s.gameObject.SetActive(false);

        _inputField = GetComponentInChildren<TMP_InputField>(true);
        if (_inputField != null)
        {
            _inputField.onEndEdit.RemoveAllListeners();
            _inputField.onValueChanged.RemoveAllListeners();
            _inputField.contentType = TMP_InputField.ContentType.Standard;
            _inputField.lineType = TMP_InputField.LineType.SingleLine;
            _inputField.characterValidation = TMP_InputField.CharacterValidation.None;

            if (IsReadOnly)
            {
                _inputField.readOnly = true;

                var bg = _inputField.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
                }

                if (_inputField.textComponent != null)
                {
                    _inputField.textComponent.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
                }
            }

            FixLayout();

            SyncValue();

            _inputField.onEndEdit.AddListener(OnEndEdit);
        }
    }

    public void RefreshUI()
    {
        SyncValue();
    }

    private void SyncValue()
    {
        if (_inputField == null) return;

        if (TargetSettingName == "Username")
        {
            _inputField.text = Plugin.Username;
        }
        else if (TargetSettingName == "BingBox Live URL")
        {
            _inputField.text = Plugin.LiveUrl;
        }
        else if (TargetSettingName == "User ID")
        {
            _inputField.text = Plugin.UserId;
        }
        else
        {
            Plugin.Log.LogWarning($"[CustomString] Unknown TargetSettingName: '{TargetSettingName}'");
        }
    }


    private void FixLayout()
    {
        if (_inputField == null) return;

        var parentWrapper = _inputField.transform.parent;
        if (parentWrapper != null)
        {
            var parentGroup = parentWrapper.GetComponent<LayoutGroup>();
            if (parentGroup == null)
            {
                var hg = parentWrapper.gameObject.AddComponent<HorizontalLayoutGroup>();
                hg.childControlWidth = true;
                hg.childControlHeight = true;
                hg.childForceExpandWidth = true;
                hg.childForceExpandHeight = true;
                hg.padding = new RectOffset(12, 12, 16, 16);
            }
            else
            {
                parentGroup.childAlignment = TextAnchor.MiddleCenter;
                if (parentGroup is HorizontalOrVerticalLayoutGroup hov)
                {
                    hov.childControlWidth = true;
                    hov.childForceExpandWidth = true;
                }
            }
        }

        var layout = _inputField.GetComponent<LayoutElement>();
        if (layout == null) layout = _inputField.gameObject.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.minWidth = 100f;
    }

    private void OnEndEdit(string newVal)
    {
        if (TargetSettingName == "Username")
        {
            if (UsernameSetting.Instance != null)
            {
                UsernameSetting.Instance.SetValue(newVal);
                UsernameSetting.Instance.ApplyValue();
            }
            else
            {
                Plugin.Username = newVal;
            }
            if (Plugin.DebugConfig.Value)
            {
                Plugin.Log.LogInfo($"[CustomString] {TargetSettingName} updated: {newVal}");
            }
        }
        else if (TargetSettingName == "BingBox Live URL")
        {
            if (LiveUrlSetting.Instance != null)
            {
                LiveUrlSetting.Instance.SetValue(newVal);
                LiveUrlSetting.Instance.ApplyValue();
            }
            else
            {
                Plugin.LiveUrl = newVal;
            }
            if (Plugin.DebugConfig.Value)
            {
                Plugin.Log.LogInfo($"[CustomString] LiveUrl updated: {newVal}");
            }
        }
    }
}
