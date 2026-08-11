using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoffeeShop
{
    [DisallowMultipleComponent]
    public sealed class PlacementProgressUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Vector2 hudSize = new Vector2(438f, 164f);
        [SerializeField] private Vector2 hudOffset = new Vector2(28f, -28f);
        [SerializeField, Range(0.5f, 1f)] private float minimumScale = 0.72f;
        [SerializeField, Range(1f, 1.5f)] private float maximumScale = 1.15f;

        private readonly Color ink = new Color(0.006f, 0.012f, 0.018f, 1f);
        private readonly Color cream = new Color(0.97f, 0.91f, 0.75f, 1f);
        private readonly Color paper = new Color(0.96f, 0.96f, 0.91f, 1f);
        private readonly Color goldBright = new Color(0.9f, 0.43f, 0.065f, 1f);
        private readonly Color espressoDark = new Color(0.012f, 0.004f, 0.003f, 1f);
        private readonly Color cherry = new Color(0.20f, 0.018f, 0.014f, 1f);
        private readonly Color cherryBright = new Color(0.38f, 0.045f, 0.028f, 1f);
        private readonly Color teal = new Color(0.018f, 0.20f, 0.22f, 1f);
        private readonly Color tealBright = new Color(0.035f, 0.36f, 0.38f, 1f);
        private readonly Color success = new Color(0.07f, 0.48f, 0.17f, 1f);

        private RectTransform hudRoot;
        private Image fillImage;
        private Image fillHighlightImage;
        private TMP_Text countText;
        private TMP_Text timerText;
        private int cachedScreenWidth;
        private int cachedScreenHeight;
        private int displayedElapsedSecond = -1;

        private Sprite circleSprite;
        private static Sprite whiteSprite;

        private void Awake()
        {
            BuildUi();
            UpdateHudScale();
            RefreshProgress();
            RefreshTimer(true);
        }

        private void OnEnable()
        {
            PlaceableObject.ProgressChanged += RefreshProgress;
            RefreshProgress();
            RefreshTimer(true);
        }

        private void OnDisable()
        {
            PlaceableObject.ProgressChanged -= RefreshProgress;
        }

        private void Update()
        {
            if (Screen.width != cachedScreenWidth || Screen.height != cachedScreenHeight)
            {
                UpdateHudScale();
            }

            RefreshTimer(false);
        }

        private void BuildUi()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            GameObject root = CreateUiObject("CoffeeShopHud", canvas.transform);
            hudRoot = root.GetComponent<RectTransform>();
            SetRect(
                hudRoot,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                hudOffset,
                hudSize);

            CreateCenteredLayer("HudShadow", root.transform, new Vector2(8f, -10f), hudSize, new Color(0f, 0f, 0f, 0.72f));

            GameObject frame = CreateCenteredLayer("CreamFrame", root.transform, Vector2.zero, hudSize, cream);
            AddOutline(frame, espressoDark, 3f);

            Vector2 faceSize = hudSize - new Vector2(14f, 14f);
            GameObject face = CreateCenteredLayer("CherryFace", root.transform, Vector2.zero, faceSize, cherry);
            AddOutline(face, espressoDark, 2f);

            BuildAwning(face.transform);
            BuildHeader(face.transform);
            BuildTimer(face.transform);
            BuildProgressBar(face.transform);

            float halfWidth = hudSize.x * 0.5f;
            float halfHeight = hudSize.y * 0.5f;
            CreateBolt(root.transform, new Vector2(-halfWidth + 11f, halfHeight - 11f));
            CreateBolt(root.transform, new Vector2(halfWidth - 11f, halfHeight - 11f));
            CreateBolt(root.transform, new Vector2(-halfWidth + 11f, -halfHeight + 11f));
            CreateBolt(root.transform, new Vector2(halfWidth - 11f, -halfHeight + 11f));
        }

        private void BuildAwning(Transform parent)
        {
            GameObject awning = CreateUiObject("CoffeeAwning", parent);
            SetRect(
                awning.GetComponent<RectTransform>(),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                new Vector2(0f, 17f));

            const int segmentCount = 12;
            for (int index = 0; index < segmentCount; index++)
            {
                GameObject segment = CreateUiObject("AwningStripe" + index, awning.transform);
                RectTransform segmentRect = segment.GetComponent<RectTransform>();
                segmentRect.anchorMin = new Vector2(index / (float)segmentCount, 0f);
                segmentRect.anchorMax = new Vector2((index + 1f) / segmentCount, 1f);
                segmentRect.offsetMin = Vector2.zero;
                segmentRect.offsetMax = Vector2.zero;

                Image image = segment.AddComponent<Image>();
                image.sprite = GetWhiteSprite();
                image.color = index % 2 == 0 ? cream : cherryBright;
                image.raycastTarget = false;
            }

            GameObject awningEdge = CreateUiObject("AwningGoldEdge", parent);
            SetRect(
                awningEdge.GetComponent<RectTransform>(),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -17f),
                new Vector2(0f, 3f));
            CreateImage(awningEdge, goldBright);
        }

        private void BuildHeader(Transform parent)
        {
            TMP_Text title = CreateText(
                "ProgressTitle",
                parent,
                "PLACEMENT PROGRESS",
                15f,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(20f, -24f),
                new Vector2(250f, 30f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                paper);
            title.characterSpacing = 1.8f;
            AddTextShadow(title, new Color(0f, 0f, 0f, 0.8f), new Vector2(2f, -2f));

            GameObject countDepth = CreateAnchoredLayer(
                "CountDepth",
                parent,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-16f, -28f),
                new Vector2(112f, 34f),
                espressoDark);
            countDepth.GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, -5f);

            GameObject countPlate = CreateAnchoredLayer(
                "CountPlate",
                parent,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-16f, -25f),
                new Vector2(112f, 34f),
                goldBright);
            AddOutline(countPlate, espressoDark, 1.5f);

            countText = CreateText(
                "ProgressCount",
                countPlate.transform,
                "0 / 0",
                17f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Vector2.zero,
                new Vector2(-8f, 0f),
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                espressoDark);
            countText.characterSpacing = 0.8f;

            GameObject separator = CreateUiObject("HeaderSeparator", parent);
            SetRect(
                separator.GetComponent<RectTransform>(),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -61f),
                new Vector2(-32f, 3f));
            CreateImage(separator, goldBright);
        }

        private void BuildTimer(Transform parent)
        {
            GameObject timerDepth = CreateAnchoredLayer(
                "TimerDepth",
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(20f, -75f),
                new Vector2(144f, 62f),
                espressoDark);
            timerDepth.GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, -5f);

            GameObject timerPlate = CreateAnchoredLayer(
                "TimerPlate",
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(20f, -72f),
                new Vector2(144f, 62f),
                teal);
            AddOutline(timerPlate, cream, 1.5f);

            TMP_Text timerLabel = CreateText(
                "TimerLabel",
                timerPlate.transform,
                "TIME",
                10f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Vector2(0f, -5f),
                new Vector2(-16f, 16f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                goldBright);
            timerLabel.characterSpacing = 3f;

            timerText = CreateText(
                "TimerValue",
                timerPlate.transform,
                "00:00",
                29f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Vector2(0f, -19f),
                new Vector2(-12f, 38f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                paper);
            timerText.characterSpacing = 1.5f;
            timerText.outlineWidth = 0.1f;
            timerText.outlineColor = espressoDark;
        }

        private void BuildProgressBar(Transform parent)
        {
            TMP_Text restoredLabel = CreateText(
                "RestoredLabel",
                parent,
                "ITEMS RESTORED",
                11f,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(180f, -72f),
                new Vector2(226f, 20f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                goldBright);
            restoredLabel.characterSpacing = 1.5f;

            GameObject trackDepth = CreateAnchoredLayer(
                "ProgressDepth",
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(180f, -103f),
                new Vector2(226f, 31f),
                espressoDark);
            trackDepth.GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, -5f);

            GameObject trackFrame = CreateAnchoredLayer(
                "ProgressFrame",
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(180f, -100f),
                new Vector2(226f, 31f),
                cream);
            AddOutline(trackFrame, espressoDark, 1.5f);

            GameObject track = CreateUiObject("ProgressTrack", trackFrame.transform);
            RectTransform trackRect = track.GetComponent<RectTransform>();
            SetFullScreen(trackRect);
            trackRect.offsetMin = new Vector2(5f, 5f);
            trackRect.offsetMax = new Vector2(-5f, -5f);
            CreateImage(track, ink);

            GameObject fill = CreateUiObject("ProgressFill", track.transform);
            SetFullScreen(fill.GetComponent<RectTransform>());
            fillImage = fill.AddComponent<Image>();
            fillImage.sprite = GetWhiteSprite();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0f;
            fillImage.color = tealBright;
            fillImage.raycastTarget = false;

            GameObject highlight = CreateUiObject("FillHighlight", track.transform);
            SetRect(
                highlight.GetComponent<RectTransform>(),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                new Vector2(0f, 4f));
            fillHighlightImage = highlight.AddComponent<Image>();
            fillHighlightImage.sprite = GetWhiteSprite();
            fillHighlightImage.type = Image.Type.Filled;
            fillHighlightImage.fillMethod = Image.FillMethod.Horizontal;
            fillHighlightImage.fillOrigin = 0;
            fillHighlightImage.fillAmount = 0f;
            fillHighlightImage.color = new Color(0.72f, 1f, 0.96f, 0.45f);
            fillHighlightImage.raycastTarget = false;
        }

        private void RefreshProgress()
        {
            if (fillImage == null || fillHighlightImage == null || countText == null)
            {
                return;
            }

            int totalCount = PlaceableObject.TotalObjectCount;
            int completedCount = PlaceableObject.CompletedObjectCount;
            float progress = totalCount > 0 ? (float)completedCount / totalCount : 0f;

            fillImage.fillAmount = progress;
            fillHighlightImage.fillAmount = progress;
            fillImage.color = totalCount > 0 && completedCount >= totalCount ? success : tealBright;
            countText.text = string.Format("{0} / {1}", completedCount, totalCount);
        }

        private void RefreshTimer(bool force)
        {
            if (timerText == null)
            {
                return;
            }

            GameSessionManager session = GameSessionManager.Instance;
            float elapsedSeconds = session == null ? 0f : session.ElapsedSeconds;
            int elapsedSecond = Mathf.FloorToInt(elapsedSeconds);
            if (!force && elapsedSecond == displayedElapsedSecond)
            {
                return;
            }

            displayedElapsedSecond = elapsedSecond;
            timerText.text = GameSessionManager.FormatTime(elapsedSeconds);
        }

        private void UpdateHudScale()
        {
            cachedScreenWidth = Screen.width;
            cachedScreenHeight = Screen.height;

            if (hudRoot == null)
            {
                return;
            }

            float widthScale = Mathf.Max(1f, Screen.width) / 1280f;
            float heightScale = Mathf.Max(1f, Screen.height) / 720f;
            float effectiveMinimumScale = PlatformSupport.IsTouchDevice ? 0.6f : minimumScale;
            float effectiveMaximumScale = PlatformSupport.IsTouchDevice ? 0.86f : maximumScale;
            float scale = Mathf.Clamp(Mathf.Min(widthScale, heightScale), effectiveMinimumScale, effectiveMaximumScale);
            hudRoot.localScale = Vector3.one * scale;

            Rect safeArea = PlatformSupport.SafeArea;
            float safeLeft = safeArea.xMin;
            float safeTop = Mathf.Max(0f, Screen.height - safeArea.yMax);
            float margin = PlatformSupport.IsTouchDevice ? 16f : hudOffset.x;
            hudRoot.anchoredPosition = new Vector2(safeLeft + margin, -(safeTop + margin));
        }

        private GameObject CreateCenteredLayer(string objectName, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            return CreateAnchoredLayer(
                objectName,
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                position,
                size,
                color);
        }

        private GameObject CreateAnchoredLayer(
            string objectName,
            Transform parent,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            GameObject layer = CreateUiObject(objectName, parent);
            SetRect(layer.GetComponent<RectTransform>(), anchor, anchor, pivot, position, size);
            CreateImage(layer, color);
            return layer;
        }

        private void CreateBolt(Transform parent, Vector2 position)
        {
            GameObject bolt = CreateUiObject("Bolt", parent);
            SetRect(
                bolt.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                position,
                new Vector2(10f, 10f));

            Image image = bolt.AddComponent<Image>();
            image.sprite = GetCircleSprite();
            image.color = goldBright;
            image.raycastTarget = false;
        }

        private static TMP_Text CreateText(
            string objectName,
            Transform parent,
            string content,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Color color)
        {
            GameObject textObject = CreateUiObject(objectName, parent);
            SetRect(textObject.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, position, size);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Truncate;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static void AddTextShadow(TMP_Text text, Color color, Vector2 distance)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static GameObject CreateUiObject(string objectName, Transform parent)
        {
            GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private static Image CreateImage(GameObject target, Color color)
        {
            Image image = target.AddComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void SetFullScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void AddOutline(GameObject target, Color color, float distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        private Sprite GetCircleSprite()
        {
            if (circleSprite != null)
            {
                return circleSprite;
            }

            const int size = 48;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "HudCircle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            Vector2 center = new Vector2((size - 1f) * 0.5f, (size - 1f) * 0.5f);
            float radius = size * 0.44f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 1f - Mathf.Clamp01((distance - radius + 1f) / 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            circleSprite.name = "HudCircleSprite";
            return circleSprite;
        }

        private static Sprite GetWhiteSprite()
        {
            if (whiteSprite == null)
            {
                whiteSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
                whiteSprite.name = "RuntimeHudWhiteSprite";
            }

            return whiteSprite;
        }
    }
}
