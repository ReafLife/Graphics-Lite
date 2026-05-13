using System.Collections.Generic;
using Graphics.Textures;
using KKAPI.Utilities;
using UnityEngine;
using static Graphics.DebugUtils;

namespace Graphics
{
    [RequireComponent(typeof(Light))]
    public class CookieTextureManager : MonoBehaviour
    {
        public string cookiename;

        private Light lightComponent;
        private Texture originalCookie;

        private static EmbeddedTextureManager _sharedSpotCookieManager;
        private static bool _isInitialized;
        private static readonly object _initLock = new object();

        private static readonly List<string> _spotCookieTexturePaths = new List<string>()
        {
            "IES_01", "IES_02", "IES_03", "IES_04", "IES_05", "IES_06",
            "IES_07", "IES_08", "IES_09", "IES_10", "IES_11", "IES_12",
            "IES_13", "IES_14", "IES_15", "IES_16", "IES_17", "IES_18",
            "IES_19", "IES_20", "IES_21", "IES_22", "IES_23", "IES_24",
            "IES_25", "IES_26", "IES_27", "IES_28", "IES_29", "IES_30",
            "IES_31", "IES_32", "IES_33", "IES_34",
            "polkadots", "square", "window_02", "window_03", "circular_sharp_01", "circular_sharp_02",
            "disco_01", "disco_02", "disco_03", "disco_04", "disco_05", "disco_06", "disco_07", "disco_08", "disco_09",
            "fan_01", "fan_02", "flash_01", "flash_02", "flash_03", "flash_04", "mandala_01", "mandala_02", "mandala_03", "mandala_04", "mandala_05"
        };

        internal Texture CurrentSpotCookieTexture => string.IsNullOrEmpty(cookiename) ? null : LoadSpotCookie(cookiename);
        internal string CurrentSpotCookieName => cookiename ?? "";
        internal int CurrentSpotCookieIndex => string.IsNullOrEmpty(cookiename) ? 0 : GetTextureIndex(cookiename);
        internal string[] SPOTCookieNames => _sharedSpotCookieManager?.TextureNames ?? new string[0];
        internal EmbeddedTextureManager _spotCookieManager => _sharedSpotCookieManager;

        public static void InitializeSharedManager()
        {
            lock (_initLock)
            {
                if (_isInitialized)
                    return;

                Graphics.Instance.Log.LogInfo("Initializing shared CookieTextureManager");

                GameObject managerObject = new GameObject("SharedCookieTextureManager");
                DontDestroyOnLoad(managerObject);

                _sharedSpotCookieManager = managerObject.AddComponent<EmbeddedTextureManager>();
                _sharedSpotCookieManager.TexturePaths = _spotCookieTexturePaths;
                _sharedSpotCookieManager.SearchPattern = "";
                _sharedSpotCookieManager.ResourcePath = "spot_cookies.unity3d";

                _isInitialized = true;
            }
        }

        public static bool IsManagerReady()
        {
            return _isInitialized && _sharedSpotCookieManager != null && _sharedSpotCookieManager.HasAssetsLoaded;
        }

        private int GetTextureIndex(string name)
        {
            if (_sharedSpotCookieManager == null || string.IsNullOrEmpty(name))
                return 0;

            var names = _sharedSpotCookieManager.TextureNames;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == name)
                    return i;
            }

            return 0;
        }

        internal Texture LoadSpotCookie(int index)
        {
            if (_sharedSpotCookieManager == null)
            {
                Graphics.Instance.Log.LogWarning("Shared cookie manager not initialized!");
                return null;
            }

            return _sharedSpotCookieManager.GetTexture(index);
        }

        internal Texture LoadSpotCookie(string name)
        {
            if (string.IsNullOrEmpty(name) || _sharedSpotCookieManager == null)
                return null;

            return _sharedSpotCookieManager.GetTexture(name);
        }

        private void Awake()
        {
            lightComponent = GetComponent<Light>();
            originalCookie = lightComponent != null ? lightComponent.cookie : null;

            if (!_isInitialized)
            {
                InitializeSharedManager();
            }
        }

        private void Start()
        {
            enabled = false;
        }

        public void OnEnable()
        {
            if (!_isInitialized)
            {
                InitializeSharedManager();
            }

            if (!string.IsNullOrEmpty(cookiename))
            {
                ApplyCookieTexture(cookiename);
            }
        }

        public void OnDisable()
        {
            if (lightComponent != null)
            {
                lightComponent.cookie = originalCookie;
            }
        }

        public void ApplyCookieTexture(string name)
        {
            if (lightComponent == null)
            {
                Graphics.Instance.Log.LogWarning("Light component is null!");
                return;
            }

            if (!IsManagerReady())
            {
                Graphics.Instance.Log.LogWarning($"Cookie manager not ready, cannot apply texture '{name}'");
                return;
            }

            Texture cookieTexture = LoadSpotCookie(name);
            if (cookieTexture == null)
            {
                Graphics.Instance.Log.LogWarning($"Cookie texture '{name}' could not be loaded");
                return;
            }

            cookiename = name;
            lightComponent.cookie = cookieTexture;
            LogWithDotsLight($"{lightComponent.name} cookie texture ", name);
        }
    }
}
