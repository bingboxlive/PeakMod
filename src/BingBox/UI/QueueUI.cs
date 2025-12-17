using UnityEngine;
using UnityEngine.UI;

namespace BingBox.UI;

public static class QueueUI
{
    private static Sprite? _circleSprite;

    public static void Inject(RectTransform parent, object? fontAsset)
    {

        if (fontAsset != null)
        {
            var name = "BingBox_Text_UpNext";
            if (parent.Find(name) == null)
            {
                UIUtils.CreateTmpText(name, "Up Next", parent, fontAsset, 16f, new Vector2(5f, -240f), new Color(1f, 1f, 1f, 0.5f));
            }
        }


        InjectQueueScrollView(parent, fontAsset);
    }

    private static void InjectQueueScrollView(RectTransform parent, object? fontAsset)
    {
        var scrollObjName = "BingBox_QueueScrollView";
        if (parent.Find(scrollObjName) != null) return;

        Plugin.Log.LogInfo("[QueueUI] Injecting Queue ScrollView...");


        var scrollObj = new GameObject(scrollObjName);
        scrollObj.transform.SetParent(parent, false);

        var bg = scrollObj.AddComponent<Image>();
        bg.color = Color.clear;

        var sr = scrollObj.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 20f;

        var rt = scrollObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(0f, -260f);
        rt.sizeDelta = new Vector2(414f, 650f);


        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);

        var viewportRt = viewport.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.sizeDelta = Vector2.zero;
        viewportRt.pivot = new Vector2(0, 1);

        var viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = new Color(0, 0, 0, 0);
        var mask = viewport.AddComponent<RectMask2D>();

