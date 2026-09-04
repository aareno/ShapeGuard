using System.Collections.Generic;
using UnityEngine;

namespace ShapeGuard
{
    /// <summary>Player preferences that are independent from game-progress saves.</summary>
    public static class GameSettings
    {
        private const string VolumeKey = "settings.master-volume";
        private const string DisplayModeKey = "settings.display-mode";
        private const string ResolutionWidthKey = "settings.resolution-width";
        private const string ResolutionHeightKey = "settings.resolution-height";
        private const string VSyncKey = "settings.vsync";
        private const string FrameRateKey = "settings.frame-rate";

        private static readonly int[] FrameRateOptions = { 30, 60, 120, 144, -1 };
        private static readonly List<Vector2Int> resolutions = new();
        private static bool loaded;

        public static float MasterVolume { get; private set; } = .8f;
        public static FullScreenMode DisplayMode { get; private set; } = FullScreenMode.FullScreenWindow;
        public static bool VSync { get; private set; } = true;
        public static int FrameRateLimit { get; private set; } = 60;
        public static string ResolutionLabel => $"{Screen.width} x {Screen.height}";
        public static string FrameRateLabel => FrameRateLimit < 0 ? "UNLIMITED" : $"{FrameRateLimit} FPS";

        public static void LoadAndApply()
        {
            if (loaded) return;
            loaded = true;
            BuildResolutionList();

            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, .8f));
            DisplayMode = ReadDisplayMode(PlayerPrefs.GetInt(DisplayModeKey, (int)Screen.fullScreenMode));
            VSync = PlayerPrefs.GetInt(VSyncKey, 1) != 0;
            FrameRateLimit = NormalizeFrameRate(PlayerPrefs.GetInt(FrameRateKey, 60));

            var width = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
            var height = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);
            AudioListener.volume = MasterVolume;
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync ? -1 : FrameRateLimit;
            Screen.SetResolution(width, height, DisplayMode);
        }

        public static void SetMasterVolume(float value)
        {
            LoadAndApply();
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(MasterVolume, value)) return;
            MasterVolume = value;
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(VolumeKey, value);
            PlayerPrefs.Save();
        }

        public static void SetDisplayMode(FullScreenMode mode)
        {
            LoadAndApply();
            DisplayMode = mode;
            Screen.SetResolution(Screen.width, Screen.height, mode);
            SaveDisplay();
        }

        public static void CycleResolution(int direction)
        {
            LoadAndApply();
            if (resolutions.Count == 0) return;
            var closest = 0;
            var closestDistance = int.MaxValue;
            for (var index = 0; index < resolutions.Count; index++)
            {
                var candidate = resolutions[index];
                var distance = Mathf.Abs(candidate.x - Screen.width) + Mathf.Abs(candidate.y - Screen.height);
                if (distance >= closestDistance) continue;
                closestDistance = distance;
                closest = index;
            }

            var selected = (closest + (direction < 0 ? -1 : 1) + resolutions.Count) % resolutions.Count;
            var resolution = resolutions[selected];
            Screen.SetResolution(resolution.x, resolution.y, DisplayMode);
            PlayerPrefs.SetInt(ResolutionWidthKey, resolution.x);
            PlayerPrefs.SetInt(ResolutionHeightKey, resolution.y);
            PlayerPrefs.Save();
        }

        public static void ToggleVSync()
        {
            LoadAndApply();
            VSync = !VSync;
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync ? -1 : FrameRateLimit;
            PlayerPrefs.SetInt(VSyncKey, VSync ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void CycleFrameRate()
        {
            LoadAndApply();
            var index = System.Array.IndexOf(FrameRateOptions, FrameRateLimit);
            FrameRateLimit = FrameRateOptions[(index + 1 + FrameRateOptions.Length) % FrameRateOptions.Length];
            Application.targetFrameRate = VSync ? -1 : FrameRateLimit;
            PlayerPrefs.SetInt(FrameRateKey, FrameRateLimit);
            PlayerPrefs.Save();
        }

        public static void ResetDefaults()
        {
            MasterVolume = .8f;
            DisplayMode = FullScreenMode.FullScreenWindow;
            VSync = true;
            FrameRateLimit = 60;
            AudioListener.volume = MasterVolume;
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, DisplayMode);
            PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
            PlayerPrefs.SetInt(VSyncKey, 1);
            PlayerPrefs.SetInt(FrameRateKey, FrameRateLimit);
            PlayerPrefs.SetInt(ResolutionWidthKey, Screen.currentResolution.width);
            PlayerPrefs.SetInt(ResolutionHeightKey, Screen.currentResolution.height);
            SaveDisplay();
        }

        private static void BuildResolutionList()
        {
            resolutions.Clear();
            foreach (var resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (!resolutions.Contains(size)) resolutions.Add(size);
            }
            if (resolutions.Count == 0) resolutions.Add(new Vector2Int(Screen.width, Screen.height));
            resolutions.Sort((left, right) =>
            {
                var pixelComparison = (left.x * left.y).CompareTo(right.x * right.y);
                return pixelComparison != 0 ? pixelComparison : left.x.CompareTo(right.x);
            });
        }

        private static FullScreenMode ReadDisplayMode(int value) => value switch
        {
            (int)FullScreenMode.ExclusiveFullScreen => FullScreenMode.ExclusiveFullScreen,
            (int)FullScreenMode.Windowed => FullScreenMode.Windowed,
            _ => FullScreenMode.FullScreenWindow
        };

        private static int NormalizeFrameRate(int value)
        {
            foreach (var option in FrameRateOptions) if (option == value) return value;
            return 60;
        }

        private static void SaveDisplay()
        {
            PlayerPrefs.SetInt(DisplayModeKey, (int)DisplayMode);
            PlayerPrefs.Save();
        }
    }
}
