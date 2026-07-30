using UnityEngine;

namespace SolarSystemScope
{
    /// <summary>
    /// Generates glowing 3D Helical/Spiral Corkscrew Trajectory Trails for planets as they move through 3D space.
    /// Simulates the galactic forward flow motion of the Solar System.
    /// </summary>
    public class HelicalTrajectoryTrail : MonoBehaviour
    {
        [Header("Trail Visual Settings")]
        [Tooltip("Lifetime of the helical trail in seconds")]
        public float trailTime = 12f;

        [Tooltip("Starting width of the trail")]
        public float startWidth = 0.35f;

        [Tooltip("Ending width of the trail")]
        public float endWidth = 0.05f;

        [Tooltip("Color tint for the helical spiral trail")]
        public Color trailColor = new Color(0.15f, 0.75f, 1.0f, 0.8f);

        [Header("Galactic Motion Simulation")]
        [Tooltip("Simulate the Solar System's forward movement through space to trace out 3D helical spirals")]
        public bool enableGalacticForwardMotion = false;

        [Tooltip("Forward velocity through space")]
        public Vector3 galacticVelocity = new Vector3(0f, 0f, 4f);

        private TrailRenderer trailRenderer;
        private CelestialBody celestialBody;

        private void Start()
        {
            celestialBody = GetComponent<CelestialBody>();
            SetupTrail();
        }

        private void Update()
        {
            // If Galactic Forward Motion is enabled, move the Solar System forward in space to trace helical spirals
            if (enableGalacticForwardMotion && transform.parent != null)
            {
                transform.parent.position += galacticVelocity * Time.deltaTime;
            }
        }

        private void SetupTrail()
        {
            GameObject trailObj = new GameObject(name + "_HelicalTrail");
            trailObj.transform.SetParent(transform, false);
            trailObj.transform.localPosition = Vector3.zero;

            trailRenderer = trailObj.AddComponent<TrailRenderer>();
            trailRenderer.time = trailTime;
            trailRenderer.startWidth = (startWidth != 0.35f) ? startWidth : ((celestialBody != null && celestialBody.orbitRadius > 0f) ? Mathf.Clamp(celestialBody.orbitRadius * 0.0018f, 0.08f, 0.25f) : startWidth);
            trailRenderer.endWidth = endWidth;
            trailRenderer.autodestruct = false;

            Shader trailShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            Material trailMat = new Material(trailShader);
            
            Color col = (celestialBody != null) ? celestialBody.orbitColor : trailColor;
            col.a = 0.75f;
            trailMat.color = col;
            if (trailMat.HasProperty("_BaseColor")) trailMat.SetColor("_BaseColor", col);
            if (trailMat.HasProperty("_Color")) trailMat.SetColor("_Color", col);

            if (trailMat.HasProperty("_Surface")) trailMat.SetFloat("_Surface", 1);
            if (trailMat.HasProperty("_Blend")) trailMat.SetFloat("_Blend", 1);
            trailMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            trailMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            trailMat.SetInt("_ZWrite", 0);
            trailMat.renderQueue = 3000;

            // Configure gradient fade
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(col, 0.0f), new GradientColorKey(col, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            trailRenderer.colorGradient = gradient;
            trailRenderer.material = trailMat;
            trailRenderer.Clear();
        }

        public void SetTrailTime(float timeSeconds)
        {
            if (trailRenderer != null)
            {
                trailRenderer.time = timeSeconds;
            }
        }

        public void ToggleGalacticMotion()
        {
            enableGalacticForwardMotion = !enableGalacticForwardMotion;
        }
    }
}
