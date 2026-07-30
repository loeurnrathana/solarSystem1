using UnityEngine;
using System.Collections.Generic;

namespace SolarSystemScope
{
    /// <summary>
    /// Implements the complete 3D Helical Vortex Orbit System:
    /// 1. Moves the Solar System forward in 3D space.
    /// 2. Pre-generates and animates long 3D spiral corkscrew ribbon trails for every planet immediately on play.
    /// </summary>
    public class GalacticVortexMotion : MonoBehaviour
    {
        [Header("Vortex Motion Toggle")]
        [Tooltip("Enable 3D Helical Corkscrew Vortex Trajectory Trails")]
        public bool enableGalacticVortexTrails = false;

        [Header("Galactic Forward Velocity")]
        public Vector3 forwardVelocity = new Vector3(0f, 2f, 4f);

        [Header("Spiral Trail Settings")]
        public float trailLengthInSeconds = 25f;
        public int trailPointsCount = 200;

        private List<CelestialBody> planetBodies = new List<CelestialBody>();
        private Dictionary<CelestialBody, LineRenderer> spiralLineRenderers = new Dictionary<CelestialBody, LineRenderer>();

        private void Start()
        {
            FindCelestialBodies();
        }

        public void FindCelestialBodies()
        {
            planetBodies.Clear();
#pragma warning disable 0618
            foreach (var body in Object.FindObjectsOfType<CelestialBody>())
            {
                if (body.bodyName != "Sun" && body.bodyName != "Asteroid Belt")
                {
                    planetBodies.Add(body);
                    if (!spiralLineRenderers.ContainsKey(body))
                    {
                        SetupSpiralTrailForPlanet(body);
                    }
                }
            }
#pragma warning restore 0618
        }

        private void Update()
        {
            if (enableGalacticVortexTrails && forwardVelocity.sqrMagnitude > 0.001f)
            {
                transform.position += forwardVelocity * Time.deltaTime;
            }

            foreach (var body in planetBodies)
            {
                if (body == null || !spiralLineRenderers.ContainsKey(body)) continue;

                LineRenderer line = spiralLineRenderers[body];
                if (line == null) continue;

                line.enabled = enableGalacticVortexTrails;

                if (enableGalacticVortexTrails)
                {
                    UpdateSpiralTrail(body, line);
                }
            }
        }

        private void SetupSpiralTrailForPlanet(CelestialBody body)
        {
            GameObject trailObj = new GameObject(body.name + "_HelicalVortexTrail");
            trailObj.transform.SetParent(transform, false);

            LineRenderer line = trailObj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = trailPointsCount;
            line.enabled = enableGalacticVortexTrails;

            float lineW = Mathf.Clamp(body.orbitRadius * 0.0018f, 0.08f, 0.25f);
            line.startWidth = lineW;
            line.endWidth = 0.01f;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            Material mat = new Material(shader);

            Color col = body.orbitColor;
            col.a = 0.75f;
            mat.color = col;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 1);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(col, 0.0f), new GradientColorKey(col, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            line.colorGradient = gradient;
            line.material = mat;

            spiralLineRenderers[body] = line;
        }

        private void UpdateSpiralTrail(CelestialBody body, LineRenderer line)
        {
            if (body == null || line == null || body.orbitCenter == null) return;

            Vector3 currentPlanetPos = body.transform.position;
            Vector3 centerPos = body.orbitCenter.position;

            float dtStep = trailLengthInSeconds / trailPointsCount;
            float currentOrbitAngleRad = Mathf.Atan2(currentPlanetPos.z - centerPos.z, currentPlanetPos.x - centerPos.x);

            for (int i = 0; i < trailPointsCount; i++)
            {
                float tBack = i * dtStep;
                float pastAngle = currentOrbitAngleRad - (body.orbitSpeed * Mathf.Deg2Rad * CelestialBody.globalTimeScale * tBack);

                Vector3 localOrbitPos = body.CalculateOrbitPosition(pastAngle);
                Vector3 pastCenterPos = centerPos - forwardVelocity * tBack;
                Vector3 pointPos = pastCenterPos + localOrbitPos;

                line.SetPosition(i, pointPos);
            }
        }

        public void SetVortexMotionEnabled(bool enabled)
        {
            enableGalacticVortexTrails = enabled;
            foreach (var kvp in spiralLineRenderers)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.enabled = enabled;
                }
            }
        }
    }
}
