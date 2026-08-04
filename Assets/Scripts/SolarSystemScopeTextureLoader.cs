using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

namespace SolarSystemScope
{
    public class SolarSystemScopeTextureLoader : MonoBehaviour
    {
        public static SolarSystemScopeTextureLoader Instance { get; private set; }

        private const string BASE_URL = "https://www.solarsystemscope.com/textures/download/";

        private readonly Dictionary<string, string> textureMapUrls = new Dictionary<string, string>()
        {
            { "Sun", BASE_URL + "8k_sun.jpg" },
            { "Mercury", BASE_URL + "8k_mercury.jpg" },
            { "Venus", BASE_URL + "8k_venus_surface.jpg" },
            { "Earth", BASE_URL + "8k_earth_daymap.jpg" },
            { "Moon", BASE_URL + "8k_moon.jpg" },
            { "Mars", BASE_URL + "8k_mars.jpg" },
            { "Jupiter", BASE_URL + "8k_jupiter.jpg" },
            { "Saturn", BASE_URL + "8k_saturn.jpg" },
            { "SaturnRings", BASE_URL + "8k_saturn_ring_alpha.png" },
            { "Uranus", BASE_URL + "2k_uranus.jpg" },
            { "Neptune", BASE_URL + "2k_neptune.jpg" },
            { "MilkyWaySkybox", BASE_URL + "8k_stars_milky_way.jpg" }
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            StartCoroutine(LoadAllSolarSystemScopeTextures());
        }

        private readonly Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>();

        public IEnumerator LoadAllSolarSystemScopeTextures()
        {
            // Wait until SolarSystemBootstrapper finishes instantiating scene objects
            yield return null;
            yield return new WaitForEndOfFrame();

            foreach (var kvp in textureMapUrls)
            {
                string bodyName = kvp.Key;
                string url = kvp.Value;
                yield return StartCoroutine(FetchAndApplyTexture(bodyName, url));
            }

            UpdateCollagePlanet();
        }

        private IEnumerator FetchAndApplyTexture(string bodyName, string url)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success && www.downloadHandler != null && www.downloadHandler.data != null && www.downloadHandler.data.Length > 0)
                {
                    Texture2D downloadedTex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                    if (downloadedTex.LoadImage(www.downloadHandler.data))
                    {
                        downloadedTex.filterMode = FilterMode.Trilinear;
                        downloadedTex.anisoLevel = 16;
                        downloadedTex.wrapModeU = TextureWrapMode.Repeat;
                        downloadedTex.wrapModeV = TextureWrapMode.Clamp;
                        downloadedTex.Apply(true, false);

                        loadedTextures[bodyName] = downloadedTex;
                        ApplyTextureToTarget(bodyName, downloadedTex);
                        Debug.Log($"<color=cyan>[Solar System Scope] Applied high-resolution 2K texture map for {bodyName}!</color>");
                    }
                }
            }
        }

        private void UpdateCollagePlanet()
        {
            if (SolarSystemCollagePlanet.Instance != null)
            {
                Texture2D moon = loadedTextures.ContainsKey("Moon") ? loadedTextures["Moon"] : null;
                Texture2D venus = loadedTextures.ContainsKey("Venus") ? loadedTextures["Venus"] : null;
                Texture2D earth = loadedTextures.ContainsKey("Earth") ? loadedTextures["Earth"] : null;
                Texture2D mars = loadedTextures.ContainsKey("Mars") ? loadedTextures["Mars"] : null;
                Texture2D jupiter = loadedTextures.ContainsKey("Jupiter") ? loadedTextures["Jupiter"] : null;
                Texture2D saturn = loadedTextures.ContainsKey("Saturn") ? loadedTextures["Saturn"] : null;
                Texture2D uranus = loadedTextures.ContainsKey("Uranus") ? loadedTextures["Uranus"] : null;
                Texture2D neptune = loadedTextures.ContainsKey("Neptune") ? loadedTextures["Neptune"] : null;
                Texture2D saturnRing = loadedTextures.ContainsKey("SaturnRings") ? loadedTextures["SaturnRings"] : null;

                SolarSystemCollagePlanet.Instance.ApplyTextures(moon, venus, earth, mars, jupiter, saturn, uranus, neptune, saturnRing);
            }
        }

        private void ApplyTextureToTarget(string bodyName, Texture2D tex)
        {
            if (bodyName == "SaturnRings")
            {
                GameObject ringDisc = GameObject.Find("SaturnRingDisc");
                if (ringDisc != null)
                {
                    Renderer r = ringDisc.GetComponent<Renderer>();
                    if (r != null && r.material != null)
                    {
                        Material mat = r.material;
                        Shader transShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Legacy Shaders/Transparent/Diffuse");
                        if (transShader != null) mat.shader = transShader;
                        mat.mainTexture = tex;
                        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                    }
                }
                return;
            }

            if (bodyName == "MilkyWaySkybox")
            {
                if (RenderSettings.skybox != null)
                {
                    RenderSettings.skybox.mainTexture = tex;
                    if (RenderSettings.skybox.HasProperty("_MainTex")) RenderSettings.skybox.SetTexture("_MainTex", tex);
                    if (RenderSettings.skybox.HasProperty("_BaseMap")) RenderSettings.skybox.SetTexture("_BaseMap", tex);
                }
                return;
            }

            GameObject bodyObj = GameObject.Find(bodyName);
            if (bodyObj != null)
            {
                Renderer r = bodyObj.GetComponent<Renderer>();
                if (r != null && r.material != null)
                {
                    Material mat = r.material;
                    if (tex != null)
                    {
                        mat.mainTexture = tex;
                        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
                    }
                }
            }
        }
    }
}
