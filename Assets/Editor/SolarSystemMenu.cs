using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SolarSystemScope
{
    public class SolarSystemMenu
    {
        [MenuItem("Solar System/Clean & Build 3D Solar System Scene", false, 1)]
        public static void CleanSceneInEditor()
        {
            if (EditorApplication.isPlaying || Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

            // 1. Destroy all magenta room floors, pedestals, teleport pads, and sample objects in scene
#pragma warning disable 0618
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);
#pragma warning restore 0618
            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;
                string n = obj.name.ToLower();
                if (n.Contains("solar") || n.Contains("sun") || n.Contains("camera") || n.Contains("light")) continue;

                // Check for room elements, magenta ground, pedestals, pads, environment, duplicate root moons, and old halo quads
                if (n.Contains("environment") || n.Contains("pedestal") || n.Contains("floor") || 
                    n.Contains("ground") || n.Contains("base") || n.Contains("room") || 
                    n.Contains("pad") || n.Contains("target") || n.Contains("structure") || 
                    n.Contains("plane") || n.Contains("cube") || n.Contains("cylinder") || n.Contains("mars") ||
                    n.Contains("halo") || n.Contains("corona") || n.Contains("flare") || n.Contains("aura") || n.Contains("atmosphere") ||
                    (n.StartsWith("moon") && obj.transform.parent == null))
                {
                    Object.DestroyImmediate(obj);
                }
            }

            GameObject existingRoot = GameObject.Find("SolarSystemRoot");
            if (existingRoot != null)
            {
                Object.DestroyImmediate(existingRoot);
            }

            // Destroy all extra EventSystems in Editor mode
#pragma warning disable 0618
            var eventSystems = Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
            for (int i = 1; i < eventSystems.Length; i++)
            {
                if (eventSystems[i] != null) Object.DestroyImmediate(eventSystems[i].gameObject);
            }
#pragma warning restore 0618

            // 2. Build complete 3D Solar System
            SolarSystemBootstrapper.AutoSetupSolarSystemOnPlay();

            // 3. Mark Scene Dirty and Save
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }

            Debug.Log("<color=green>[Solar System Scope] Cleaned scene and built 3D Solar System successfully!</color>");
        }
    }
}
