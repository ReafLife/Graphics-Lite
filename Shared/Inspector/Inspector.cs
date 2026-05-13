using ADV.Commands.Base;
using Graphics.AmplifyOcclusion;
using Graphics.GlobalFog;
using Graphics.GTAO;
using Graphics.Settings;
using Graphics.VAO;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Graphics.Inspector
{
    internal class Inspector
    {
        private static Rect _windowRect;
        private readonly int _windowID = 53157126;
        public static int adaptiveColumns;
        public static int adaptivePreviewColumns;
        private static int cachedWindowWidth = -1;
        public static int maxTextWidth;

        public static float currentToolbarWidth = 180f;

        private int _selectedProbeIndex;
        private Vector2 _postProcessingScrollPosition;
        private Vector2 _reflectionProbesScrollPosition;

        public enum Tab
        {
            Environmental,
            Probes,
            Lights,
            GI,
            PostProcessing,
            AntiAliasing,
            SSS,
            Presets,
            Settings,
            Screenshot
        }

        public enum PostProcessingSubTab
        {
            Volume,
            AmbientOcclusion,
            Bloom,
            ColorGrading,
            SSR,
            AutoExposure,
            ChromaticAberration,
            DepthOfField,
            Grain,
            Vignette,
            MotionBlur,
            SunShafts,
            GlobalFog,
            LuxWater,
            Aura
        }

        public static readonly Dictionary<Tab, string> DisplayNames = new Dictionary<Tab, string>
        {
            { Tab.Environmental, "Environment" },
            { Tab.Probes, "Probes" },
            { Tab.Lights, "Lights" },
            { Tab.GI, "Global Illumination" },
            { Tab.PostProcessing, "Post Processing" },
            { Tab.AntiAliasing, "Anti-Aliasing" },
            { Tab.SSS, "SSS" },
            { Tab.Presets, "Presets" },
            { Tab.Settings, "Settings" },
            { Tab.Screenshot, "Screenshot" }
        };

        public static readonly Dictionary<PostProcessingSubTab, string> PostProcessingDisplayNames = new Dictionary<PostProcessingSubTab, string>
        {
            { PostProcessingSubTab.Volume, "Volume" },
            { PostProcessingSubTab.AmbientOcclusion, "Ambient Occlusion" },
            { PostProcessingSubTab.Bloom, "Bloom" },
            { PostProcessingSubTab.ColorGrading, "Color Grading" },
            { PostProcessingSubTab.SSR, "SSR" },
            { PostProcessingSubTab.AutoExposure, "Auto Exposure" },
            { PostProcessingSubTab.ChromaticAberration, "Chromatic Effects" },
            { PostProcessingSubTab.DepthOfField, "Depth Of Field" },
            { PostProcessingSubTab.Grain, "Grain" },
            { PostProcessingSubTab.Vignette, "Vignette" },
            { PostProcessingSubTab.MotionBlur, "Motion Blur" },
            { PostProcessingSubTab.SunShafts, "Sun Shafts HDR" },
            { PostProcessingSubTab.GlobalFog, "Global Fog" },
            { PostProcessingSubTab.LuxWater, "Lux Water" },
            { PostProcessingSubTab.Aura, "Aura" }
        };

        private Tab SelectedTab { get; set; }
        private PostProcessingSubTab SelectedPostProcessingSubTab { get; set; }
        internal Graphics Parent { get; set; }

        internal Inspector(Graphics parent)
        {
            Parent = parent;

            if (StartOffsetX + 20 > Screen.width || StartOffsetY + 20 > Screen.height)
            {
                StartOffsetX = (Screen.width - Graphics.ConfigWindowWidth.Value) / 2;
                StartOffsetY = (Screen.height - Graphics.ConfigWindowHeight.Value) / 2;
            }

            if (StartOffsetX < 0) StartOffsetX = 0;
            if (StartOffsetY < 0) StartOffsetY = 0;

            _windowRect = new Rect(StartOffsetX, StartOffsetY, Width, Height);
        }

        internal static int Width
        {
            get => Graphics.ConfigWindowWidth.Value;
            set { Graphics.ConfigWindowWidth.Value = value; _windowRect.width = value; }
        }

        internal static int Height
        {
            get => Graphics.ConfigWindowHeight.Value;
            set { Graphics.ConfigWindowHeight.Value = value; _windowRect.height = value; }
        }

        internal static int StartOffsetX
        {
            get => Graphics.ConfigWindowOffsetX.Value;
            set => Graphics.ConfigWindowOffsetX.Value = value;
        }

        internal static int StartOffsetY
        {
            get => Graphics.ConfigWindowOffsetY.Value;
            set => Graphics.ConfigWindowOffsetY.Value = value;
        }

        private static void UpdateColumns(GlobalSettings renderSettings)
        {
            if (cachedWindowWidth == Width)
                return;

            // 22% of window width, clamped between 120 px and 220 px
            currentToolbarWidth = Mathf.Clamp(Width * 0.22f, 120f, 220f);

            maxTextWidth = Width - (int)currentToolbarWidth;
            cachedWindowWidth = Width;

            int availableWidth = Width - (int)currentToolbarWidth - 60; // 60 px padding
            adaptiveColumns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / 150f));
            adaptivePreviewColumns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / 74f));
        }

        internal void DrawWindow()
        {
            _windowRect = GUILayout.Window(_windowID, _windowRect, WindowFunction, "");
            EatInputInRect(_windowRect);
            StartOffsetX = (int)_windowRect.x;
            StartOffsetY = (int)_windowRect.y;

            UpdateColumns(Parent.Settings);
        }

        private void WindowFunction(int thisWindowID)
        {
            GUILayout.BeginHorizontal(GUIStyles.headerStyle);
            {
                GUILayout.Space(80);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Graphics Plugin Lite {Graphics.Version}", GUIStyles.windowLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X", GUIStyles.closeButton))
                    Parent.ToggleGUI();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUIStyles.boxPadding);

            // ── Left column: adaptive width, main tabs NOT in a scroll view ──────────
            GUILayout.BeginVertical(GUILayout.Width(currentToolbarWidth));

            SelectedTab = VerticalToolbar(SelectedTab);

            // PostProcessing sub-tabs appear only on that tab, inside their own scroll
            if (SelectedTab == Tab.PostProcessing)
            {
                GUILayout.Space(10);
                GUILayout.Box(GUIContent.none, GUIStyles.lineStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(10);

                _postProcessingScrollPosition = GUILayout.BeginScrollView(
                    _postProcessingScrollPosition,
                    GUILayout.ExpandHeight(true),
                    GUILayout.ExpandWidth(true));
                SelectedPostProcessingSubTab = PostProcessingVerticalToolbar(
                    SelectedPostProcessingSubTab, Parent.PostProcessingSettings);
                GUILayout.EndScrollView();
            }

            // Probe list appears only on the Probes tab, inside its own scroll
            if (SelectedTab == Tab.Probes)
            {
                GUILayout.Space(10);
                GUILayout.Box(GUIContent.none, GUIStyles.lineStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(10);

                _reflectionProbesScrollPosition = GUILayout.BeginScrollView(
                    _reflectionProbesScrollPosition,
                    GUILayout.ExpandHeight(true),
                    GUILayout.ExpandWidth(true));
                _selectedProbeIndex = ProbesVerticalToolbar(_selectedProbeIndex, Parent.SkyboxManager);
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
            // ─────────────────────────────────────────────────────────────────────────

            // ── Right content area ───────────────────────────────────────────────────
            GUILayout.BeginVertical(GUIStyles.rightTab, GUILayout.ExpandWidth(true));
            DrawTabs(SelectedTab);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            // ─────────────────────────────────────────────────────────────────────────

            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        private void DrawTabs(Tab tabSelected)
        {
            switch (tabSelected)
            {
                case Tab.Environmental:
                    LightingInspector.Draw(Parent.LightingSettings, Parent.SkyboxManager, Parent.LightManager, Parent.Settings, Parent.Settings.ShowAdvancedSettings);
                    break;
                case Tab.Lights:
                    LightInspector.Draw(Parent.Settings, Parent.LightManager, Parent.LightingSettings, Parent.Settings.ShowAdvancedSettings);
                    break;
                case Tab.Probes:
                    ReflectionProbeInspector.Draw(Parent.LightingSettings, Parent.SkyboxManager, Parent.Settings, Parent.Settings.ShowAdvancedSettings, _selectedProbeIndex);
                    break;
                case Tab.GI:
                    SEGIInspector.Draw(Parent.LightManager, Parent.Settings);
                    break;
                case Tab.PostProcessing:
                    PostProcessingInspector.Draw(Parent.LightManager, Parent.PostProcessingSettings, Parent.Settings, Parent.PostProcessingManager, Parent.Settings.ShowAdvancedSettings, SelectedPostProcessingSubTab);
                    break;
                case Tab.AntiAliasing:
                    AntiAliasingInspector.Draw(Parent.Settings, Parent.CameraSettings, Parent.PostProcessingSettings, Parent.PostProcessingManager, Parent.Settings.ShowAdvancedSettings);
                    break;
                case Tab.SSS:
                    SSSInspector.Draw(Parent.Settings);
                    break;
                case Tab.Presets:
                    PresetInspector.Draw(Parent.PresetManager, Parent.Settings, Parent.Settings.ShowAdvancedSettings);
                    break;
                case Tab.Settings:
                    SettingsInspector.Draw(Parent.CameraSettings, Parent.Settings, Parent.LightManager, Parent.Settings.ShowAdvancedSettings);
                    break;
                case Tab.Screenshot:
                    ScreenshotInspector.Draw(Parent.Settings);
                    break;
            }
        }

        private int ProbesVerticalToolbar(int selectedIndex, SkyboxManager skyboxManager)
        {
            ReflectionProbe[] probes = skyboxManager.GetReflectinProbes();
            if (probes == null || probes.Length == 0)
            {
                GUILayout.Label("No probes found", GUIStyles.subToolbarButton);
                return 0;
            }

            if (selectedIndex >= probes.Length)
                selectedIndex = 0;

            for (int i = 0; i < probes.Length; i++)
            {
                ReflectionProbe probe = probes[i];
                string probeName = probe != null ? probe.name : $"Probe {i}";
                if (probeName.Length > 18)
                    probeName = probeName.Substring(0, 18) + "...";

                GUIStyle buttonStyle = selectedIndex == i ? GUIStyles.activestylebutton : GUIStyles.subToolbarButton;
                if (GUILayout.Button(probeName, buttonStyle))
                    selectedIndex = i;
            }

            return selectedIndex;
        }

        private PostProcessingSubTab PostProcessingVerticalToolbar(PostProcessingSubTab selected, PostProcessingSettings settings)
        {
            foreach (KeyValuePair<PostProcessingSubTab, string> subTab in PostProcessingDisplayNames)
            {
                bool isEnabled = IsPostProcessingEffectEnabled(subTab.Key, settings);
                string displayName = LocalizationManager.HasLocalization() ? LocalizationManager.Localized(subTab.Value) : subTab.Value;
                GUIStyle buttonStyle;

                if (selected == subTab.Key)
                    buttonStyle = isEnabled ? GUIStyles.activeSubToolbarButtonSelected : GUIStyles.activestylebutton;
                else if (isEnabled)
                    buttonStyle = GUIStyles.activeSubToolbarButton;
                else
                    buttonStyle = GUIStyles.subToolbarButton;

                if (GUILayout.Button(displayName, buttonStyle))
                    selected = subTab.Key;
            }

            return selected;
        }

        private bool IsPostProcessingEffectEnabled(PostProcessingSubTab subTab, PostProcessingSettings settings)
        {
            if (settings == null)
                return false;

            switch (subTab)
            {
                case PostProcessingSubTab.Volume:
                    return settings.Volume != null;

                case PostProcessingSubTab.AmbientOcclusion:
                    return settings.ambientOcclusionLayer != null && settings.ambientOcclusionLayer.enabled.value
                        || VAOManager.settings != null && VAOManager.settings.Enabled
                        || GTAOManager.settings != null && GTAOManager.settings.Enabled
                        || AmplifyOccManager.settings != null && AmplifyOccManager.settings.Enabled;

                case PostProcessingSubTab.Bloom:
                    return settings.bloomLayer != null && settings.bloomLayer.enabled.value;

                case PostProcessingSubTab.ColorGrading:
                    return settings.colorGradingLayer != null && settings.colorGradingLayer.enabled.value
                        || settings.agxColorLayer != null && settings.agxColorLayer.enabled.value
                        || settings.agxColorPostLayer != null && settings.agxColorPostLayer.enabled.value
                        || settings.colorClippingLayer != null && settings.colorClippingLayer.enabled.value;

                case PostProcessingSubTab.SSR:
                    return settings.screenSpaceReflectionsLayer != null && settings.screenSpaceReflectionsLayer.enabled.value;

                case PostProcessingSubTab.AutoExposure:
                    return settings.autoExposureLayer != null && settings.autoExposureLayer.enabled.value;

                case PostProcessingSubTab.ChromaticAberration:
                    return settings.chromaticAberrationLayer != null && settings.chromaticAberrationLayer.enabled.value;

                case PostProcessingSubTab.DepthOfField:
                    return settings.depthOfFieldLayer != null && settings.depthOfFieldLayer.enabled.value;

                case PostProcessingSubTab.Grain:
                    return FilmGrainManager.settings != null && FilmGrainManager.settings.enabled;

                case PostProcessingSubTab.Vignette:
                    return settings.vignetteLayer != null && settings.vignetteLayer.enabled.value;

                case PostProcessingSubTab.MotionBlur:
                    return settings.motionBlurLayer != null && settings.motionBlurLayer.enabled.value;

                case PostProcessingSubTab.SunShafts:
                    return settings.sunShaftsHDRLayer != null && settings.sunShaftsHDRLayer.enabled.value;

                case PostProcessingSubTab.GlobalFog:
                    return GlobalFogManager.settings != null && GlobalFogManager.settings.Enabled;

                case PostProcessingSubTab.LuxWater:
                    return LuxWater_UnderWaterRenderingManager.settings != null && LuxWater_UnderWaterRenderingManager.settings.Enabled;

                case PostProcessingSubTab.Aura:
                    return AuraManager.Available && AuraManager.settings != null && AuraManager.settings.Enabled;

                default:
                    return false;
            }
        }

        private Tab VerticalToolbar(Tab selected)
        {
            GUILayout.BeginVertical();

            foreach (KeyValuePair<Tab, string> tab in DisplayNames)
            {
                string displayName = LocalizationManager.HasLocalization() ? LocalizationManager.Localized(tab.Value) : tab.Value;
                GUIStyle style = selected == tab.Key ? GUIStyles.activestylebutton : GUIStyles.toolbarbutton;
                if (GUILayout.Button(displayName, style))
                    selected = tab.Key;
            }

            GUILayout.EndVertical();
            return selected;
        }

        private static void EatInputInRect(Rect eatRect)
        {
            if (eatRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                Input.ResetInputAxes();
        }
    }
}