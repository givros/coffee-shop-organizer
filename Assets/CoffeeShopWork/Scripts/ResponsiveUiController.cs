using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoffeeShop
{
    [DefaultExecutionOrder(-240)]
    [DisallowMultipleComponent]
    public sealed class ResponsiveUiController : MonoBehaviour
    {
        private static ResponsiveUiController instance;

        private int cachedWidth;
        private int cachedHeight;
        private Rect cachedSafeArea;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            GameObject runtimeObject = new GameObject("ResponsiveUiRuntime");
            DontDestroyOnLoad(runtimeObject);
            instance = runtimeObject.AddComponent<ResponsiveUiController>();
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
            StartCoroutine(ApplyAfterUiBuild());
        }

        private void Update()
        {
            Rect safeArea = PlatformSupport.SafeArea;
            if (Screen.width == cachedWidth && Screen.height == cachedHeight && safeArea == cachedSafeArea)
            {
                return;
            }

            ApplyLayout();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(ApplyAfterUiBuild());
        }

        private IEnumerator ApplyAfterUiBuild()
        {
            yield return null;
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            cachedWidth = Screen.width;
            cachedHeight = Screen.height;
            cachedSafeArea = PlatformSupport.SafeArea;

            ApplyMenuLayout();
            ApplyPauseLayout();
        }

        private static void ApplyMenuLayout()
        {
            Canvas menuCanvas = FindCanvas("MenuCanvas");
            if (menuCanvas == null)
            {
                return;
            }

            bool portrait = PlatformSupport.IsPortrait;
            CanvasScaler scaler = menuCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.referenceResolution = portrait
                    ? new Vector2(1080f, 1920f)
                    : new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = portrait ? 0f : 0.5f;
            }

            RectTransform mainView = FindRect(menuCanvas.transform, "MainView");
            RectTransform resultView = FindRect(menuCanvas.transform, "ResultView");
            ApplySafeArea(mainView);
            ApplySafeArea(resultView);

            RectTransform logo = FindRect(mainView, "GameLogo");
            RectTransform tagline = FindRect(mainView, "Tagline");
            RectTransform buttons = FindRect(mainView, "MainButtons");
            RectTransform medallion = FindRect(mainView, "ItemsMedallion");

            if (portrait)
            {
                SetTopPosition(logo, new Vector2(0f, -145f));
                SetTopPosition(tagline, new Vector2(0f, -565f));
                SetTopPosition(buttons, new Vector2(0f, -690f));
                SetTopPosition(medallion, new Vector2(0f, -980f));
                if (medallion != null)
                {
                    medallion.localScale = Vector3.one * 1.08f;
                }
            }
            else
            {
                SetTopPosition(logo, new Vector2(0f, -64f));
                SetTopPosition(tagline, new Vector2(0f, -402f));
                SetTopPosition(buttons, new Vector2(0f, -465f));
                SetTopPosition(medallion, new Vector2(-405f, -536f));
                if (medallion != null)
                {
                    medallion.localScale = Vector3.one;
                }
            }

            FitBoard(menuCanvas, "ResultBoard", new Vector2(960f, 720f), PlatformSupport.IsTouchDevice ? 1.02f : 1f);
            FitBoard(menuCanvas, "ResultBoardShadow", new Vector2(960f, 720f), PlatformSupport.IsTouchDevice ? 1.02f : 1f);

            TMP_Text controls = FindText(menuCanvas.transform, "Controls");
            TMP_Text gameName = FindText(menuCanvas.transform, "GameName");
            if (controls != null)
            {
                controls.text = PlatformSupport.IsTouchDevice
                    ? "LEFT THUMB  MOVE   |   SWIPE  LOOK   |   USE  INTERACT"
                    : "WASD  MOVE   |   MOUSE  LOOK   |   LEFT CLICK  INTERACT";
                controls.gameObject.SetActive(!portrait);
            }

            if (gameName != null)
            {
                RectTransform gameNameRect = gameName.rectTransform;
                if (portrait)
                {
                    gameName.alignment = TextAlignmentOptions.Center;
                    SetRect(
                        gameNameRect,
                        new Vector2(0.5f, 0f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        new Vector2(880f, 0f));
                }
                else
                {
                    gameName.alignment = TextAlignmentOptions.MidlineLeft;
                    SetRect(
                        gameNameRect,
                        new Vector2(0f, 0f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 0.5f),
                        new Vector2(44f, 0f),
                        new Vector2(450f, 0f));
                }
            }
        }

        private static void ApplyPauseLayout()
        {
            Canvas pauseCanvas = FindCanvas("PauseCanvas");
            if (pauseCanvas == null)
            {
                return;
            }

            bool portrait = PlatformSupport.IsPortrait;
            CanvasScaler scaler = pauseCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.referenceResolution = portrait
                    ? new Vector2(1080f, 1920f)
                    : new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = portrait ? 0f : 0.5f;
            }

            float preferredScale = PlatformSupport.IsTouchDevice ? (portrait ? 1.28f : 1.12f) : 1f;
            FitBoard(pauseCanvas, "PauseBoardFrame", new Vector2(620f, 500f), preferredScale);
            FitBoard(pauseCanvas, "PauseBoardShadow", new Vector2(620f, 500f), preferredScale);

            TMP_Text resumeHint = FindText(pauseCanvas.transform, "ResumeHint");
            if (resumeHint != null)
            {
                resumeHint.text = PlatformSupport.IsTouchDevice
                    ? "PAUSE BUTTON   RESUME"
                    : "ESC   RESUME";
            }
        }

        private static void FitBoard(Canvas canvas, string objectName, Vector2 designSize, float preferredScale)
        {
            RectTransform board = FindRect(canvas.transform, objectName);
            RectTransform canvasRect = canvas.transform as RectTransform;
            if (board == null || canvasRect == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = PlatformSupport.SafeArea;
            Vector2 canvasSize = canvasRect.rect.size;
            float safeWidth = canvasSize.x * safeArea.width / Screen.width;
            float safeHeight = canvasSize.y * safeArea.height / Screen.height;
            float fitScale = Mathf.Min(
                Mathf.Max(0.1f, safeWidth - 56f) / designSize.x,
                Mathf.Max(0.1f, safeHeight - 72f) / designSize.y);
            board.localScale = Vector3.one * Mathf.Min(preferredScale, fitScale);
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

        private static void SetTopPosition(RectTransform rect, Vector2 position)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
        }

        private static Canvas FindCanvas(string objectName)
        {
            Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            for (int index = 0; index < canvases.Length; index++)
            {
                Canvas canvas = canvases[index];
                if (canvas != null && canvas.gameObject.scene.IsValid() && canvas.name == objectName)
                {
                    return canvas;
                }
            }

            return null;
        }

        private static RectTransform FindRect(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
            for (int index = 0; index < rects.Length; index++)
            {
                if (rects[index] != null && rects[index].name == objectName)
                {
                    return rects[index];
                }
            }

            return null;
        }

        private static TMP_Text FindText(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < texts.Length; index++)
            {
                if (texts[index] != null && texts[index].name == objectName)
                {
                    return texts[index];
                }
            }

            return null;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
