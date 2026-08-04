using UnityEngine;

namespace SolarSystemScope
{
    public class SolarSystemCollagePlanet : MonoBehaviour
    {
        public static SolarSystemCollagePlanet Instance { get; private set; }

        [Header("Rotation Settings")]
        public float rotationSpeed = 6.0f;

        private GameObject collageSphere;
        private GameObject ringSegment;
        private Material collageMaterial;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public void CreateCollagePlanet(Vector3 position, float diameter = 8.0f)
        {
            gameObject.name = "TrueColorSolarSystemCollageObject";
            transform.position = position;

            // 1. Create main collage sphere
            collageSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            collageSphere.name = "TrueColorCollageSphere";
            collageSphere.transform.SetParent(transform, false);
            collageSphere.transform.localScale = Vector3.one * diameter;

            Shader collageShader = Shader.Find("SolarSystem/SolarSystemCollage") ?? Shader.Find("Unlit/Texture");
            collageMaterial = new Material(collageShader);
            
            Renderer r = collageSphere.GetComponent<Renderer>();
            if (r != null) r.material = collageMaterial;

            // 2. Create Saturn Ring extension segment on the right side
            CreateSaturnRingExtension(diameter);

            // 3. Create 3D Title Header canvas text
            CreateTitleHeader(diameter);
        }

        private void CreateSaturnRingExtension(float planetDiameter)
        {
            ringSegment = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ringSegment.name = "SaturnRingExtension";
            ringSegment.transform.SetParent(transform, false);

            // Position at the Saturn slice band (approx Y = -diameter * 0.12) extending to the right
            float radius = planetDiameter * 0.5f;
            ringSegment.transform.localPosition = new Vector3(radius * 1.15f, -radius * 0.25f, 0f);
            ringSegment.transform.localScale = new Vector3(radius * 0.8f, radius * 0.35f, 1f);
            ringSegment.transform.localRotation = Quaternion.Euler(15f, 0f, -12f);

            Destroy(ringSegment.GetComponent<Collider>());

            Renderer r = ringSegment.GetComponent<Renderer>();
            if (r != null)
            {
                Shader ringShader = Shader.Find("SolarSystem/SaturnCollageRing") ?? Shader.Find("Unlit/Transparent");
                r.material = new Material(ringShader);
            }
        }

        private void CreateTitleHeader(float planetDiameter)
        {
            GameObject headerObj = new GameObject("CollageTitleHeader");
            headerObj.transform.SetParent(transform, false);
            float radius = planetDiameter * 0.5f;
            headerObj.transform.localPosition = new Vector3(0f, radius + 1.2f, 0f);

            TextMesh tm = headerObj.AddComponent<TextMesh>();
            tm.text = "True-Color solar system collage";
            tm.fontSize = 48;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            tm.fontStyle = FontStyle.Bold;
        }

        public void ApplyTextures(Texture2D moon, Texture2D venus, Texture2D earth, Texture2D mars, Texture2D jupiter, Texture2D saturn, Texture2D uranus, Texture2D neptune, Texture2D saturnRing)
        {
            if (collageMaterial == null) return;

            if (moon != null) collageMaterial.SetTexture("_TexMoon", moon);
            if (venus != null) collageMaterial.SetTexture("_TexVenus", venus);
            if (earth != null) collageMaterial.SetTexture("_TexEarth", earth);
            if (mars != null) collageMaterial.SetTexture("_TexMars", mars);
            if (jupiter != null) collageMaterial.SetTexture("_TexJupiter", jupiter);
            if (saturn != null) collageMaterial.SetTexture("_TexSaturn", saturn);
            if (uranus != null) collageMaterial.SetTexture("_TexUranus", uranus);
            if (neptune != null) collageMaterial.SetTexture("_TexNeptune", neptune);
            if (moon != null) collageMaterial.SetTexture("_TexPluto", moon); // Pluto fallback

            if (ringSegment != null && saturnRing != null)
            {
                Renderer r = ringSegment.GetComponent<Renderer>();
                if (r != null && r.material != null)
                {
                    r.material.mainTexture = saturnRing;
                }
            }
        }

        private void Update()
        {
            if (collageSphere != null)
            {
                collageSphere.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
        }
    }
}
