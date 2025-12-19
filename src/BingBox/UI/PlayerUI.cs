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


        if (fontAsset != null)
        {
            InjectProgressBar(parent, fontAsset);
        }
        else
        {
            InjectProgressBar(parent, null);
        }
        InjectInputComponent(parent);
    }

    private static void InjectProgressBar(RectTransform parent, object? fontAsset)
    {
        var barName = "BingBox_ProgressBar";
        if (parent.Find(barName) != null) return;

        var barObj = new GameObject(barName);
        barObj.transform.SetParent(parent, false);

        var img = barObj.AddComponent<Image>();
        img.color = new Color(0.18f, 0.18f, 0.18f);

        var rt = barObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);

        rt.anchoredPosition = new Vector2(54, -192);
        rt.sizeDelta = new Vector2(302, 8);

        var fillName = "BingBox_ProgressFill";
        if (barObj.transform.Find(fillName) == null)
        {
            var fillObj = new GameObject(fillName);
            fillObj.transform.SetParent(barObj.transform, false);

            var fillImg = fillObj.AddComponent<Image>();
            fillImg.color = Color.white;

            var fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0, 1);
            fillRt.pivot = new Vector2(0, 0.5f);
            fillRt.sizeDelta = Vector2.zero;
            fillRt.anchoredPosition = Vector2.zero;
        }

        var controller = parent.GetComponent<BingBoxPlayerUIController>();
        if (controller != null)
        {
            var fill = barObj.transform.Find(fillName);
            if (fill != null) controller.ProgressFillRect = fill.GetComponent<RectTransform>();
        }

        if (fontAsset != null)
        {

            var curTimeObj = UIUtils.CreateTmpText("BingBox_Text_CurrentTime", "00:00", parent, fontAsset, 14f, new Vector2(0, -196), new Color(1f, 1f, 1f, 0.5f));
            curTimeObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            var curTimeRt = curTimeObj.GetComponent<RectTransform>();
            curTimeRt.pivot = new Vector2(0, 0.5f);
            curTimeRt.sizeDelta = new Vector2(60, 20);
            curTimeRt.anchoredPosition = new Vector2(-6, -194);
            if (controller != null) controller.CurrentTimeText = curTimeObj.GetComponent<TextMeshProUGUI>();

            var totTimeObj = UIUtils.CreateTmpText("BingBox_Text_TotalTime", "00:00", parent, fontAsset, 14f, new Vector2(382, -196), new Color(1f, 1f, 1f, 0.5f));
            totTimeObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            var totTimeRt = totTimeObj.GetComponent<RectTransform>();
            totTimeRt.pivot = new Vector2(0, 0.5f);
            totTimeRt.sizeDelta = new Vector2(60, 20);
            totTimeRt.anchoredPosition = new Vector2(358, -194);
            if (controller != null) controller.TotalTimeText = totTimeObj.GetComponent<TextMeshProUGUI>();
        }
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
                var bbSprite = UIUtils.LoadSprite("Images.bing-bong.png");
                if (bbSprite != null)
                {
                    bbImg.sprite = bbSprite;
                    bbImg.color = Color.white;
                }
                else
                {
                    bbImg.color = Color.black;
                }


                var bbLe = bb.AddComponent<LayoutElement>();
                bbLe.ignoreLayout = true;

                var bbRt = bb.GetComponent<RectTransform>();
                bbRt.anchorMin = new Vector2(0.5f, 0.5f);
                bbRt.anchorMax = new Vector2(0.5f, 0.5f);
                bbRt.pivot = new Vector2(0.5f, 0.5f);

                bbRt.sizeDelta = new Vector2(160, 160);
                bbRt.anchoredPosition = Vector2.zero;
            }


            var btnColor = new Color(1f, 1f, 1f, 0.7f);
            var playBtnRect = UIUtils.GetOrCreateMediaButton(albumArtTransform, "BingBox_PlayOverlay", "Images.play.png", new Vector2(64, 64), Vector2.zero, btnColor);
            if (playBtnRect != null)
            {
                var playBtnObj = playBtnRect.gameObject;
                var btn = playBtnObj.GetComponent<Button>() ?? playBtnObj.AddComponent<Button>();
                var img = playBtnObj.GetComponent<Image>();
            }

            UIUtils.GetOrCreateMediaButton(albumArtTransform, "BingBox_PrevOverlay", "Images.previous.png", new Vector2(32, 32), new Vector2(-56, 0), btnColor);
            UIUtils.GetOrCreateMediaButton(albumArtTransform, "BingBox_NextOverlay", "Images.next.png", new Vector2(32, 32), new Vector2(56, 0), btnColor);
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

        var headerText = UIUtils.CreateTmpText("BingBox_Text_Header", "Currently Playing:", parent, fontAsset, 18f, new Vector2(xPos, yStart), new Color(1f, 1f, 1f, 0.5f));
        var titleText = UIUtils.CreateTmpText("BingBox_Text_Title", "Nothing Playing", parent, fontAsset, 32f, new Vector2(xPos, yStart - 20), Color.white).GetComponent<TextMeshProUGUI>();

        var artistText = UIUtils.CreateTmpText("BingBox_Text_Artist", "", parent, fontAsset, 24f, new Vector2(xPos, yStart - 55), new Color(1f, 1f, 1f, 0.8f)).GetComponent<TextMeshProUGUI>();

        var reqText = UIUtils.CreateTmpText("BingBox_Text_Requester", "Queue is empty.", parent, fontAsset, 14f, new Vector2(xPos, yStart - 85), new Color(1f, 1f, 1f, 0.5f)).GetComponent<TextMeshProUGUI>();

        var controller = parent.gameObject.AddComponent<BingBoxPlayerUIController>();
        controller.TitleText = titleText;
        controller.ArtistText = artistText;
        controller.RequesterText = reqText;

        var albumArt = parent.Find("BingBox_AlbumArt");
        if (albumArt != null)
        {
            var blackBox = albumArt.Find("BingBox_MedBlackBox");
            if (blackBox != null)
            {
                controller.AlbumArtImage = blackBox.GetComponent<Image>();
            }

            var playOverlay = albumArt.Find("BingBox_PlayOverlay");
            if (playOverlay != null)
            {
                var playImg = playOverlay.GetComponent<Image>();
                var playBtn = playOverlay.GetComponent<Button>() ?? playOverlay.gameObject.AddComponent<Button>();

                controller.PlayPauseButton = playBtn;
                controller.PlayPauseImage = playImg;
            }

            var prevOverlay = albumArt.Find("BingBox_PrevOverlay");
            if (prevOverlay != null)
            {
                var btn = prevOverlay.GetComponent<Button>() ?? prevOverlay.gameObject.AddComponent<Button>();
                controller.PrevButton = btn;
            }

            var nextOverlay = albumArt.Find("BingBox_NextOverlay");
            if (nextOverlay != null)
            {
                var btn = nextOverlay.GetComponent<Button>() ?? nextOverlay.gameObject.AddComponent<Button>();
                controller.NextButton = btn;
            }
        }

        var logo = parent.Find("BingBox_Logo");
        if (logo != null)
        {
            var btn = logo.GetComponent<Button>() ?? logo.gameObject.AddComponent<Button>();
            controller.LogoButton = btn;
        }
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

            var controller = parent.GetComponent<BingBoxPlayerUIController>();
            if (controller != null)
            {
                controller.RequestInput = inputComp;
            }
            inputComp.characterValidation = TMP_InputField.CharacterValidation.None;
            if (inputComp.placeholder is TextMeshProUGUI ph) ph.text = "Enter song request...";

            var rt = wrapper.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(0, -210);
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
