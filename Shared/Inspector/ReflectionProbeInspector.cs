using Graphics.Settings;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using static Graphics.Inspector.Util;

namespace Graphics.Inspector
{
    internal static class ReflectionProbeInspector
    {
        private static Vector2 probeSettingsScrollView;
        private static int cachedFontSize = -1;
        private static int paddingL;
        private static int paddingR;
        private static GUIStyle SmallBox;

        private static void UpdateCachedValues(GlobalSettings renderSettings)
        {
            if (cachedFontSize == renderSettings.FontSize)
                return;

            cachedFontSize = renderSettings.FontSize;
            paddingL = Mathf.RoundToInt(renderSettings.FontSize * 2f);
            paddingR = Mathf.RoundToInt(renderSettings.FontSize * 2.9f);

            SmallBox = new GUIStyle(GUIStyles.tabcontent);
            SmallBox.padding = new RectOffset(paddingL, paddingR, paddingL, 0);
            SmallBox.margin = new RectOffset(0, 0, 0, 5);
        }

        internal static void Draw(LightingSettings lightingSettings, SkyboxManager skyboxManager, GlobalSettings renderSettings, bool showAdvanced, int selectedProbeIndex)
        {
            UpdateCachedValues(renderSettings);

            probeSettingsScrollView = GUILayout.BeginScrollView(probeSettingsScrollView);

            GUILayout.BeginVertical(SmallBox);
            {
                Label("REFLECTION PROBE:", "", true);
                ReflectionProbe[] probes = skyboxManager.GetReflectinProbes();

                if (probes != null && probes.Length > 0)
                {
                    if (selectedProbeIndex >= probes.Length)
                        selectedProbeIndex = 0;

                    ReflectionProbe probe = probes[selectedProbeIndex];
                    if (probe != null)
                    {
                        GUILayout.Space(30);
                        Label("Probe Name", probe.name);
                        GUILayout.Space(10);
                        Toggle("Enabled", probe.enabled, false, enabled => { probe.enabled = enabled; QueueProbeUpdate(probe); });

                        GUI.enabled = probe.enabled;
                        Label("Type", probe.mode.ToString());
                        Selection("Refresh Mode", probe.refreshMode, mode => { probe.refreshMode = mode; QueueProbeUpdate(probe); });

                        GUILayout.Space(10);
                        Label("Runtime settings", "");
                        Slider("Importance", probe.importance, 0, 1000, importance => { probe.importance = importance; QueueProbeUpdate(probe); });
                        Slider("Intensity", probe.intensity, 0, 10, "N1", intensity => { probe.intensity = intensity; QueueProbeUpdate(probe); });
                        Toggle("Box Projection", probe.boxProjection, false, box => { probe.boxProjection = box; QueueProbeUpdate(probe); });
                        Text("Blend Distance", probe.blendDistance, "N0", distance => { probe.blendDistance = distance; QueueProbeUpdate(probe); });
                        Dimension("Box Size", probe.size, size => { probe.size = size; QueueProbeUpdate(probe); });
                        Dimension("Box Offset", probe.center, size => { probe.center = size; QueueProbeUpdate(probe); });

                        GUILayout.Space(10);
                        Label("Cubemap capture settings", "");
                        Selection("Resolution", probe.resolution, LightingSettings.ReflectionResolutions, resolution => { probe.resolution = resolution; QueueProbeUpdate(probe); });
                        Toggle("HDR", probe.hdr, false, hdr => { probe.hdr = hdr; QueueProbeUpdate(probe); });
                        Text("Shadow Distance", probe.shadowDistance, "N0", distance => { probe.shadowDistance = distance; QueueProbeUpdate(probe); });
                        Selection("Clear Flags", probe.clearFlags, flag => { probe.clearFlags = flag; QueueProbeUpdate(probe); });
                        SelectionMask("Culling Mask", probe.cullingMask, mask => { probe.cullingMask = mask; QueueProbeUpdate(probe); });
                        Text("Clipping Planes - Near", probe.nearClipPlane, "N2", plane => { probe.nearClipPlane = plane; QueueProbeUpdate(probe); });
                        Text("Clipping Planes - Far", probe.farClipPlane, "N0", plane => { probe.farClipPlane = plane; QueueProbeUpdate(probe); });
                        SliderColor("Background", probe.backgroundColor, colour => { probe.backgroundColor = colour; QueueProbeUpdate(probe); });
                        SelectionVertical("Time Slicing Mode", probe.timeSlicingMode, mode => { probe.timeSlicingMode = mode; QueueProbeUpdate(probe); });
                        GUILayout.Space(25);
                        GUI.enabled = true;
                    }
                }
                else
                {
                    GUILayout.Space(10);
                    Label("No reflection probes found in scene", "");
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(SmallBox);
            {
                Label("UPDATE SETTINGS", "", true);
                GUILayout.Space(10);
                Toggle("Realtime Reflection Probes", renderSettings.RealtimeReflectionProbes, false, realtime => renderSettings.RealtimeReflectionProbes = realtime);
                Toggle("Pulse Realtime Reflection Probes", renderSettings.PulseReflectionProbes, false, pulse => renderSettings.PulseReflectionProbes = pulse);
                if (renderSettings.PulseReflectionProbes)
                    Slider("Pulse Timing (Secs)", renderSettings.PulseReflectionTimer, 0.25f, 10f, "N1", prt => renderSettings.PulseReflectionTimer = prt);
                GUILayout.Space(30);
            }
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
        }

        private static void QueueProbeUpdate(ReflectionProbe probe)
        {
            if (Graphics.Instance != null)
            {
                Graphics.Instance.StartCoroutine(UpdateProbeNextFrame(probe));
            }
        }

        private static IEnumerator UpdateProbeNextFrame(ReflectionProbe probe)
        {
            if (probe.refreshMode == ReflectionProbeRefreshMode.OnAwake || probe.refreshMode == ReflectionProbeRefreshMode.ViaScripting)
            {
                yield return null;
                probe.RenderProbe();
            }
        }
    }
}
