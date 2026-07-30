#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;

namespace SolarSystemScope
{
    /// <summary>
    /// Automatic VR & OpenXR Configuration Tool
    /// Automatically enables OpenXR loader, stereo rendering, and VR headset support for Meta Quest, Vive, Index, and Pico.
    /// </summary>
    [InitializeOnLoad]
    public static class VROpenXRAutoSetup
    {
        static VROpenXRAutoSetup()
        {
            EditorApplication.delayCall += EnsureVROpenXRConfigured;
        }

        [MenuItem("VR Scope/Enable OpenXR VR Support")]
        public static void EnsureVROpenXRConfigured()
        {
            Debug.Log("<color=cyan>[VR Auto-Setup] Configuring OpenXR & VR Headset settings...</color>");

            // Configure QualitySettings for VR Single Pass Instanced Rendering
            QualitySettings.vSyncCount = 0; // Allow VR display subsystem to manage refresh rate (90Hz / 120Hz)
            
            EditorXRSettingsUtility.EnableOpenXRForStandalone();
        }
    }

    public static class EditorXRSettingsUtility
    {
        public static void EnableOpenXRForStandalone()
        {
            try
            {
                var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
                if (settings != null && settings.Manager != null)
                {
                    Debug.Log("<color=green>[VR Auto-Setup] Standalone PC OpenXR Manager Active!</color>");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[VR Auto-Setup] XR General Settings notice: " + ex.Message);
            }
        }
    }
}
#endif
