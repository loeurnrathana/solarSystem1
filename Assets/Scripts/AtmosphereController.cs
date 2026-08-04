using UnityEngine;

namespace SolarSystemScope
{
    [ExecuteAlways]
    public class AtmosphereController : MonoBehaviour
    {
        [Header("Lighting Settings")]
        public Transform sunTransform;
        public Color atmosphereColor = new Color(0.3f, 0.6f, 1.0f, 1.0f);
        public Vector4 rayleighCoefficients = new Vector4(0.15f, 0.45f, 1.0f, 1.0f);
        [Range(0.5f, 10.0f)] public float densityFalloff = 3.5f;
        [Range(0.1f, 10.0f)] public float glowIntensity = 2.5f;

        private Material mat;
        private static readonly int SunDirProperty = Shader.PropertyToID("_SunDir");
        private static readonly int AtmosColorProperty = Shader.PropertyToID("_AtmosphereColor");
        private static readonly int RayleighProperty = Shader.PropertyToID("_RayleighCoeff");
        private static readonly int FalloffProperty = Shader.PropertyToID("_DensityFalloff");
        private static readonly int IntensityProperty = Shader.PropertyToID("_GlowIntensity");

        private void Start()
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                mat = r.material;
            }

            if (sunTransform == null)
            {
                GameObject sunObj = GameObject.Find("Sun");
                if (sunObj != null) sunTransform = sunObj.transform;
            }
        }

        private void Update()
        {
            if (mat == null) return;

            Vector3 sunDir = Vector3.forward;
            if (sunTransform != null)
            {
                sunDir = (transform.position - sunTransform.position).normalized;
            }

            mat.SetVector(SunDirProperty, new Vector4(sunDir.x, sunDir.y, sunDir.z, 0f));
            mat.SetColor(AtmosColorProperty, atmosphereColor);
            mat.SetVector(RayleighProperty, rayleighCoefficients);
            mat.SetFloat(FalloffProperty, densityFalloff);
            mat.SetFloat(IntensityProperty, glowIntensity);
        }
    }
}
