using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoffeeShop
{
    [DisallowMultipleComponent]
    public sealed class GameMenuUI : MonoBehaviour
    {
        private enum MenuView
        {
            Main,
            Result
        }

        private enum GameButtonStyle
        {
            Gold,
            Teal,
            Red
        }

        [Header("Scene Flow")]
        [SerializeField] private string gameplaySceneName = GameSessionManager.GameplaySceneName;

        [Header("Shop")]
        [SerializeField, Min(1)] private int objectCount = 170;

        [Header("Background")]
        [SerializeField] private string backgroundResourceName = "CoffeeShopMenuBackground";
        [SerializeField] private Color backgroundTint = new Color(0.78f, 0.82f, 0.84f, 1f);

        private readonly Color ink = new Color(0.006f, 0.012f, 0.018f, 1f);
        private readonly Color cream = new Color(0.97f, 0.91f, 0.75f, 1f);
        private readonly Color paper = new Color(0.96f, 0.96f, 0.91f, 1f);
        private readonly Color muted = new Color(0.38f, 0.46f, 0.44f, 1f);
        private readonly Color gold = new Color(0.62f, 0.27f, 0.035f, 1f);
        private readonly Color goldBright = new Color(0.9f, 0.43f, 0.065f, 1f);
        private readonly Color espresso = new Color(0.03f, 0.012f, 0.008f, 1f);
        private readonly Color espressoDark = new Color(0.012f, 0.004f, 0.003f, 1f);
        private readonly Color cherry = new Color(0.20f, 0.018f, 0.014f, 1f);
        private readonly Color cherryBright = new Color(0.38f, 0.045f, 0.028f, 1f);
        private readonly Color teal = new Color(0.018f, 0.20f, 0.22f, 1f);
        private readonly Color tealBright = new Color(0.035f, 0.36f, 0.38f, 1f);
        private readonly Color success = new Color(0.07f, 0.48f, 0.17f, 1f);

        private CanvasGroup canvasGroup;
        private RectTransform backgroundRect;
        private GameObject mainView;
        private GameObject resultView;

        private TMP_Text resultTimeValue;
        private TMP_Text resultProgressValue;

        private Sprite backgroundSprite;
        private Sprite vignetteSprite;
        private Sprite circleSprite;
        private static Sprite whiteSprite;

        private void Awake()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            UnlockCursor();
            EnsureEventSystem();
            BuildUi();
        }

        private void Start()
        {
            bool showResult = PlayerPrefs.GetInt(GameSessionManager.ShowResultKey, 0) == 1;
            if (showResult)
            {
                PlayerPrefs.DeleteKey(GameSessionManager.ShowResultKey);
                PlayerPrefs.Save();
                ShowView(MenuView.Result);
            }
            else
            {
                ShowView(MenuView.Main);
            }

        }

        private void Update()
        {
            if (PlatformSupport.IsTouchDevice || backgroundRect == null || Mouse.current == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Vector2 pointer = Mouse.current.position.ReadValue();
            Vector2 normalized = new Vector2(pointer.x / Screen.width, pointer.y / Screen.height) - new Vector2(0.5f, 0.5f);
            Vector2 target = new Vector2(-normalized.x * 16f, -normalized.y * 9f);
            backgroundRect.anchoredPosition = Vector2.Lerp(backgroundRect.anchoredPosition, target, Time.unscaledDeltaTime * 2.4f);
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject(
                "MenuCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 250;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            BuildBackground(canvasObject.transform);

            mainView = CreateFullScreenObject("MainView", canvasObject.transform);
            resultView = CreateFullScreenObject("ResultView", canvasObject.transform);

            BuildMainView(mainView.transform);
            BuildResultView(resultView.transform);
            BuildScreenChrome(canvasObject.transform);
        }

        private void BuildBackground(Transform parent)
        {
            GameObject background = CreateUiObject("CoffeeShopBackdrop", parent);
            backgroundRect = background.GetComponent<RectTransform>();
            SetFullScreen(backgroundRect);
            backgroundRect.sizeDelta = new Vector2(72f, 48f);

            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.sprite = GetBackgroundSprite();
            backgroundImage.color = backgroundTint;
            backgroundImage.raycastTarget = false;

            GameObject tone = CreateFullScreenObject("CinematicTone", parent);
            Image toneImage = tone.AddComponent<Image>();
            toneImage.sprite = GetWhiteSprite();
            toneImage.color = new Color(0.018f, 0.028f, 0.034f, 0.32f);
            toneImage.raycastTarget = false;

            GameObject vignette = CreateFullScreenObject("EdgeVignette", parent);
            Image vignetteImage = vignette.AddComponent<Image>();
            vignetteImage.sprite = GetVignetteSprite();
            vignetteImage.color = Color.white;
            vignetteImage.raycastTarget = false;
        }

        private void BuildScreenChrome(Transform parent)
        {
            GameObject topStrip = CreateUiObject("CoffeeAwningStrip", parent);
            SetRect(topStrip.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 18f));

            for (int i = 0; i < 20; i++)
            {
                GameObject segment = CreateUiObject("AwningSegment" + i, topStrip.transform);
                RectTransform rect = segment.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(i / 20f, 0f);
                rect.anchorMax = new Vector2((i + 1) / 20f, 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                Image image = segment.AddComponent<Image>();
                image.sprite = GetWhiteSprite();
                image.color = i % 2 == 0 ? cherry : cream;
                image.raycastTarget = false;
            }

            GameObject bottomBar = CreateUiObject("BottomGameBar", parent);
            SetRect(bottomBar.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 58f));
            Image bottomImage = bottomBar.AddComponent<Image>();
            bottomImage.sprite = GetWhiteSprite();
            bottomImage.color = new Color(ink.r, ink.g, ink.b, 0.82f);
            bottomImage.raycastTarget = false;

            TMP_Text gameName = CreateText("GameName", bottomBar.transform, "COFFEE SHOP ORGANIZER", 13f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Vector2(44f, 0f), new Vector2(450f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), cream);
            gameName.characterSpacing = 2.5f;

            TMP_Text controls = CreateText("Controls", bottomBar.transform, "WASD  MOVE     •     MOUSE  LOOK     •     LEFT CLICK  INTERACT", 12f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Vector2(-44f, 0f), new Vector2(800f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), muted);
            controls.characterSpacing = 1.2f;
        }

        private void BuildMainView(Transform parent)
        {
            BuildGameLogo(parent);

            TMP_Text tagline = CreateText(
                "Tagline",
                parent,
                "EVERYTHING HAS A PLACE.  PUT IT BACK.",
                17f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Vector2(0f, -402f),
                new Vector2(800f, 36f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                paper);
            tagline.characterSpacing = 3.5f;
            AddTextShadow(tagline, new Color(0f, 0f, 0f, 0.85f), new Vector2(3f, -3f));

            GameObject buttons = CreateUiObject("MainButtons", parent);
            SetRect(buttons.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -465f), new Vector2(540f, 205f));
            CreateGameButton("PlayButton", buttons.transform, "PLAY", new Vector2(0f, 0f), new Vector2(540f, 88f), GameButtonStyle.Gold, StartGame, 27f);

            CreateMedallion(parent, "ItemsMedallion", "ITEMS", objectCount.ToString(), new Vector2(-405f, -536f), 158f, teal);
        }

        private void BuildGameLogo(Transform parent)
        {
            GameObject logo = CreateUiObject("GameLogo", parent);
            SetRect(logo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(940f, 320f));

            CreateRectLayer("SignShadow", logo.transform, new Vector2(0f, -140f), new Vector2(830f, 224f), new Color(0f, 0f, 0f, 0.64f));
            GameObject frame = CreateRectLayer("SignFrame", logo.transform, new Vector2(0f, -128f), new Vector2(842f, 224f), cream);
            AddOutline(frame, espressoDark, 4f);
            GameObject face = CreateRectLayer("SignFace", logo.transform, new Vector2(0f, -128f), new Vector2(810f, 192f), cherry);
            AddOutline(face, new Color(0.28f, 0.06f, 0.05f, 1f), 3f);

            for (int i = 0; i < 10; i++)
            {
                GameObject stripe = CreateUiObject("SignStripe" + i, face.transform);
                RectTransform stripeRect = stripe.GetComponent<RectTransform>();
                stripeRect.anchorMin = new Vector2(i / 10f, 1f);
                stripeRect.anchorMax = new Vector2((i + 1) / 10f, 1f);
                stripeRect.pivot = new Vector2(0.5f, 1f);
                stripeRect.anchoredPosition = Vector2.zero;
                stripeRect.sizeDelta = new Vector2(0f, 16f);
                Image stripeImage = stripe.AddComponent<Image>();
                stripeImage.sprite = GetWhiteSprite();
                stripeImage.color = i % 2 == 0 ? cream : new Color(0.38f, 0.075f, 0.06f, 1f);
                stripeImage.raycastTarget = false;
            }

            TMP_Text eyebrow = CreateText("LogoEyebrow", logo.transform, "A COZY RESTORATION GAME", 15f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, -42f), new Vector2(650f, 28f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), goldBright);
            eyebrow.characterSpacing = 4f;

            TMP_Text title = CreateText("LogoTitle", logo.transform, "COFFEE SHOP", 76f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, -73f), new Vector2(790f, 104f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), paper);
            title.outlineWidth = 0.18f;
            title.outlineColor = espressoDark;
            AddTextShadow(title, new Color(0.06f, 0.015f, 0.01f, 0.9f), new Vector2(6f, -7f));

            CreateBolt(logo.transform, new Vector2(-388f, -50f));
            CreateBolt(logo.transform, new Vector2(388f, -50f));
            CreateBolt(logo.transform, new Vector2(-388f, -196f));
            CreateBolt(logo.transform, new Vector2(388f, -196f));

            CreateRectLayer("RibbonDepth", logo.transform, new Vector2(0f, -245f), new Vector2(500f, 68f), espressoDark);
            GameObject ribbon = CreateRectLayer("OrganizerRibbon", logo.transform, new Vector2(0f, -235f), new Vector2(500f, 68f), gold);
            ribbon.transform.localEulerAngles = new Vector3(0f, 0f, -1.2f);
            AddOutline(ribbon, espressoDark, 3f);
            TMP_Text ribbonText = CreateText("RibbonText", ribbon.transform, "O R G A N I Z E R", 25f, FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), espressoDark);
            ribbonText.characterSpacing = 2.8f;
        }

        private void BuildResultView(Transform parent)
        {
            Transform board = CreateGameBoard(parent, "ResultBoard", "SHIFT COMPLETE!", "THE COFFEE SHOP IS READY");
            BuildConfetti(board);

            resultTimeValue = CreateMedallion(board, "FinalTimeMedal", "TIME", "00:00", new Vector2(-170f, -300f), 210f, teal, true);
            resultProgressValue = CreateMedallion(board, "FinalProgressMedal", "RESTORED", "0/0", new Vector2(170f, -300f), 210f, success, true);

            CreateText("ResultMessage", board, "Every object is back where it belongs.", 18f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, -522f), new Vector2(600f, 36f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), paper).characterSpacing = 1.3f;

            CreateGameButton("ResultMenuButton", board, "MAIN MENU", new Vector2(54f, -604f), new Vector2(250f, 68f), GameButtonStyle.Red, () => ShowView(MenuView.Main), 17f);
            CreateGameButton("ResultPlayButton", board, "PLAY AGAIN", new Vector2(636f, -604f), new Vector2(250f, 68f), GameButtonStyle.Gold, StartGame, 19f);
        }

        private Transform CreateGameBoard(Transform parent, string objectName, string title, string subtitle)
        {
            GameObject shadow = CreateUiObject(objectName + "Shadow", parent);
            SetRect(shadow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(16f, -25f), new Vector2(960f, 720f));
            Image shadowImage = shadow.AddComponent<Image>();
            shadowImage.sprite = GetWhiteSprite();
            shadowImage.color = new Color(0f, 0f, 0f, 0.62f);
            shadowImage.raycastTarget = false;

            GameObject board = CreateUiObject(objectName, parent);
            SetRect(board.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -9f), new Vector2(960f, 720f));
            Image frameImage = board.AddComponent<Image>();
            frameImage.sprite = GetWhiteSprite();
            frameImage.color = cream;
            frameImage.raycastTarget = false;
            AddOutline(board, espressoDark, 5f);

            GameObject inner = CreateUiObject("ChalkboardFace", board.transform);
            RectTransform innerRect = inner.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(14f, 14f);
            innerRect.offsetMax = new Vector2(-14f, -14f);
            Image innerImage = inner.AddComponent<Image>();
            innerImage.sprite = GetWhiteSprite();
            innerImage.color = new Color(espresso.r, espresso.g, espresso.b, 0.98f);
            innerImage.raycastTarget = false;
            AddOutline(inner, new Color(0.28f, 0.09f, 0.06f, 1f), 3f);

            CreateBolt(board.transform, new Vector2(-448f, -28f));
            CreateBolt(board.transform, new Vector2(448f, -28f));
            CreateBolt(board.transform, new Vector2(-448f, -692f));
            CreateBolt(board.transform, new Vector2(448f, -692f));

            CreateRectLayer("TitleRibbonDepth", board.transform, new Vector2(0f, 6f), new Vector2(520f, 88f), espressoDark, new Vector2(0.5f, 1f));
            GameObject ribbon = CreateRectLayer("TitleRibbon", board.transform, new Vector2(0f, 17f), new Vector2(520f, 88f), cherry, new Vector2(0.5f, 1f));
            AddOutline(ribbon, cream, 3f);
            TMP_Text titleText = CreateText("BoardTitle", ribbon.transform, title, 32f, FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), paper);
            titleText.outlineWidth = 0.12f;
            titleText.outlineColor = espressoDark;
            AddTextShadow(titleText, new Color(0f, 0f, 0f, 0.7f), new Vector2(3f, -3f));

            TMP_Text subtitleText = CreateText("BoardSubtitle", board.transform, subtitle, 14f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, -104f), new Vector2(720f, 30f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), goldBright);
            subtitleText.characterSpacing = 3f;

            return board.transform;
        }

        private TMP_Text CreateMedallion(Transform parent, string objectName, string label, string value, Vector2 position, float size, Color accent, bool localBoardCoordinates = false)
        {
            GameObject root = CreateUiObject(objectName, parent);
            Vector2 anchor = localBoardCoordinates ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 1f);
            SetRect(root.GetComponent<RectTransform>(), anchor, anchor, new Vector2(0.5f, 0.5f), position, new Vector2(size, size));

            GameObject depth = CreateUiObject("MedalDepth", root.transform);
            SetRect(depth.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(size, size));
            Image depthImage = depth.AddComponent<Image>();
            depthImage.sprite = GetCircleSprite();
            depthImage.color = espressoDark;
            depthImage.raycastTarget = false;

            GameObject rim = CreateUiObject("MedalRim", root.transform);
            SetRect(rim.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            Image rimImage = rim.AddComponent<Image>();
            rimImage.sprite = GetCircleSprite();
            rimImage.color = accent;
            rimImage.raycastTarget = false;

            GameObject face = CreateUiObject("MedalFace", root.transform);
            SetRect(face.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size - 18f, size - 18f));
            Image faceImage = face.AddComponent<Image>();
            faceImage.sprite = GetCircleSprite();
            faceImage.color = new Color(ink.r, ink.g, ink.b, 0.97f);
            faceImage.raycastTarget = false;

            TMP_Text labelText = CreateText("MedalLabel", root.transform, label, Mathf.Clamp(size * 0.075f, 11f, 16f), FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, size * 0.16f), new Vector2(size - 30f, 28f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), accent);
            labelText.characterSpacing = 1.5f;
            float valueSize = Mathf.Clamp(size * 0.23f, 30f, 52f);
            TMP_Text valueText = CreateText("MedalValue", root.transform, value, valueSize, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, -size * 0.06f), new Vector2(size - 26f, size * 0.42f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), paper);
            valueText.enableAutoSizing = true;
            valueText.fontSizeMin = 22f;
            valueText.fontSizeMax = valueSize;
            valueText.outlineWidth = 0.1f;
            valueText.outlineColor = espressoDark;
            return valueText;
        }

        private TMP_Text CreatePlank(Transform parent, string objectName, string text, Vector2 position, Vector2 size, Color faceColor, Color textColor)
        {
            GameObject root = CreateUiObject(objectName, parent);
            SetRect(root.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, size);

            GameObject depth = CreateRectLayer("PlankDepth", root.transform, new Vector2(0f, -8f), size, espressoDark, new Vector2(0.5f, 0.5f));
            depth.transform.SetAsFirstSibling();
            GameObject face = CreateRectLayer("PlankFace", root.transform, Vector2.zero, size, faceColor, new Vector2(0.5f, 0.5f));
            AddOutline(face, espressoDark, 2f);
            CreateBolt(face.transform, new Vector2(-size.x * 0.43f, -size.y * 0.5f + 10f), 12f);
            CreateBolt(face.transform, new Vector2(size.x * 0.43f, -size.y * 0.5f + 10f), 12f);
            TMP_Text label = CreateText("PlankText", face.transform, text, 16f, FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), textColor);
            label.characterSpacing = 1.5f;
            return label;
        }

        private Button CreateGameButton(string objectName, Transform parent, string label, Vector2 position, Vector2 size, GameButtonStyle style, UnityEngine.Events.UnityAction action, float fontSize)
        {
            GameObject buttonObject = CreateUiObject(objectName, parent);
            SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);

            Color normal;
            Color highlighted;
            Color pressed;
            Color textColor;
            Color depthColor;

            switch (style)
            {
                case GameButtonStyle.Teal:
                    normal = teal;
                    highlighted = tealBright;
                    pressed = new Color(0.05f, 0.31f, 0.32f, 1f);
                    textColor = paper;
                    depthColor = new Color(0.025f, 0.19f, 0.20f, 1f);
                    break;
                case GameButtonStyle.Red:
                    normal = cherry;
                    highlighted = cherryBright;
                    pressed = new Color(0.38f, 0.08f, 0.07f, 1f);
                    textColor = paper;
                    depthColor = new Color(0.27f, 0.055f, 0.05f, 1f);
                    break;
                default:
                    normal = gold;
                    highlighted = goldBright;
                    pressed = new Color(0.76f, 0.40f, 0.09f, 1f);
                    textColor = espressoDark;
                    depthColor = new Color(0.53f, 0.27f, 0.055f, 1f);
                    break;
            }

            GameObject depth = CreateUiObject("ButtonDepth", buttonObject.transform);
            SetFullScreen(depth.GetComponent<RectTransform>());
            depth.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -10f);
            Image depthImage = depth.AddComponent<Image>();
            depthImage.sprite = GetWhiteSprite();
            depthImage.color = depthColor;
            depthImage.raycastTarget = false;
            AddOutline(depth, espressoDark, 2f);

            GameObject face = CreateUiObject("ButtonFace", buttonObject.transform);
            SetFullScreen(face.GetComponent<RectTransform>());
            Image faceImage = face.AddComponent<Image>();
            faceImage.sprite = GetWhiteSprite();
            faceImage.color = Color.white;
            AddOutline(face, style == GameButtonStyle.Gold ? espressoDark : cream, 2f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = faceImage;
            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.selectedColor = highlighted;
            colors.pressedColor = pressed;
            colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.4f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(action);

            GameObject shine = CreateUiObject("ButtonShine", face.transform);
            SetRect(shine.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(8f, -7f), new Vector2(-16f, 5f));
            Image shineImage = shine.AddComponent<Image>();
            shineImage.sprite = GetWhiteSprite();
            shineImage.color = new Color(1f, 1f, 1f, 0.22f);
            shineImage.raycastTarget = false;

            TMP_Text buttonText = CreateText("ButtonText", face.transform, label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, new Vector2(-56f, 0f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), textColor);
            buttonText.characterSpacing = 2.2f;
            buttonText.outlineWidth = style == GameButtonStyle.Gold ? 0f : 0.08f;
            buttonText.outlineColor = espressoDark;
            CreateText("ButtonArrow", face.transform, ">", fontSize + 4f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(-22f, 0f), new Vector2(44f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), textColor);

            MenuButtonMotion motion = buttonObject.AddComponent<MenuButtonMotion>();
            motion.Configure(1.035f);
            return button;
        }

        private void BuildConfetti(Transform parent)
        {
            Color[] colors = { gold, tealBright, cherryBright, success, cream };
            Vector2[] positions =
            {
                new Vector2(-392f, -164f), new Vector2(-352f, -238f), new Vector2(-420f, -322f),
                new Vector2(392f, -170f), new Vector2(354f, -246f), new Vector2(418f, -326f),
                new Vector2(-374f, -438f), new Vector2(380f, -446f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject piece = CreateUiObject("Confetti" + i, parent);
                SetRect(piece.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), positions[i], new Vector2(i % 2 == 0 ? 14f : 10f, i % 3 == 0 ? 30f : 20f));
                piece.transform.localEulerAngles = new Vector3(0f, 0f, (i * 37f) % 90f);
                Image image = piece.AddComponent<Image>();
                image.sprite = GetWhiteSprite();
                image.color = colors[i % colors.Length];
                image.raycastTarget = false;
            }
        }

        private void ShowView(MenuView view)
        {
            mainView.SetActive(view == MenuView.Main);
            resultView.SetActive(view == MenuView.Result);

            if (view == MenuView.Result)
            {
                RefreshResultView();
            }
        }

        private void RefreshResultView()
        {
            float lastTime = PlayerPrefs.GetFloat(GameSessionManager.LastTimeKey, 0f);
            int completed = PlayerPrefs.GetInt(GameSessionManager.LastCompletedKey, objectCount);
            int target = PlayerPrefs.GetInt(GameSessionManager.LastTargetKey, objectCount);

            resultTimeValue.text = GameSessionManager.FormatTime(lastTime);
            resultProgressValue.text = string.Format("{0}/{1}", completed, Mathf.Max(1, target));
        }

        private void StartGame()
        {
            PlayerPrefs.DeleteKey(GameSessionManager.ShowResultKey);
            PlayerPrefs.Save();
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameplaySceneName);
        }

        private GameObject CreateRectLayer(string objectName, Transform parent, Vector2 position, Vector2 size, Color color, Vector2? anchor = null)
        {
            Vector2 actualAnchor = anchor ?? new Vector2(0.5f, 1f);
            GameObject layer = CreateUiObject(objectName, parent);
            SetRect(layer.GetComponent<RectTransform>(), actualAnchor, actualAnchor, new Vector2(0.5f, 0.5f), position, size);
            Image image = layer.AddComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.color = color;
            image.raycastTarget = false;
            return layer;
        }

        private void CreateBolt(Transform parent, Vector2 position, float size = 16f)
        {
            GameObject bolt = CreateUiObject("Bolt", parent);
            SetRect(bolt.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
            Image image = bolt.AddComponent<Image>();
            image.sprite = GetCircleSprite();
            image.color = goldBright;
            image.raycastTarget = false;
        }

        private TMP_Text CreateText(
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
            text.textWrappingMode = TextWrappingModes.Normal;
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

        private static GameObject CreateFullScreenObject(string objectName, Transform parent)
        {
            GameObject uiObject = CreateUiObject(objectName, parent);
            SetFullScreen(uiObject.GetComponent<RectTransform>());
            return uiObject;
        }

        private static void SetFullScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void AddOutline(GameObject target, Color color, float distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        private Sprite GetBackgroundSprite()
        {
            if (backgroundSprite != null)
            {
                return backgroundSprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(backgroundResourceName);
            if (texture == null)
            {
                return GetWhiteSprite();
            }

            backgroundSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            backgroundSprite.name = "CoffeeShopMenuBackgroundSprite";
            return backgroundSprite;
        }

        private Sprite GetVignetteSprite()
        {
            if (vignetteSprite != null)
            {
                return vignetteSprite;
            }

            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "MenuVignette",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = Mathf.Abs((x / (size - 1f)) * 2f - 1f);
                    float ny = Mathf.Abs((y / (size - 1f)) * 2f - 1f);
                    float edge = Mathf.Clamp01((Mathf.Max(nx, ny) - 0.28f) / 0.72f);
                    float alpha = edge * edge * 0.72f;
                    texture.SetPixel(x, y, new Color(0.004f, 0.01f, 0.014f, alpha));
                }
            }

            texture.Apply(false, true);
            vignetteSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            vignetteSprite.name = "MenuVignetteSprite";
            return vignetteSprite;
        }

        private Sprite GetCircleSprite()
        {
            if (circleSprite != null)
            {
                return circleSprite;
            }

            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "MenuCircle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            Vector2 center = new Vector2((size - 1f) * 0.5f, (size - 1f) * 0.5f);
            float radius = size * 0.48f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 1f - Mathf.Clamp01((distance - radius + 1.5f) / 2.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            circleSprite.name = "MenuCircleSprite";
            return circleSprite;
        }

        private static Sprite GetWhiteSprite()
        {
            if (whiteSprite == null)
            {
                whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                whiteSprite.name = "RuntimeWhiteSprite";
            }

            return whiteSprite;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    internal sealed class MenuButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private float hoverScale = 1.03f;
        private Vector3 targetScale = Vector3.one;

        public void Configure(float scale)
        {
            hoverScale = scale;
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 15f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = Vector3.one * hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetScale = Vector3.one * 0.975f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetScale = PlatformSupport.IsTouchDevice
                ? Vector3.one
                : Vector3.one * hoverScale;
        }
    }
}
