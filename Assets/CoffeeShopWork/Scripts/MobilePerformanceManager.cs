using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoffeeShop
{
    [DefaultExecutionOrder(-320)]
    [DisallowMultipleComponent]
    public sealed class MobilePerformanceManager : MonoBehaviour
    {
        private static MobilePerformanceManager instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            GameObject runtimeObject = new GameObject("MobilePerformanceRuntime");
            DontDestroyOnLoad(runtimeObject);
            instance = runtimeObject.AddComponent<MobilePerformanceManager>();
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
            ApplyGlobalSettings();
        }

        private void Start()
        {
            StartCoroutine(ApplySceneSettings());
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyGlobalSettings();
            StartCoroutine(ApplySceneSettings());
        }

        private static void ApplyGlobalSettings()
        {
            bool mobile = PlatformSupport.IsTouchDevice;
            Application.targetFrameRate = 60;
            Application.backgroundLoadingPriority = ThreadPriority.Low;
            QualitySettings.vSyncCount = 0;

            if (!mobile)
            {
                return;
            }

            if (QualitySettings.names.Length > 0 && QualitySettings.GetQualityLevel() != 0)
            {
                QualitySettings.SetQualityLevel(0, true);
            }

            QualitySettings.pixelLightCount = 1;
            QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, 25f);
            QualitySettings.lodBias = Mathf.Min(QualitySettings.lodBias, 1f);
            Shader.globalMaximumLOD = 300;
        }

        private static IEnumerator ApplySceneSettings()
        {
            yield return null;

            if (!PlatformSupport.IsTouchDevice)
            {
                yield break;
            }

            Camera[] cameras = Camera.allCameras;
            for (int index = 0; index < cameras.Length; index++)
            {
                Camera camera = cameras[index];
                if (camera == null)
                {
                    continue;
                }

                camera.allowHDR = false;
                camera.allowMSAA = false;
                if (!camera.orthographic)
                {
                    camera.farClipPlane = Mathf.Min(camera.farClipPlane, 90f);
                }
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
    }
}
