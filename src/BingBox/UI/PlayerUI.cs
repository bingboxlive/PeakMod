using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zorro.Core;
using Zorro.Settings;

namespace BingBox.UI;

public static class PlayerUI
{
    public static void Inject(GameObject root, RectTransform parent, object? fontAsset)
    {

        InjectAlbumArt(parent);


        InjectLogo(parent);


        if (fontAsset != null)
        {
            InjectTextInfo(parent, fontAsset);
        }


        InjectInputComponent(parent);
    }


    private static void InjectAlbumArt(RectTransform groupTransform)
    {
        var imgName = "BingBox_AlbumArt";
        var existingImg = groupTransform.Find(imgName);


        if (existingImg == null)
        {
            var imgObj = new GameObject(imgName);
            imgObj.transform.SetParent(groupTransform, false);

            var img = imgObj.AddComponent<Image>();
            var sprite = UIUtils.LoadSprite("Images.album-bg.png");
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
            }
            else
            {
                img.color = Color.magenta;
            }

            var rect = imgObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(180f, 180f);
        }
        else
        {

            var rect = existingImg.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180f, 180f);
        }


        var albumArtTransform = groupTransform.Find(imgName);
        if (albumArtTransform != null)
        {

            var bbName = "BingBox_MedBlackBox";
            if (albumArtTransform.Find(bbName) == null)
            {
                var bb = new GameObject(bbName);
                bb.transform.SetParent(albumArtTransform, false);

                bb.transform.SetAsFirstSibling();

                var bbImg = bb.AddComponent<Image>();
                bbImg.color = Color.black;


                var bbLe = bb.AddComponent<LayoutElement>();
                bbLe.ignoreLayout = true;

                var bbRt = bb.GetComponent<RectTransform>();
                bbRt.anchorMin = new Vector2(0.5f, 0.5f);
                bbRt.anchorMax = new Vector2(0.5f, 0.5f);
                bbRt.pivot = new Vector2(0.5f, 0.5f);

                bbRt.sizeDelta = new Vector2(160, 160);
                bbRt.anchoredPosition = Vector2.zero;
            }


            UIUtils.GetOrCreateMediaButton(albumArtTransform, "BingBox_PlayOverlay", "Images.play.png", new Vector2(64, 64), Vector2.zero);


            UIUtils.GetOrCreateMediaButton(albumArtTransform, "BingBox_PrevOverlay", "Images.previous.png", new Vector2(32, 32), new Vector2(-56, 0));
            UIUtils.GetOrCreateMediaButton(albumArtTransform, "BingBox_NextOverlay", "Images.next.png", new Vector2(32, 32), new Vector2(56, 0));
        }
    }

    private static void InjectLogo(RectTransform groupTransform)
    {
        var logoName = "BingBox_Logo";
        if (groupTransform.Find(logoName) != null) return;

        var logoObj = new GameObject(logoName);
        logoObj.transform.SetParent(groupTransform, false);

        var img = logoObj.AddComponent<Image>();
        img.preserveAspect = true;
        var sprite = UIUtils.LoadSprite("Images.logo.png");
        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = Color.white;
        }
        else
        {
            img.color = Color.magenta;
        }

        var rect = logoObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(190f, -10f);
        rect.sizeDelta = new Vector2(220f, 110f);
    }

    private static void InjectTextInfo(RectTransform parent, object fontAsset)
    {

        if (parent.Find("BingBox_Text_Header") != null) return;

        float xPos = 202f;
        float yStart = -60f;

        UIUtils.CreateTmpText("BingBox_Text_Header", "Currently Playing:", parent, fontAsset, 18f, new Vector2(xPos, yStart), new Color(1f, 1f, 1f, 0.5f));
        UIUtils.CreateTmpText("BingBox_Text_Title", "Dummy Title", parent, fontAsset, 32f, new Vector2(xPos, yStart - 20), Color.white);
        UIUtils.CreateTmpText("BingBox_Text_Artist", "Dummy Artist", parent, fontAsset, 24f, new Vector2(xPos, yStart - 55), new Color(1f, 1f, 1f, 0.8f));
        UIUtils.CreateTmpText("BingBox_Text_Requester", "Requested by: bing bong", parent, fontAsset, 14f, new Vector2(xPos, yStart - 85), new Color(1f, 1f, 1f, 0.5f));
    }

    private static void InjectInputComponent(RectTransform parent)
    {
        var inputName = "BingBox_Input";
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

            inputComp.text = "";
            inputComp.characterValidation = TMP_InputField.CharacterValidation.None;
            if (inputComp.placeholder is TextMeshProUGUI ph) ph.text = "Enter song request...";

            var rt = wrapper.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(0, -190);
            rt.sizeDelta = new Vector2(414, 40);

            var bgImages = wrapper.GetComponentsInChildren<Image>(true);
            foreach (var img in bgImages)
            {
                if (img.gameObject == wrapper)
                {
                    var c = img.color;
                    c.a = 0.85f;
                    img.color = c;
                }
            }

            wrapper.SetActive(true);
        }
    }


}
