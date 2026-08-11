using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CoffeeShop
{
    public static class PlatformSupport
    {
        private static int cachedTouchState = -1;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int CoffeeShop_IsMobileOrTouch();
#endif

        public static bool IsTouchDevice
        {
            get
            {
                if (cachedTouchState >= 0)
                {
                    return cachedTouchState == 1;
                }

#if UNITY_WEBGL && !UNITY_EDITOR
                bool detected;
                try
                {
                    detected = CoffeeShop_IsMobileOrTouch() == 1;
                }
                catch
                {
                    detected = Application.isMobilePlatform;
                }
#else
                bool detected = Application.isMobilePlatform || Touchscreen.current != null;
#endif

                cachedTouchState = detected ? 1 : 0;
                return detected;
            }
        }

        public static bool IsPortrait => Screen.height > Screen.width;

        public static Rect SafeArea
        {
            get
            {
                Rect safeArea = Screen.safeArea;
                if (safeArea.width <= 0f || safeArea.height <= 0f)
                {
                    return new Rect(0f, 0f, Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
                }

                return safeArea;
            }
        }

        public static void RefreshDetection()
        {
            cachedTouchState = -1;
        }
    }
}
