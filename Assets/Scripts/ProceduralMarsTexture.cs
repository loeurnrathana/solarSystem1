using UnityEngine;

namespace MarsExplorer
{
    public class ProceduralMarsTexture : MonoBehaviour
    {
        [Header("Texture Resolution")]
        public int textureWidth = 1024;
        public int textureHeight = 512;

        [Header("Generated Textures")]
        public Texture2D diffuseTexture;
        public Texture2D elevationTexture;
        public Texture2D thermalTexture;
        public Texture2D bumpTexture;

        public enum RenderMode { Realistic, Topography, Thermal, Wireframe }
        public RenderMode currentMode = RenderMode.Realistic;

        private Renderer planetRenderer;
        private Material planetMaterial;

        private void Awake()
        {
            planetRenderer = GetComponent<Renderer>();
            if (planetRenderer != null)
            {
                planetMaterial = planetRenderer.material;
            }
            GenerateAllTextures();
        }

        public void GenerateAllTextures()
        {
            if (diffuseTexture != null) DestroyTexture(diffuseTexture);
            if (elevationTexture != null) DestroyTexture(elevationTexture);
            if (thermalTexture != null) DestroyTexture(thermalTexture);
            if (bumpTexture != null) DestroyTexture(bumpTexture);

            diffuseTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, true);
            elevationTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, true);
            thermalTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, true);
            bumpTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, true);

            Color rustBase = new Color(0.76f, 0.33f, 0.16f);
            Color rustBright = new Color(0.86f, 0.47f, 0.25f);
            Color basaltDark = new Color(0.25f, 0.18f, 0.15f);
            Color iceWhite = new Color(0.94f, 0.96f, 0.98f);
            Color canyonColor = new Color(0.16f, 0.10f, 0.08f);

            Color[] diffPixels = new Color[textureWidth * textureHeight];
            Color[] elevPixels = new Color[textureWidth * textureHeight];
            Color[] thermPixels = new Color[textureWidth * textureHeight];
            Color[] bumpPixels = new Color[textureWidth * textureHeight];

            float seed = 42.1337f;

            for (int y = 0; y < textureHeight; y++)
            {
                float v = (float)y / textureHeight;
                float lat = (v - 0.5f) * Mathf.PI; // -PI/2 to PI/2
                float absLat = Mathf.Abs(lat);

                for (int x = 0; x < textureWidth; x++)
                {
                    float u = (float)x / textureWidth;
                    float lon = (u - 0.5f) * 2f * Mathf.PI; // -PI to PI
                    int idx = y * textureWidth + x;

                    // Spherical 3D mapping for seamless wrap
                    float nx = Mathf.Cos(lat) * Mathf.Cos(lon);
                    float ny = Mathf.Cos(lat) * Mathf.Sin(lon);
                    float nz = Mathf.Sin(lat);

                    // Multi-scale Perlin Noise
                    float n1 = Mathf.PerlinNoise(nx * 2f + seed, ny * 2f + seed);
                    float n2 = Mathf.PerlinNoise(nx * 5f + seed * 2f, ny * 5f + seed * 2f);
                    float nDetail = Mathf.PerlinNoise(nx * 12f + seed * 3f, ny * 12f + seed * 3f);

                    float elev = (n1 * 0.5f + n2 * 0.3f + nDetail * 0.2f);

                    // 1. Olympus Mons (Shield volcano around lat 18.65°N, lon 226.2°E -> ~-1.63 rad)
                    float olympusLat = 18.65f * Mathf.Deg2Rad;
                    float olympusLon = (226.2f - 180f) * Mathf.Deg2Rad;
                    float dOlympus = Mathf.Sqrt(Mathf.Pow(lat - olympusLat, 2) + Mathf.Pow(lon - olympusLon, 2));
                    float olympusHeight = 0f;
                    if (dOlympus < 0.25f)
                    {
                        olympusHeight = Mathf.Cos((dOlympus / 0.25f) * (Mathf.PI / 2f)) * 0.7f;
                    }

                    // 2. Valles Marineris canyon rift
                    float canyonLat = -13.9f * Mathf.Deg2Rad;
                    float canyonLon = (300.8f - 180f) * Mathf.Deg2Rad;
                    float canyonDepth = 0f;
                    if (Mathf.Abs(lat - canyonLat) < 0.15f && Mathf.Abs(lon - canyonLon) < 0.6f)
                    {
                        float lineDist = Mathf.Abs(lat - canyonLat + Mathf.Sin((lon - canyonLon) * 3f) * 0.04f);
                        if (lineDist < 0.08f)
                        {
                            canyonDepth = (1.0f - (lineDist / 0.08f)) * 0.6f;
                        }
                    }

                    float finalElev = Mathf.Clamp01(elev + olympusHeight - canyonDepth);

                    // Polar Ice Cap
                    float iceFactor = 0f;
                    float iceBound = Mathf.PI / 2f - 0.28f;
                    if (absLat > iceBound)
                    {
                        iceFactor = (absLat - iceBound) / (Mathf.PI / 2f - iceBound);
                        iceFactor = Mathf.Clamp01(iceFactor + (n2 - 0.5f) * 0.3f);
                    }

                    // Diffuse Color
                    Color diffColor;
                    if (iceFactor > 0.4f)
                    {
                        diffColor = Color.Lerp(rustBright, iceWhite, iceFactor);
                    }
                    else if (canyonDepth > 0.2f)
                    {
                        diffColor = canyonColor;
                    }
                    else if (finalElev < 0.38f)
                    {
                        diffColor = Color.Lerp(basaltDark, rustBase, finalElev / 0.38f);
                    }
                    else
                    {
                        diffColor = Color.Lerp(rustBase, rustBright, (finalElev - 0.38f) / 0.62f);
                    }

                    diffPixels[idx] = diffColor;

                    // Elevation Map (False Color: Blue lowland -> Green -> Red -> White highland)
                    Color elevColor;
                    if (finalElev < 0.3f) elevColor = Color.Lerp(Color.blue, Color.cyan, finalElev / 0.3f);
                    else if (finalElev < 0.6f) elevColor = Color.Lerp(Color.cyan, Color.yellow, (finalElev - 0.3f) / 0.3f);
                    else if (finalElev < 0.85f) elevColor = Color.Lerp(Color.yellow, Color.red, (finalElev - 0.6f) / 0.25f);
                    else elevColor = Color.Lerp(Color.red, Color.white, (finalElev - 0.85f) / 0.15f);

                    elevPixels[idx] = elevColor;

                    // Thermal Night IR Map
                    float thermVal = (1.0f - finalElev) * 0.7f + (1.0f - iceFactor) * 0.3f;
                    thermPixels[idx] = new Color(thermVal * 0.9f, thermVal * 0.2f, thermVal * 0.1f, 1f);

                    // Bump Map (Gray)
                    bumpPixels[idx] = new Color(finalElev, finalElev, finalElev, 1f);
                }
            }

            diffuseTexture.SetPixels(diffPixels); diffuseTexture.Apply();
            elevationTexture.SetPixels(elevPixels); elevationTexture.Apply();
            thermalTexture.SetPixels(thermPixels); thermalTexture.Apply();
            bumpTexture.SetPixels(bumpPixels); bumpTexture.Apply();

            ApplyMode(currentMode);
        }

        public void ApplyMode(RenderMode mode)
        {
            currentMode = mode;
            if (planetMaterial == null) return;

            switch (mode)
            {
                case RenderMode.Realistic:
                    planetMaterial.mainTexture = diffuseTexture;
                    planetMaterial.color = Color.white;
                    break;
                case RenderMode.Topography:
                    planetMaterial.mainTexture = elevationTexture;
                    planetMaterial.color = Color.white;
                    break;
                case RenderMode.Thermal:
                    planetMaterial.mainTexture = thermalTexture;
                    planetMaterial.color = Color.white;
                    break;
                case RenderMode.Wireframe:
                    planetMaterial.mainTexture = diffuseTexture;
                    planetMaterial.color = new Color(0.2f, 1f, 0.4f);
                    break;
            }
        }

        private void DestroyTexture(Texture2D tex)
        {
            if (tex == null) return;
            if (Application.isPlaying) Destroy(tex);
            else DestroyImmediate(tex);
        }
    }
}
