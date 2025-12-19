using UnityEngine;
using TMPro;

namespace BingBox.UI;

public class PauseMenuInjector : MonoBehaviour
{
    private float _lastInjectTime = 0f;
    private const float INJECT_INTERVAL = 1.0f;

    private void Update()
    {
        if (Time.time - _lastInjectTime > INJECT_INTERVAL)
        {
            _lastInjectTime = Time.time;
            InjectPauseMenuUI();
        }
    }

    private GameObject? _cachedPauseMenu;

    private void InjectPauseMenuUI()
    {


        if (_cachedPauseMenu == null)
        {
            _cachedPauseMenu = GameObject.Find("PauseMenu");
        }

        if (_cachedPauseMenu != null && _cachedPauseMenu.activeInHierarchy)
        {
            var pauseMenu = _cachedPauseMenu;

            var targetParent = pauseMenu.transform;


            var groupName = "BingBox_UI_Group";
            var groupTransform = targetParent.Find(groupName) as RectTransform;
            GameObject groupObj;

            if (groupTransform == null)
            {

                var oldBox = targetParent.Find("BingBox_BrownBox");
                if (oldBox != null) Destroy(oldBox.gameObject);

                if (Plugin.DebugConfig.Value)
                {
                    Plugin.Log.LogInfo("[PauseMenuInjector] Creating BingBox UI Group...");
                }
                groupObj = new GameObject(groupName);
                groupObj.transform.SetParent(targetParent, false);

                groupTransform = groupObj.AddComponent<RectTransform>();


                groupTransform.anchorMin = new Vector2(0, 1);
                groupTransform.anchorMax = new Vector2(0, 1);
                groupTransform.pivot = new Vector2(0, 1);


                groupTransform.anchoredPosition = new Vector2(20f, -20f);

                groupTransform.sizeDelta = new Vector2(500f, 1100f);

                groupTransform.SetAsLastSibling();


                _hasInjectedUI = false;
                if (Plugin.DebugConfig.Value)
                {
                    Plugin.Log.LogInfo("[PauseMenuInjector] Resetting Injection Flag for new Menu.");
                }
            }
            else
            {
                groupObj = groupTransform.gameObject;
            }


            bool shouldShow = true;


            var internalMainPage = pauseMenu.transform.Find("MainPage/MainPage");
            if (internalMainPage != null)
            {
                if (!internalMainPage.gameObject.activeInHierarchy)
                {
                    shouldShow = false;
                }
            }
            else
            {

            }

            var settingsPage = pauseMenu.transform.Find("SettingsPage");
            var controlsPage = pauseMenu.transform.Find("ControlsPage");
            var accoladesPage = pauseMenu.transform.Find("AccoladesPage");
            var rebindPage = pauseMenu.transform.Find("RebindKeyPage");

            bool subPageActive = (settingsPage != null && settingsPage.gameObject.activeInHierarchy) ||
                                 (controlsPage != null && controlsPage.gameObject.activeInHierarchy) ||
                                 (accoladesPage != null && accoladesPage.gameObject.activeInHierarchy) ||
                                 (rebindPage != null && rebindPage.gameObject.activeInHierarchy);

            if (subPageActive)
            {
                shouldShow = false;
            }

            if (groupObj.activeSelf != shouldShow)
            {
                if (Plugin.DebugConfig.Value)
                {
                    Plugin.Log.LogInfo($"[PauseMenuInjector] Setting Group Active: {shouldShow}");
                }
                groupObj.SetActive(shouldShow);
            }

            if (shouldShow && !_hasInjectedUI)
            {
                if (Plugin.DebugConfig.Value)
                {
                    Plugin.Log.LogInfo("[PauseMenuInjector] Logic says SHOW. Attempting Injection...");
                }



                var donor = UIUtils.FindChildByName(pauseMenu.transform, "Title");


                if (donor == null)
                {
                    if (Plugin.DebugConfig.Value)
                    {
                        Plugin.Log.LogInfo("[PauseMenuInjector] 'Title' not found. Searching for fallback text...");
                    }
                    var allText = pauseMenu.GetComponentsInChildren<TextMeshProUGUI>(true);
                    if (allText.Length > 0)
                    {
                        donor = allText[0].transform;
                        if (Plugin.DebugConfig.Value)
                        {
                            Plugin.Log.LogInfo($"[PauseMenuInjector] Found fallback text: {donor.name}");
                        }
                    }
                }

                object? fontAsset = null;
                if (donor != null)
                {
                    var donorComp = donor.GetComponent("TMPro.TextMeshProUGUI");
                    if (donorComp != null)
                    {
                        fontAsset = donorComp.GetType().GetProperty("font")?.GetValue(donorComp);
                    }
                }

                if (fontAsset == null)
                {
                    Plugin.Log.LogError("[PauseMenuInjector] Failed to find Font Asset! Injection aborted.");
                }
                else
                {
                    if (Plugin.DebugConfig.Value)
                    {
                        Plugin.Log.LogInfo("[PauseMenuInjector] Font Asset Found. Injecting Child Components...");
                    }

                    PlayerUI.Inject(pauseMenu, groupTransform, fontAsset);
                    QueueUI.Inject(groupTransform, fontAsset);
                    FooterUI.Inject(pauseMenu, groupTransform, fontAsset);

                    _hasInjectedUI = true;
                    if (Plugin.DebugConfig.Value)
                    {
                        Plugin.Log.LogInfo("[PauseMenuInjector] UI Injected Successfully.");
                    }
                }
            }
        }
    }

    private bool _hasInjectedUI = false;

    private string GetPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return GetPath(t.parent) + "/" + t.name;
    }
}
