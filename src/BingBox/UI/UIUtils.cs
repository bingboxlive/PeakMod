using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Linq;

namespace BingBox.UI;

internal static class UIUtils
{
    public static Transform? FindChildByName(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var result = FindChildByName(child, name);
            if (result != null) return result;
        }
        return null;
    }

    public static Sprite? LoadSprite(string resourceSuffix)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fullResourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith(resourceSuffix));

            if (string.IsNullOrEmpty(fullResourceName))
            {
                Plugin.Log.LogError($"[UIUtils] Resource with suffix '{resourceSuffix}' NOT FOUND.");
                return null;
            }

            using (var stream = assembly.GetManifestResourceStream(fullResourceName))
            {
                if (stream != null)
                {
                    var fileData = new byte[stream.Length];
                    stream.Read(fileData, 0, (int)stream.Length);

                    var texture = new Texture2D(2, 2);
                    if (ImageConversion.LoadImage(texture, fileData))
                    {
                        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[UIUtils] Failed to load sprite from resources: {ex}");
        }
        return null;
    }

    public static GameObject CreateTmpText(string name, string text, Transform parent, object fontAsset, float fontSize, Vector2 pos, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var tmpType = fontAsset.GetType().Assembly.GetType("TMPro.TextMeshProUGUI");

        if (tmpType == null)
        {
            Plugin.Log.LogError("Could not resolve TMPro.TextMeshProUGUI type!");
            return go;
        }

        var comp = go.AddComponent(tmpType);


        tmpType.GetProperty("font")?.SetValue(comp, fontAsset);
        tmpType.GetProperty("fontSize")?.SetValue(comp, fontSize);
        tmpType.GetProperty("text")?.SetValue(comp, text);

        tmpType.GetProperty("color")?.SetValue(comp, color);


        tmpType.GetProperty("alignment")?.SetValue(comp, 257);

        var rect = go.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0, 1);
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(400, 25);


        return go;
    }

    public static RectTransform? GetOrCreateMediaButton(Transform parent, string name, string resourceName, Vector2 size, Vector2 pos, Color? color = null)
    {
        var existing = parent.Find(name);
        if (existing == null)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var img = obj.AddComponent<Image>();
            var sprite = LoadSprite(resourceName);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = color ?? Color.white;
            }
            else
            {
                img.color = Color.red;
            }

            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }
        return existing.GetComponent<RectTransform>();
    }
}