        sr.viewport = viewportRt;


        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);

        var contentRt = content.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0, 1);
        contentRt.sizeDelta = new Vector2(0, 300);

        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 12f;
        layout.padding = new RectOffset(0, 0, 0, 4);

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.content = contentRt;


        var scrollbarObj = new GameObject("Scrollbar Vertical");
        scrollbarObj.transform.SetParent(scrollObj.transform, false);

        var sbImage = scrollbarObj.AddComponent<Image>();
        sbImage.color = Color.clear;

        var sb = scrollbarObj.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        var sbRt = scrollbarObj.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(1, 0);
        sbRt.anchorMax = new Vector2(1, 1);
        sbRt.pivot = new Vector2(1, 1);
        sbRt.sizeDelta = new Vector2(10, 0);
        sbRt.anchoredPosition = Vector2.zero;


        var slidingArea = new GameObject("Sliding Area");
        slidingArea.transform.SetParent(scrollbarObj.transform, false);
        var slRt = slidingArea.AddComponent<RectTransform>();
        slRt.anchorMin = Vector2.zero;
        slRt.anchorMax = Vector2.one;
        slRt.sizeDelta = Vector2.zero;


        var handle = new GameObject("Handle");
        handle.transform.SetParent(slidingArea.transform, false);
        var hImg = handle.AddComponent<Image>();
        hImg.color = Color.clear;

        var hRt = handle.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(0, 0);


        var barColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        float width = 10f;
        float radius = width / 2f;


        var midBar = new GameObject("Mid");
        midBar.transform.SetParent(handle.transform, false);
        var mImg = midBar.AddComponent<Image>();
        mImg.color = barColor;
        var mRt = midBar.GetComponent<RectTransform>();
        mRt.anchorMin = Vector2.zero;
        mRt.anchorMax = Vector2.one;
        mRt.sizeDelta = new Vector2(0, -width);
        mRt.anchoredPosition = Vector2.zero;


        var topCap = new GameObject("Top");
        topCap.transform.SetParent(handle.transform, false);
        var tImg = topCap.AddComponent<Image>();
        tImg.sprite = GetCircleSprite();
        tImg.color = barColor;
        var tRt = topCap.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0, 1);
        tRt.anchorMax = new Vector2(1, 1);
        tRt.pivot = new Vector2(0.5f, 0.5f);
        tRt.sizeDelta = new Vector2(0, width);
        tRt.anchoredPosition = new Vector2(0, -radius);


        var botCap = new GameObject("Bot");
        botCap.transform.SetParent(handle.transform, false);
        var bImg = botCap.AddComponent<Image>();
        bImg.sprite = GetCircleSprite();
        bImg.color = barColor;
        var bRt = botCap.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0, 0);
        bRt.anchorMax = new Vector2(1, 0);
        bRt.pivot = new Vector2(0.5f, 0.5f);
        bRt.sizeDelta = new Vector2(0, width);
        bRt.anchoredPosition = new Vector2(0, radius);

        sb.handleRect = hRt;
        sb.targetGraphic = hImg;
        sr.verticalScrollbar = sb;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;


        InjectDummyData(contentRt, fontAsset);
    }

    private static Sprite GetCircleSprite()
    {
        if (_circleSprite != null) return _circleSprite;

        int res = 64;
        var tex = new Texture2D(res, res);
        var colors = new Color[res * res];
        float center = res / 2f;
        float rSq = (res / 2f - 1) * (res / 2f - 1);

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dSq = (x - center) * (x - center) + (y - center) * (y - center);
                float alpha = dSq < rSq ? 1f : 0f;
                if (dSq > rSq && dSq < rSq + 2f) alpha = 0.5f;
                colors[y * res + x] = new Color(1, 1, 1, alpha);
            }
        }
        tex.SetPixels(colors);
        tex.Apply();
        _circleSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        return _circleSprite;
    }

    private static void InjectDummyData(RectTransform content, object? fontAsset)
    {

        var spacer = new GameObject("TopSpacer");
        spacer.transform.SetParent(content, false);
        var le = spacer.AddComponent<LayoutElement>();
        le.minHeight = 12f;
        le.preferredHeight = 12f;

        for (int i = 0; i < 10; i++)
        {
            CreateQueueItem(content, fontAsset,
                $"Song Title {i + 1}",
                $"Artist Name {i + 1}",
                $"User{i + 99}");
        }
    }

    private static void CreateQueueItem(RectTransform parent, object? fontAsset, string title, string artist, string requester)
    {
        var itemObj = new GameObject("QueueItem");
        itemObj.transform.SetParent(parent, false);

        var img = itemObj.AddComponent<Image>();
        img.color = Color.clear;

        var itemRt = itemObj.GetComponent<RectTransform>();
        var layoutEl = itemObj.AddComponent<LayoutElement>();

        layoutEl.minHeight = 80f;
        layoutEl.preferredHeight = 80f;


        var albumBg = new GameObject("AlbumArt");
        albumBg.transform.SetParent(itemObj.transform, false);
        var aImg = albumBg.AddComponent<Image>();

        var subBg = UIUtils.LoadSprite("Images.album-bg-sub.png");
        if (subBg != null)
        {
            aImg.sprite = subBg;
            aImg.color = Color.white;
        }
        else
        {
            aImg.color = Color.gray;
        }

        var aRt = albumBg.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0, 0.5f);
        aRt.anchorMax = new Vector2(0, 0.5f);
        aRt.pivot = new Vector2(0, 0.5f);

        aRt.sizeDelta = new Vector2(80, 80);
        aRt.anchoredPosition = new Vector2(0f, 0f);


        var blackBox = new GameObject("BlackBox");
        blackBox.transform.SetParent(albumBg.transform, false);
        var bbImg = blackBox.AddComponent<Image>();
        bbImg.color = Color.black;
        var bbRt = blackBox.GetComponent<RectTransform>();
        bbRt.anchorMin = new Vector2(0.5f, 0.5f);
        bbRt.anchorMax = new Vector2(0.5f, 0.5f);
        bbRt.pivot = new Vector2(0.5f, 0.5f);
        bbRt.sizeDelta = new Vector2(68, 68);
        bbRt.anchoredPosition = Vector2.zero;


        if (fontAsset != null)
        {

            UIUtils.CreateTmpText("Title", title, itemObj.transform, fontAsset, 20f, new Vector2(86f, 0f), Color.white);
            UIUtils.CreateTmpText("Artist", artist, itemObj.transform, fontAsset, 16f, new Vector2(86f, -20f), new Color(0.8f, 0.8f, 0.8f, 1f));
            UIUtils.CreateTmpText("Requester", $"Req: {requester}", itemObj.transform, fontAsset, 12f, new Vector2(86f, -38f), new Color(1f, 1f, 1f, 0.5f));


            UIUtils.CreateTmpText("Timestamp", "3:45", itemObj.transform, fontAsset, 12f, new Vector2(86f, -56f), new Color(1f, 1f, 1f, 0.3f));


            var xObj = UIUtils.CreateTmpText("RemoveButton", "X", itemObj.transform, fontAsset, 24f, new Vector2(-20f, 8f), Color.red);
            var xRt = xObj.GetComponent<RectTransform>();
            xRt.anchorMin = new Vector2(1, 0.5f);
            xRt.anchorMax = new Vector2(1, 0.5f);
            xRt.pivot = new Vector2(1, 0.5f);
            xRt.sizeDelta = new Vector2(40, 40);
            xRt.anchoredPosition = new Vector2(-20f, 8f);
        }
    }
}
