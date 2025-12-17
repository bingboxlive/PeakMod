using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using HarmonyLib;
using BingBox.Logging;

namespace BingBox.Settings;

public class SettingsInjector : MonoBehaviour
{
    private Coroutine? _updateTabsRoutine;
    private GameObject? _buttonSource;
    private static LoggerWrapper? _log;

    private void Awake()
    {
        _log = Plugin.Log;
        SceneManager.sceneLoaded += OnSceneLoaded;

        var harmony = new Harmony("pro.kenn.bingbox.settings");
        harmony.PatchAll(typeof(SettingsInjector));
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_updateTabsRoutine != null)
        {
            StopCoroutine(_updateTabsRoutine);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _buttonSource = null;
        if (_updateTabsRoutine != null)
        {
            StopCoroutine(_updateTabsRoutine);
        }
        _updateTabsRoutine = StartCoroutine(UpdateTabs(scene));
    }

    private IEnumerator UpdateTabs(Scene scene)
    {
        _log?.LogInfo($"[SettingsInjector] Started searching for tabs in scene: {scene.name}");
        int loopCount = 0;

        while (_buttonSource == null)
        {
            loopCount++;
            if (loopCount % 50 == 0)
            {
                _log?.LogWarning($"[SettingsInjector] Still searching for Settings Tabs in '{scene.name}'... (It's been {loopCount * 0.1f:F1}s)");
            }

            var buttons = FindTabsButtons(scene);
            if (buttons.Count > 0)
            {
                _buttonSource = buttons[0].gameObject;
            }
            yield return new WaitForSeconds(0.1f);
        }

        if (_buttonSource != null)
        {
            _log?.LogInfo("Found Settings Tab Source. Injecting BingBox tab...");

            GameObject newButton = Instantiate(_buttonSource, _buttonSource.transform.parent);
            newButton.name = "BingBox";

            var loc = newButton.GetComponentInChildren<LocalizedText>();
            if (loc != null) Destroy(loc);

            var tabsButton = newButton.GetComponent<SettingsTABSButton>();
            if (tabsButton != null)
            {
                BingBoxSettings.Initialize();
                tabsButton.category = BingBoxSettings.CategoryId;

                if (tabsButton.text != null)
                {
                    tabsButton.text.text = BingBoxSettings.CategoryName;
                }
            }

            newButton.transform.SetSiblingIndex(newButton.transform.parent.childCount - 2);
        }
    }

    private List<SettingsTABSButton> FindTabsButtons(Scene scene)
    {
        var result = new List<SettingsTABSButton>();
        foreach (var root in scene.GetRootGameObjects())
        {
            result.AddRange(root.GetComponentsInChildren<SettingsTABSButton>(true));
        }
        return result;
    }

    private static readonly HashSet<string> _customKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "BINGBOX",
        "Doppler Effect",
        "Enable Debugging",
        "BingBox Live URL",
        "Username",
        "User ID"
    };

    [HarmonyPatch(typeof(LocalizedText), "GetText", new Type[] { typeof(string), typeof(bool) })]
    [HarmonyPrefix]
    public static bool GetTextPrefixOverload(string id, bool printDebug, ref string __result)
    {
        if (_customKeys.Contains(id))
        {
            __result = id;
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(LocalizedText), "GetText", new Type[] { typeof(string), typeof(bool) })]
    [HarmonyPostfix]
    public static void GetTextPostfix(string id, ref string __result)
    {
    }
}
