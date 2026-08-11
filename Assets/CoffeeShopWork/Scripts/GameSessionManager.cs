using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoffeeShop
{
    [DefaultExecutionOrder(-300)]
    [DisallowMultipleComponent]
    public sealed class GameSessionManager : MonoBehaviour
    {
        public const string MenuSceneName = "MenuScene";
        public const string GameplaySceneName = "MainScene";
        public const string LastTimeKey = "CoffeeShop.LastTime";
        public const string LastCompletedKey = "CoffeeShop.LastCompleted";
        public const string LastTargetKey = "CoffeeShop.LastTarget";
        public const string ShowResultKey = "CoffeeShop.ShowResult";

        public static GameSessionManager Instance { get; private set; }
        public static event Action StateChanged;

        [Header("Timer")]
        [SerializeField, Min(1f)] private float targetTimeSeconds = 180f;

        private int targetObjectCount;

        public bool IsGameRunning { get; private set; }
        public bool HasFinished { get; private set; }
        public bool IsPaused { get; private set; }
        public bool RequiresSceneRestart { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public float LastGameSeconds { get; private set; }
        public int LastCompletedObjectCount { get; private set; }
        public int LastTargetObjectCount { get; private set; }
        public float TargetTimeSeconds => targetTimeSeconds;
        public int TargetObjectCount => targetObjectCount > 0 ? targetObjectCount : PlaceableObject.TotalObjectCount;

        private void Awake()
        {
            Application.runInBackground = true;

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Time.timeScale = 0f;
        }

        private void OnEnable()
        {
            PlaceableObject.ProgressChanged += HandleProgressChanged;
        }

        private void Start()
        {
            if (!IsGameRunning && !HasFinished)
            {
                StartGame();
            }
        }

        private void OnDisable()
        {
            PlaceableObject.ProgressChanged -= HandleProgressChanged;
        }

        private void Update()
        {
            if (!IsGameRunning || IsPaused)
            {
                return;
            }

            ElapsedSeconds += Time.unscaledDeltaTime;

            if (targetObjectCount > 0 && PlaceableObject.CompletedObjectCount >= targetObjectCount)
            {
                FinishGame();
            }
        }

        public void StartGame()
        {
            if (RequiresSceneRestart)
            {
                RestartCurrentScene();
                return;
            }

            targetObjectCount = PlaceableObject.TotalObjectCount;
            ElapsedSeconds = 0f;
            LastGameSeconds = 0f;
            LastCompletedObjectCount = 0;
            LastTargetObjectCount = targetObjectCount;
            HasFinished = false;
            IsPaused = false;
            IsGameRunning = true;
            Time.timeScale = 1f;
            StateChanged?.Invoke();
        }

        public void PauseGame()
        {
            if (!IsGameRunning || HasFinished || IsPaused)
            {
                return;
            }

            IsPaused = true;
            Time.timeScale = 0f;
            StateChanged?.Invoke();
        }

        public void ResumeGame()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            Time.timeScale = 1f;
            StateChanged?.Invoke();
        }

        public void ReturnToMenu()
        {
            IsGameRunning = false;
            HasFinished = false;
            IsPaused = false;
            Time.timeScale = 1f;
            StateChanged?.Invoke();
            SceneManager.LoadScene(MenuSceneName);
        }

        public void FinishGame()
        {
            if (!IsGameRunning)
            {
                return;
            }

            IsGameRunning = false;
            HasFinished = true;
            IsPaused = false;
            RequiresSceneRestart = true;
            LastGameSeconds = ElapsedSeconds;
            LastCompletedObjectCount = PlaceableObject.CompletedObjectCount;
            LastTargetObjectCount = targetObjectCount;

            PlayerPrefs.SetFloat(LastTimeKey, LastGameSeconds);
            PlayerPrefs.SetInt(LastCompletedKey, LastCompletedObjectCount);
            PlayerPrefs.SetInt(LastTargetKey, LastTargetObjectCount);
            PlayerPrefs.SetInt(ShowResultKey, 1);
            PlayerPrefs.Save();

            StateChanged?.Invoke();

            // Results live in the dedicated menu scene, so gameplay never carries a menu overlay.
            Time.timeScale = 1f;
            SceneManager.LoadScene(MenuSceneName);
        }

        public void RestartCurrentScene()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            string scenePath = SceneManager.GetActiveScene().path;
            SceneManager.LoadScene(scenePath);
        }

        public static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;
            return string.Format("{0:00}:{1:00}", minutes, remainingSeconds);
        }

        private void HandleProgressChanged()
        {
            if (IsGameRunning && targetObjectCount > 0 && PlaceableObject.CompletedObjectCount >= targetObjectCount)
            {
                FinishGame();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Time.timeScale = 1f;
                Instance = null;
            }
        }
    }
}
