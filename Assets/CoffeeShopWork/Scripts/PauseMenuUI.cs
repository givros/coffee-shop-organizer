using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoffeeShop
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class PauseMenuUI : MonoBehaviour
    {
        private enum PauseButtonStyle
        {
            Gold,
            Red
        }

        private readonly Color cream = new Color(0.97f, 0.91f, 0.75f, 1f);
        private readonly Color paper = new Color(0.96f, 0.96f, 0.91f, 1f);
        private readonly Color gold = new Color(0.62f, 0.27f, 0.035f, 1f);
        private readonly Color goldBright = new Color(0.9f, 0.43f, 0.065f, 1f);
        private readonly Color espresso = new Color(0.03f, 0.012f, 0.008f, 1f);
        private readonly Color espressoDark = new Color(0.012f, 0.004f, 0.003f, 1f);
        private readonly Color cherry = new Color(0.20f, 0.018f, 0.014f, 1f);
        private readonly Color cherryBright = new Color(0.38f, 0.045f, 0.028f, 1f);

        private GameObject pauseRoot;
        private Button restartButton;
        private FirstPersonPlayerController playerController;
        private PlayerObjectInteraction playerInteraction;
        private uint lastMobilePauseVersion;
        private static Sprite whiteSprite;

        public bool IsOpen => pauseRoot != null && pauseRoot.activeSelf;

        private void Awake()
        {
            ResolvePlayerReferences();
            EnsureEventSystem();
            BuildUi();
            pauseRoot.SetActive(false);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            bool keyboardPressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            bool mobilePressed = MobileControlsUI.ReadPausePress(ref lastMobilePauseVersion);
            if (keyboardPressed || mobilePressed)
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            if (IsOpen)
            {
                ClosePauseMenu();
            }
            else
            {
                OpenPauseMenu();
            }
        }

        public void OpenPauseMenu()
        {
            GameSessionManager session = GameSessionManager.Instance;
            if (IsOpen || session == null || !session.IsGameRunning || session.HasFinished)
            {
                return;
            }

            session.PauseGame();
            SetGameplayControls(false);
            pauseRoot.SetActive(true);

            if (EventSystem.current != null && restartButton != null)
            {
                EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
            }
        }

        public void ClosePauseMenu()
        {
            if (!IsOpen)
            {
                return;
            }

            pauseRoot.SetActive(false);

            GameSessionManager session = GameSessionManager.Instance;
            if (session != null)
            {
                session.ResumeGame();
            }

            SetGameplayControls(true);
        }

        public void RestartGame()
        {
            PrepareForSceneChange();

            GameSessionManager session = GameSessionManager.Instance;
            if (session != null)
            {
                session.RestartCurrentScene();
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(GameSessionManager.GameplaySceneName);
        }

        public void QuitToMenu()
        {
            PrepareForSceneChange();

            GameSessionManager session = GameSessionManager.Instance;
            if (session != null)
            {
                session.ReturnToMenu();
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(GameSessionManager.MenuSceneName);
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject(
                "PauseCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            pauseRoot = CreateUiObject("PauseOverlay", canvasObject.transform);
            SetFullScreen(pauseRoot.GetComponent<RectTransform>());
            Image overlay = pauseRoot.AddComponent<Image>();
            overlay.sprite = GetWhiteSprite();
            overlay.color = new Color(espressoDark.r, espressoDark.g, espressoDark.b, 0.78f);

            Vector2 boardSize = new Vector2(620f, 500f);
            CreateCenteredLayer(
                "PauseBoardShadow",
                pauseRoot.transform,
                new Vector2(14f, -18f),
                boardSize,
                new Color(0f, 0f, 0f, 0.72f),
                false);

            GameObject frame = CreateCenteredLayer(
                "PauseBoardFrame",
                pauseRoot.transform,
                Vector2.zero,
                boardSize,
                cream,
                false);
            AddOutline(frame, espressoDark, 5f);

            GameObject face = CreateCenteredLayer(
                "PauseBoardFace",
                frame.transform,
                Vector2.zero,
                boardSize - new Vector2(28f, 28f),
                new Color(espresso.r, espresso.g, espresso.b, 0.99f),
                false);
            AddOutline(face, cherryBright, 3f);

            BuildAwning(face.transform);
            BuildTitle(face.transform);

            restartButton = CreateButton(
                "RestartButton",
                face.transform,
                "RESTART",
                new Vector2(0f, 10f),
                PauseButtonStyle.Gold,
                RestartGame);

            CreateButton(
                "QuitButton",
                face.transform,
                "QUIT TO MENU",
                new Vector2(0f, -92f),
                PauseButtonStyle.Red,
                QuitToMenu);

            TMP_Text hint = CreateText(
                "ResumeHint",
                face.transform,
                "ESC   RESUME",
                13f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Vector2(0f, -202f),
                new Vector2(340f, 28f),
                paper);
            hint.characterSpacing = 3.2f;

            CreateBolt(frame.transform, new Vector2(-284f, 224f));
            CreateBolt(frame.transform, new Vector2(284f, 224f));
            CreateBolt(frame.transform, new Vector2(-284f, -224f));
            CreateBolt(frame.transform, new Vector2(284f, -224f));
        }

        private void BuildAwning(Transform parent)
        {
            GameObject awning = CreateUiObject("PauseAwning", parent);
            RectTransform awningRect = awning.GetComponent<RectTransform>();
            awningRect.anchorMin = new Vector2(0f, 1f);
            awningRect.anchorMax = new Vector2(1f, 1f);
            awningRect.pivot = new Vector2(0.5f, 1f);
            awningRect.anchoredPosition = Vector2.zero;
            awningRect.sizeDelta = new Vector2(0f, 24f);

            const int stripeCount = 12;
            for (int index = 0; index < stripeCount; index++)
            {
                GameObject stripe = CreateUiObject("AwningStripe" + index, awning.transform);
                RectTransform stripeRect = stripe.GetComponent<RectTransform>();
                stripeRect.anchorMin = new Vector2(index / (float)stripeCount, 0f);
                stripeRect.anchorMax = new Vector2((index + 1f) / stripeCount, 1f);
                stripeRect.offsetMin = Vector2.zero;
                stripeRect.offsetMax = Vector2.zero;

                Image stripeImage = stripe.AddComponent<Image>();
                stripeImage.sprite = GetWhiteSprite();
                stripeImage.color = index % 2 == 0 ? cream : cherry;
                stripeImage.raycastTarget = false;
            }

            GameObject edge = CreateUiObject("AwningGoldEdge", parent);
            RectTransform edgeRect = edge.GetComponent<RectTransform>();
            edgeRect.anchorMin = new Vector2(0f, 1f);
            edgeRect.anchorMax = new Vector2(1f, 1f);
            edgeRect.pivot = new Vector2(0.5f, 1f);
            edgeRect.anchoredPosition = new Vector2(0f, -24f);
            edgeRect.sizeDelta = new Vector2(0f, 4f);
            Image edgeImage = edge.AddComponent<Image>();
            edgeImage.sprite = GetWhiteSprite();
            edgeImage.color = goldBright;
            edgeImage.raycastTarget = false;
        }

        private void BuildTitle(Transform parent)
        {
            GameObject ribbonDepth = CreateCenteredLayer(
                "PauseRibbonDepth",
                parent,
                new Vector2(0f, 143f),
                new Vector2(420f, 94f),
                espressoDark,
                false);

            GameObject ribbon = CreateCenteredLayer(
                "PauseRibbon",
                parent,
                new Vector2(0f, 153f),
                new Vector2(420f, 94f),
                cherry,
                false);
            AddOutline(ribbon, cream, 3f);

            TMP_Text title = CreateText(
                "PauseTitle",
                ribbon.transform,
                "PAUSED",
                42f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Vector2.zero,
                new Vector2(390f, 70f),
                paper);
            title.characterSpacing = 4.5f;
            title.outlineWidth = 0.12f;
            title.outlineColor = espressoDark;
            AddTextShadow(title, new Color(0f, 0f, 0f, 0.78f), new Vector2(4f, -4f));

            TMP_Text subtitle = CreateText(
                "PauseSubtitle",
                parent,
                "THE SHIFT IS ON HOLD",
                13f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Vector2(0f, 88f),
                new Vector2(430f, 28f),
                goldBright);
            subtitle.characterSpacing = 3.4f;

            ribbonDepth.transform.SetAsFirstSibling();
        }

        private Button CreateButton(
            string objectName,
            Transform parent,
            string label,
            Vector2 position,
            PauseButtonStyle style,
            UnityEngine.Events.UnityAction action)
        {
            Vector2 size = new Vector2(420f, 74f);
            GameObject buttonObject = CreateUiObject(objectName, parent);
            SetCenteredRect(buttonObject.GetComponent<RectTransform>(), position, size);

            Color normal = style == PauseButtonStyle.Gold ? gold : cherry;
            Color highlighted = style == PauseButtonStyle.Gold ? goldBright : cherryBright;
            Color pressed = style == PauseButtonStyle.Gold
                ? new Color(0.76f, 0.40f, 0.09f, 1f)
                : new Color(0.38f, 0.08f, 0.07f, 1f);
            Color depthColor = style == PauseButtonStyle.Gold
                ? new Color(0.53f, 0.27f, 0.055f, 1f)
                : new Color(0.27f, 0.055f, 0.05f, 1f);
            Color textColor = style == PauseButtonStyle.Gold ? espressoDark : paper;

            GameObject depth = CreateUiObject("ButtonDepth", buttonObject.transform);
            SetFullScreen(depth.GetComponent<RectTransform>());
            depth.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -9f);
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
            AddOutline(face, style == PauseButtonStyle.Gold ? espressoDark : cream, 2f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = faceImage;
            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.selectedColor = highlighted;
            colors.pressedColor = pressed;
            colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.45f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(action);

            GameObject shine = CreateUiObject("ButtonShine", face.transform);
            RectTransform shineRect = shine.GetComponent<RectTransform>();
            shineRect.anchorMin = new Vector2(0f, 1f);
            shineRect.anchorMax = new Vector2(1f, 1f);
            shineRect.pivot = new Vector2(0.5f, 1f);
            shineRect.anchoredPosition = new Vector2(0f, -7f);
            shineRect.sizeDelta = new Vector2(-16f, 5f);
            Image shineImage = shine.AddComponent<Image>();
            shineImage.sprite = GetWhiteSprite();
            shineImage.color = new Color(1f, 1f, 1f, 0.22f);
            shineImage.raycastTarget = false;

            TMP_Text buttonText = CreateText(
                "ButtonText",
                face.transform,
                label,
                22f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Vector2(-10f, 0f),
                new Vector2(330f, 50f),
                textColor);
            buttonText.characterSpacing = 2.4f;
            buttonText.outlineWidth = style == PauseButtonStyle.Gold ? 0f : 0.08f;
            buttonText.outlineColor = espressoDark;

            CreateText(
                "ButtonArrow",
                face.transform,
                ">",
                26f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Vector2(174f, 0f),
                new Vector2(44f, 50f),
                textColor);

            MenuButtonMotion motion = buttonObject.AddComponent<MenuButtonMotion>();
            motion.Configure(1.035f);
            return button;
        }

        private void ResolvePlayerReferences()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                return;
            }

            playerController = player.GetComponent<FirstPersonPlayerController>();
            playerInteraction = player.GetComponent<PlayerObjectInteraction>();
        }

        private void SetGameplayControls(bool enabled)
        {
            if (playerController == null || playerInteraction == null)
            {
                ResolvePlayerReferences();
            }

            if (playerController != null)
            {
                playerController.SetGameplayInputEnabled(enabled);
            }

            if (playerInteraction != null)
            {
                playerInteraction.SetInteractionEnabled(enabled);
            }

            MobileControlsUI.SetGameplayControlsVisible(enabled);
        }

        private void PrepareForSceneChange()
        {
            if (pauseRoot != null)
            {
                pauseRoot.SetActive(false);
            }

            if (playerInteraction != null)
            {
                playerInteraction.SetInteractionEnabled(false);
            }

            MobileControlsUI.HideAllControls();
        }

        private void OnDestroy()
        {
            GameSessionManager session = GameSessionManager.Instance;
            if (session != null && session.IsPaused)
            {
                session.ResumeGame();
            }
            else
            {
                Time.timeScale = 1f;
            }
        }

        private GameObject CreateCenteredLayer(
            string objectName,
            Transform parent,
            Vector2 position,
            Vector2 size,
            Color color,
            bool raycastTarget)
        {
            GameObject layer = CreateUiObject(objectName, parent);
            SetCenteredRect(layer.GetComponent<RectTransform>(), position, size);
            Image image = layer.AddComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return layer;
        }

        private void CreateBolt(Transform parent, Vector2 position)
        {
            GameObject bolt = CreateCenteredLayer(
                "Bolt",
                parent,
                position,
                new Vector2(12f, 12f),
                goldBright,
                false);
            bolt.transform.localEulerAngles = new Vector3(0f, 0f, 45f);
        }

        private static TMP_Text CreateText(
            string objectName,
            Transform parent,
            string content,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            GameObject textObject = CreateUiObject(objectName, parent);
            SetCenteredRect(textObject.GetComponent<RectTransform>(), position, size);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
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

        private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetFullScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void AddOutline(GameObject target, Color color, float distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
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
                whiteSprite.name = "RuntimePauseWhiteSprite";
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
    }
}
