using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoffeeShop
{
    [DefaultExecutionOrder(-250)]
    [DisallowMultipleComponent]
    public sealed class MobileControlsUI : MonoBehaviour
    {
        private static readonly Color Cream = new Color(0.97f, 0.91f, 0.75f, 0.88f);
        private static readonly Color Paper = new Color(1f, 1f, 1f, 1f);
        private static readonly Color Espresso = new Color(0.025f, 0.01f, 0.006f, 0.82f);
        private static readonly Color Cherry = new Color(0.32f, 0.025f, 0.018f, 0.94f);
        private static readonly Color Gold = new Color(0.9f, 0.43f, 0.065f, 0.96f);

        private static MobileControlsUI instance;
        private static Sprite whiteSprite;
        private static Sprite circleSprite;

        private CanvasScaler gameplayScaler;
        private CanvasScaler pauseScaler;
        private RectTransform gameplaySafeArea;
        private RectTransform pauseSafeArea;
        private RectTransform joystickRect;
        private RectTransform actionRect;
        private RectTransform pauseRect;
        private CanvasGroup gameplayGroup;
        private GameObject gameplayCanvasObject;
        private GameObject pauseCanvasObject;
        private TMP_Text actionLabel;
        private TMP_Text pauseLabel;
        private PlayerObjectInteraction playerInteraction;

        private Vector2 moveInput;
        private Vector2 accumulatedLookDelta;
        private uint actionVersion;
        private uint pauseVersion;
        private bool mobileModeEnabled;
        private bool gameplayControlsVisible = true;
        private bool lastPausedState;
        private int cachedWidth;
        private int cachedHeight;
        private Rect cachedSafeArea;

        public static bool IsTouchControlsActive => instance != null && instance.mobileModeEnabled;
        public static Vector2 MoveInput => instance != null ? instance.moveInput : Vector2.zero;
        public static uint ActionVersion => instance != null ? instance.actionVersion : 0u;
        public static uint PauseVersion => instance != null ? instance.pauseVersion : 0u;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            GameObject runtimeObject = new GameObject("MobileControlsRuntime");
            DontDestroyOnLoad(runtimeObject);
            instance = runtimeObject.AddComponent<MobileControlsUI>();
        }

        public static Vector2 ConsumeLookDelta()
        {
            if (instance == null)
            {
                return Vector2.zero;
            }

            Vector2 value = instance.accumulatedLookDelta;
            instance.accumulatedLookDelta = Vector2.zero;
            return value;
        }

        public static bool ReadActionPress(ref uint lastSeenVersion)
        {
            uint currentVersion = ActionVersion;
            if (currentVersion == 0u || currentVersion == lastSeenVersion)
            {
                return false;
            }

            lastSeenVersion = currentVersion;
            return true;
        }

        public static bool ReadPausePress(ref uint lastSeenVersion)
        {
            uint currentVersion = PauseVersion;
            if (currentVersion == 0u || currentVersion == lastSeenVersion)
            {
                return false;
            }

            lastSeenVersion = currentVersion;
            return true;
        }

        public static void SetGameplayControlsVisible(bool visible)
        {
            if (instance != null)
            {
                instance.SetGameplayVisible(visible);
            }
        }

        public static void HideAllControls()
        {
            if (instance != null)
            {
                instance.SetMobileMode(false);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            ConfigureForScene(SceneManager.GetActiveScene());
        }

        private void Update()
        {
            if (!mobileModeEnabled)
            {
                return;
            }

            Rect safeArea = PlatformSupport.SafeArea;
            if (Screen.width != cachedWidth || Screen.height != cachedHeight || safeArea != cachedSafeArea)
            {
                ApplyResponsiveLayout();
            }

            GameSessionManager session = GameSessionManager.Instance;
            bool isPaused = session != null && session.IsPaused;
            if (isPaused != lastPausedState)
            {
                lastPausedState = isPaused;
                SetGameplayVisible(!isPaused);
                if (pauseLabel != null)
                {
                    pauseLabel.text = isPaused ? ">" : "II";
                }
            }

            if (playerInteraction == null)
            {
                playerInteraction = FindAnyObjectByType<PlayerObjectInteraction>();
            }

            if (actionLabel != null)
            {
                actionLabel.text = playerInteraction != null && playerInteraction.IsHoldingObject
                    ? "PLACE"
                    : "USE";
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ConfigureForScene(scene);
        }

        private void ConfigureForScene(Scene scene)
        {
            PlatformSupport.RefreshDetection();
            bool shouldEnable = scene.name == GameSessionManager.GameplaySceneName && PlatformSupport.IsTouchDevice;
            if (shouldEnable)
            {
                EnsureUiBuilt();
            }

            playerInteraction = null;
            lastPausedState = false;
            SetMobileMode(shouldEnable);
            ApplyResponsiveLayout();
        }

        private void EnsureUiBuilt()
        {
            if (gameplayCanvasObject != null)
            {
                return;
            }

            EnsureEventSystem();
            BuildGameplayCanvas();
            BuildPauseCanvas();
        }

        private void BuildGameplayCanvas()
        {
            gameplayCanvasObject = CreateCanvas("MobileGameplayCanvas", 400, out gameplayScaler);
            gameplayCanvasObject.transform.SetParent(transform, false);
            gameplayGroup = gameplayCanvasObject.AddComponent<CanvasGroup>();

            gameplaySafeArea = CreateSafeArea("GameplaySafeArea", gameplayCanvasObject.transform);

            GameObject lookArea = CreateUiObject("LookSurface", gameplaySafeArea);
            RectTransform lookRect = lookArea.GetComponent<RectTransform>();
            lookRect.anchorMin = new Vector2(0.42f, 0f);
            lookRect.anchorMax = Vector2.one;
            lookRect.offsetMin = Vector2.zero;
            lookRect.offsetMax = Vector2.zero;
            Image lookImage = lookArea.AddComponent<Image>();
            lookImage.sprite = GetWhiteSprite();
            lookImage.color = new Color(1f, 1f, 1f, 0.001f);
            lookImage.raycastTarget = true;
            lookArea.AddComponent<MobileLookSurface>().Configure(this);

            GameObject joystick = CreateUiObject("MoveJoystick", gameplaySafeArea);
            joystickRect = joystick.GetComponent<RectTransform>();
            SetAnchoredRect(joystickRect, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(175f, 155f), new Vector2(244f, 244f));
            Image joystickImage = joystick.AddComponent<Image>();
            joystickImage.sprite = GetCircleSprite();
            joystickImage.color = Espresso;
            joystickImage.raycastTarget = true;
            AddOutline(joystick, Cream, 4f);

            GameObject joystickInner = CreateLayer(
                "JoystickInner",
                joystick.transform,
                Vector2.zero,
                new Vector2(205f, 205f),
                new Color(Cream.r, Cream.g, Cream.b, 0.16f),
                GetCircleSprite(),
                false);

            GameObject knob = CreateLayer(
                "JoystickKnob",
                joystick.transform,
                Vector2.zero,
                new Vector2(92f, 92f),
                Gold,
                GetCircleSprite(),
                false);
            AddOutline(knob, Espresso, 3f);
            joystick.AddComponent<MobileVirtualJoystick>().Configure(this, joystickRect, knob.GetComponent<RectTransform>());

            TMP_Text moveLabel = CreateText(
                "MoveLabel",
                joystick.transform,
                "MOVE",
                18f,
                new Vector2(0f, -125f),
                new Vector2(180f, 36f),
                Paper);
            moveLabel.characterSpacing = 3f;

            GameObject action = CreateUiObject("ActionButton", gameplaySafeArea);
            actionRect = action.GetComponent<RectTransform>();
            SetAnchoredRect(actionRect, Vector2.right, new Vector2(0.5f, 0.5f), new Vector2(-150f, 160f), new Vector2(172f, 172f));
            Image actionImage = action.AddComponent<Image>();
            actionImage.sprite = GetCircleSprite();
            actionImage.color = Cherry;
            actionImage.raycastTarget = true;
            AddOutline(action, Cream, 5f);

            CreateLayer(
                "ActionInner",
                action.transform,
                Vector2.zero,
                new Vector2(144f, 144f),
                new Color(Gold.r, Gold.g, Gold.b, 0.2f),
                GetCircleSprite(),
                false);

            actionLabel = CreateText(
                "ActionLabel",
                action.transform,
                "USE",
                25f,
                Vector2.zero,
                new Vector2(134f, 52f),
                Paper);
            actionLabel.fontStyle = FontStyles.Bold;
            actionLabel.characterSpacing = 2f;
            action.AddComponent<MobileTouchButton>().Configure(this, MobileTouchButton.ButtonKind.Action);

            TMP_Text lookHint = CreateText(
                "LookHint",
                gameplaySafeArea,
                "SWIPE TO LOOK",
                15f,
                new Vector2(-255f, 56f),
                new Vector2(300f, 36f),
                new Color(Paper.r, Paper.g, Paper.b, 0.58f));
            RectTransform lookHintRect = lookHint.rectTransform;
            lookHintRect.anchorMin = Vector2.right;
            lookHintRect.anchorMax = Vector2.right;
            lookHintRect.pivot = new Vector2(0.5f, 0.5f);
            lookHint.characterSpacing = 2.5f;

            joystickInner.transform.SetAsFirstSibling();
        }

        private void BuildPauseCanvas()
        {
            pauseCanvasObject = CreateCanvas("MobilePauseCanvas", 650, out pauseScaler);
            pauseCanvasObject.transform.SetParent(transform, false);
            pauseSafeArea = CreateSafeArea("PauseSafeArea", pauseCanvasObject.transform);

            GameObject pause = CreateUiObject("PauseTouchButton", pauseSafeArea);
            pauseRect = pause.GetComponent<RectTransform>();
            SetAnchoredRect(pauseRect, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-66f, -66f), new Vector2(92f, 92f));
            Image pauseImage = pause.AddComponent<Image>();
            pauseImage.sprite = GetCircleSprite();
            pauseImage.color = Espresso;
            pauseImage.raycastTarget = true;
            AddOutline(pause, Cream, 4f);

            pauseLabel = CreateText(
                "PauseLabel",
                pause.transform,
                "II",
                25f,
                Vector2.zero,
                new Vector2(70f, 50f),
                Paper);
            pauseLabel.fontStyle = FontStyles.Bold;
            pauseLabel.characterSpacing = 3f;
            pause.AddComponent<MobileTouchButton>().Configure(this, MobileTouchButton.ButtonKind.Pause);
        }

        private void ApplyResponsiveLayout()
        {
            cachedWidth = Screen.width;
            cachedHeight = Screen.height;
            cachedSafeArea = PlatformSupport.SafeArea;

            if (gameplayCanvasObject == null)
            {
                return;
            }

            bool portrait = PlatformSupport.IsPortrait;
            Vector2 referenceResolution = portrait
                ? new Vector2(1080f, 1920f)
                : new Vector2(1920f, 1080f);

            gameplayScaler.referenceResolution = referenceResolution;
            gameplayScaler.matchWidthOrHeight = 0.5f;
            pauseScaler.referenceResolution = referenceResolution;
            pauseScaler.matchWidthOrHeight = 0.5f;

            ApplySafeArea(gameplaySafeArea);
            ApplySafeArea(pauseSafeArea);

            if (portrait)
            {
                joystickRect.anchoredPosition = new Vector2(170f, 250f);
                actionRect.anchoredPosition = new Vector2(-155f, 260f);
                pauseRect.anchoredPosition = new Vector2(-70f, -78f);
            }
            else
            {
                joystickRect.anchoredPosition = new Vector2(175f, 155f);
                actionRect.anchoredPosition = new Vector2(-150f, 160f);
                pauseRect.anchoredPosition = new Vector2(-66f, -66f);
            }
        }

        private void SetMobileMode(bool enabled)
        {
            mobileModeEnabled = enabled;
            moveInput = Vector2.zero;
            accumulatedLookDelta = Vector2.zero;

            if (gameplayCanvasObject != null)
            {
                gameplayCanvasObject.SetActive(enabled);
            }

            if (pauseCanvasObject != null)
            {
                pauseCanvasObject.SetActive(enabled);
            }

            if (enabled)
            {
                SetGameplayVisible(true);
            }
        }

        private void SetGameplayVisible(bool visible)
        {
            gameplayControlsVisible = visible;
            moveInput = visible ? moveInput : Vector2.zero;
            accumulatedLookDelta = Vector2.zero;

            if (gameplayGroup == null)
            {
                return;
            }

            gameplayGroup.alpha = visible ? 1f : 0f;
            gameplayGroup.interactable = visible;
            gameplayGroup.blocksRaycasts = visible;
        }

        internal void SetMoveInput(Vector2 value)
        {
            moveInput = gameplayControlsVisible ? Vector2.ClampMagnitude(value, 1f) : Vector2.zero;
        }

        internal void AddLookDelta(Vector2 value)
        {
            if (gameplayControlsVisible)
            {
                accumulatedLookDelta += value;
            }
        }

        internal void RequestAction()
        {
            if (!gameplayControlsVisible)
            {
                return;
            }

            unchecked
            {
                actionVersion++;
                if (actionVersion == 0u)
                {
                    actionVersion = 1u;
                }
            }
        }

        internal void RequestPause()
        {
            unchecked
            {
                pauseVersion++;
                if (pauseVersion == 0u)
                {
                    pauseVersion = 1u;
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SetMoveInput(Vector2.zero);
                accumulatedLookDelta = Vector2.zero;
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (instance == this)
            {
                instance = null;
            }
        }

        private static GameObject CreateCanvas(string objectName, int sortingOrder, out CanvasScaler scaler)
        {
            GameObject canvasObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvasObject;
        }

        private static RectTransform CreateSafeArea(string objectName, Transform parent)
        {
            GameObject safeArea = CreateUiObject(objectName, parent);
            RectTransform rect = safeArea.GetComponent<RectTransform>();
            SetFullScreen(rect);
            return rect;
        }

        private static GameObject CreateUiObject(string objectName, Transform parent)
        {
            GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private static GameObject CreateLayer(
            string objectName,
            Transform parent,
            Vector2 position,
            Vector2 size,
            Color color,
            Sprite sprite,
            bool raycastTarget)
        {
            GameObject layer = CreateUiObject(objectName, parent);
            SetAnchoredRect(layer.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            Image image = layer.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            return layer;
        }

        private static TMP_Text CreateText(
            string objectName,
            Transform parent,
            string content,
            float fontSize,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            GameObject textObject = CreateUiObject(objectName, parent);
            SetAnchoredRect(textObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }

        private static void ApplySafeArea(RectTransform rect)
        {
            if (rect == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = PlatformSupport.SafeArea;
            rect.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            rect.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchoredRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
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
                whiteSprite.name = "RuntimeMobileWhiteSprite";
            }

            return whiteSprite;
        }

        private static Sprite GetCircleSprite()
        {
            if (circleSprite != null)
            {
                return circleSprite;
            }

            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "MobileControlCircle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            Vector2 center = new Vector2((size - 1f) * 0.5f, (size - 1f) * 0.5f);
            float radius = size * 0.47f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 1f - Mathf.Clamp01((distance - radius + 1.5f) / 3f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            circleSprite.name = "RuntimeMobileCircleSprite";
            return circleSprite;
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

    internal sealed class MobileVirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private MobileControlsUI owner;
        private RectTransform baseRect;
        private RectTransform knobRect;
        private int activePointerId = int.MinValue;

        public void Configure(MobileControlsUI controls, RectTransform joystickBase, RectTransform knob)
        {
            owner = controls;
            baseRect = joystickBase;
            knobRect = knob;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != int.MinValue)
            {
                return;
            }

            activePointerId = eventData.pointerId;
            UpdateValue(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId)
            {
                UpdateValue(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
            {
                return;
            }

            activePointerId = int.MinValue;
            if (knobRect != null)
            {
                knobRect.anchoredPosition = Vector2.zero;
            }

            owner?.SetMoveInput(Vector2.zero);
        }

        private void UpdateValue(PointerEventData eventData)
        {
            if (owner == null || baseRect == null || knobRect == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    baseRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            float radius = Mathf.Max(1f, Mathf.Min(baseRect.rect.width, baseRect.rect.height) * 0.34f);
            Vector2 clamped = Vector2.ClampMagnitude(localPoint, radius);
            knobRect.anchoredPosition = clamped;
            owner.SetMoveInput(clamped / radius);
        }
    }

    internal sealed class MobileLookSurface : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private MobileControlsUI owner;
        private int activePointerId = int.MinValue;

        public void Configure(MobileControlsUI controls)
        {
            owner = controls;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId == int.MinValue)
            {
                activePointerId = eventData.pointerId;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId)
            {
                owner?.AddLookDelta(eventData.delta);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId)
            {
                activePointerId = int.MinValue;
            }
        }
    }

    internal sealed class MobileTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        internal enum ButtonKind
        {
            Action,
            Pause
        }

        private MobileControlsUI owner;
        private ButtonKind buttonKind;

        public void Configure(MobileControlsUI controls, ButtonKind kind)
        {
            owner = controls;
            buttonKind = kind;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * 0.92f;
            if (buttonKind == ButtonKind.Action)
            {
                owner?.RequestAction();
            }
            else
            {
                owner?.RequestPause();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }
    }
}
