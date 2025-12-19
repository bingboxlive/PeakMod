using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BingBox.Settings;
using BingBox.Utils;
using Zorro.Core;
using Zorro.Settings;
using BingBox.Audio;

namespace BingBox.UI;

public static class FooterUI
{
    public static void Inject(GameObject root, RectTransform parent, object? fontAsset)
    {

        InjectVolumeSlider(root, parent);


        InjectRoomIDInput(parent);


        InjectSyncCheckbox(parent, fontAsset);


        if (fontAsset != null)
        {
            InjectUsernameText(parent, fontAsset);
        }
    }

    private static void InjectRoomIDInput(RectTransform parent)
    {
        var inputName = "BingBox_RoomIDInput";
        if (parent.Find(inputName) != null) return;

        GameObject? prefab = null;
        var mapper = SingletonAsset<InputCellMapper>.Instance;
        if (mapper != null && mapper.FloatSettingCell != null) prefab = mapper.FloatSettingCell;

        GameObject wrapper;
        TMP_InputField? inputComp;

        if (prefab != null)
        {
            wrapper = Object.Instantiate(prefab);
            wrapper.name = inputName;
            wrapper.transform.SetParent(parent, false);

            foreach (var s in wrapper.GetComponentsInChildren<Slider>(true)) s.gameObject.SetActive(false);
            foreach (var m in wrapper.GetComponentsInChildren<MonoBehaviour>())
            {
                if (m.GetType().Name.Contains("SettingUI")) Object.Destroy(m);
            }
            inputComp = wrapper.GetComponentInChildren<TMP_InputField>(true);
        }
        else
        {
            wrapper = new GameObject(inputName);
            wrapper.transform.SetParent(parent, false);
            var bg = wrapper.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            inputComp = wrapper.AddComponent<TMP_InputField>();
        }

        if (inputComp != null)
        {
            foreach (var g in wrapper.GetComponentsInChildren<LayoutGroup>(true)) Object.Destroy(g);
            var inputRt = inputComp.GetComponent<RectTransform>();
            if (inputRt != null && inputComp.gameObject != wrapper)
            {
                inputRt.anchorMin = Vector2.zero;
                inputRt.anchorMax = Vector2.one;
                inputRt.sizeDelta = Vector2.zero;
                inputRt.anchoredPosition = Vector2.zero;
            }

            inputComp.text = RoomIdManager.CurrentRoomId;
            inputComp.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
            if (inputComp.placeholder is TextMeshProUGUI ph) ph.text = "Room ID...";

            inputComp.onEndEdit.AddListener((val) =>
            {
                if (!string.IsNullOrEmpty(val))
                {
                    RoomIdManager.SetRoomId(val);
                }
            });

            var rt = wrapper.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(0, -982);
            rt.sizeDelta = new Vector2(150, 40);

            var bgImages = wrapper.GetComponentsInChildren<Image>(true);
            foreach (var img in bgImages)
            {
                if (img.gameObject == wrapper)
                {
                    var c = img.color;
                    c.a = 0.5f;
                    img.color = c;
                }
            }
            wrapper.SetActive(true);
        }
    }

    private static void InjectSyncCheckbox(RectTransform parent, object? fontAsset)
    {
        var name = "BingBox_SyncCheckbox";
        if (parent.Find(name) != null) return;

        GameObject wrapper = new GameObject(name);
        wrapper.transform.SetParent(parent, false);
        var toggle = wrapper.AddComponent<Toggle>();

        var bg = wrapper.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        var checkObj = new GameObject("Checkmark");
        checkObj.transform.SetParent(wrapper.transform, false);
        var checkImg = checkObj.AddComponent<Image>();
        checkImg.color = Color.green;

        var checkRt = checkObj.GetComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(0.5f, 0.5f);
        checkRt.anchorMax = new Vector2(0.5f, 0.5f);
        checkRt.pivot = new Vector2(0.5f, 0.5f);
        checkRt.sizeDelta = new Vector2(20f, 20f);
        checkRt.anchoredPosition = Vector2.zero;

        toggle.targetGraphic = bg;
        toggle.graphic = checkImg;

        var groups = wrapper.GetComponentsInChildren<LayoutGroup>(true);
        foreach (var g in groups) Object.Destroy(g);

        var rt = wrapper.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);

        rt.anchoredPosition = new Vector2(160f, -988f);
        rt.sizeDelta = new Vector2(30f, 30f);

        toggle.isOn = Plugin.SyncRoomWithLobby;
        toggle.onValueChanged.AddListener((val) =>
        {
            Plugin.SyncRoomWithLobby = val;
            Plugin.Log.LogInfo($"[BingBox] SyncRoomWithLobby set to {val}");
        });

        if (fontAsset != null)
        {
            UIUtils.CreateTmpText("BingBox_SyncLabel", "Sync Room ID with Lobby", parent, fontAsset, 16f, new Vector2(200f, -988f), new Color(1f, 1f, 1f, 0.8f));
        }
        else
        {
            Plugin.Log.LogError("[FooterUI] Could not find font asset! Sync Label will be missing.");
        }

        wrapper.SetActive(true);
    }

    private static void InjectUsernameText(RectTransform parent, object fontAsset)
    {

        if (parent.Find("BingBox_Text_Username_Left") != null) return;

        var left = UIUtils.CreateTmpText("BingBox_Text_Username_Left", $"Username: {Plugin.Username} ({Plugin.UserId})", parent, fontAsset, 12f, new Vector2(5f, -1032f), new Color(1f, 1f, 1f, 0.25f));
        var right = UIUtils.CreateTmpText("BingBox_Text_Username_Right", "Change it in settings!", parent, fontAsset, 12f, new Vector2(409f, -1032f), new Color(1f, 1f, 1f, 0.25f));

        var rightTmp = right.GetComponent<TextMeshProUGUI>();
        if (rightTmp != null)
        {
            rightTmp.alignment = TextAlignmentOptions.TopRight;
            rightTmp.rectTransform.pivot = new Vector2(1, 1);
            rightTmp.rectTransform.sizeDelta = new Vector2(400, 30);
        }

        var leftTmp = left.GetComponent<TextMeshProUGUI>();
        if (leftTmp != null)
        {
            leftTmp.rectTransform.sizeDelta = new Vector2(400, 30);
        }
    }


    private static void InjectVolumeSlider(GameObject root, RectTransform parent)
    {
        var sliderName = "BingBox_Volume";
        if (parent.Find(sliderName) != null) return;


        var donorPath = "MainPage/AudioLevels/AudioLevelSlider";
        var donor = root.transform.Find(donorPath);

        if (donor == null)
        {
            Plugin.Log.LogError($"[FooterUI] Could not find Slider Donor at {donorPath}");
            return;
        }

        Plugin.Log.LogInfo($"[FooterUI] Found donor: {donor.name}. Cloning...");
        var clone = Object.Instantiate(donor.gameObject);
        clone.name = sliderName;
        clone.transform.SetParent(parent, false);

        var rt = clone.GetComponent<RectTransform>();


        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        var pos = rt.localPosition;
        pos.z = 0;
        rt.localPosition = pos;

        clone.SetActive(true);


        foreach (var comp in clone.GetComponents<LayoutGroup>()) Object.Destroy(comp);
        foreach (var comp in clone.GetComponents<ContentSizeFitter>()) Object.Destroy(comp);
        foreach (var comp in clone.GetComponents<LayoutElement>()) Object.Destroy(comp);


        var cg = clone.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.ignoreParentGroups = false;
        }


        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);

        rt.anchoredPosition = new Vector2(2f, -930f);
        rt.sizeDelta = new Vector2(402f, 30f);

        Plugin.Log.LogInfo($"[FooterUI] Slider placed at {rt.anchoredPosition} with size {rt.sizeDelta} | Active: {clone.activeSelf} | ActiveInHierarchy: {clone.activeInHierarchy}");

        foreach (var c in clone.GetComponentsInChildren<MonoBehaviour>())
        {
            if (!c.GetType().FullName.StartsWith("UnityEngine.UI") && !c.GetType().FullName.StartsWith("TMPro"))
            {
                Object.Destroy(c);
            }
        }

        var slider = clone.GetComponentInChildren<Slider>();
        if (slider != null)
        {
            var label = clone.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {

                label.gameObject.SetActive(false);
            }


            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            slider.onValueChanged.AddListener((val) =>
            {
                if (BingBoxAudioManager.Instance != null)
                {
                    BingBoxAudioManager.Instance.SetVolume(val);
                }
            });
            slider.interactable = true;
        }
    }
}
