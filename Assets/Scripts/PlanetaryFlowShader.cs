using UnityEngine;

namespace MarsExplorer
{
    /// <summary>
    /// Simulates dynamic atmospheric, cloud, and fluid surface flow motion on 3D planets using UV vector animation and procedural flow noise.
    /// </summary>
    public class PlanetaryFlowShader : MonoBehaviour
    {
        [Header("Flow Animation Settings")]
        [Tooltip("Horizontal flow velocity (U axis)")]
        public float flowSpeedU = 0.015f;

        [Tooltip("Vertical flow velocity (V axis)")]
        public float flowSpeedV = 0.005f;

        [Tooltip("Turbulence / swirling frequency")]
        public float turbulenceSpeed = 0.5f;

        [Header("Atmosphere Cloud Layers")]
        public bool animateCloudLayer = true;
        public Vector2 cloudFlowVelocity = new Vector2(0.02f, 0.008f);

        [Header("Dual Phase Flow Blending")]
        public bool useDualPhaseBlending = true;
        public float cycleDuration = 4.0f;

        private Renderer targetRenderer;
        private Material planetMaterial;

        private Vector2 currentUvOffset = Vector2.zero;
        private float phase0 = 0f;
        private float phase1 = 0f;

        private static readonly int MainTexOffsetProperty = Shader.PropertyToID("_MainTex");
        private static readonly int BaseMapOffsetProperty = Shader.PropertyToID("_BaseMap");
        private static readonly int CloudOffsetProperty = Shader.PropertyToID("_CloudOffset");

        private void Awake()
        {
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                planetMaterial = targetRenderer.material;
            }
        }

        private void Update()
        {
            if (planetMaterial == null) return;

            float dt = Time.deltaTime;

            // 1. Primary texture flow offset
            if (flowSpeedU != 0f || flowSpeedV != 0f)
            {
                currentUvOffset.x += flowSpeedU * dt;
                currentUvOffset.y += flowSpeedV * dt;

                // Keep UV offsets within [0, 1] bounds to prevent floating point overflow
                currentUvOffset.x %= 1.0f;
                currentUvOffset.y %= 1.0f;

                if (planetMaterial.HasProperty("_BaseMap"))
                {
                    planetMaterial.SetTextureOffset("_BaseMap", currentUvOffset);
                }
                else if (planetMaterial.HasProperty("_MainTex"))
                {
                    planetMaterial.SetTextureOffset("_MainTex", currentUvOffset);
                }
            }

            // 2. Dual Phase Swirl / Turbulence animation
            if (useDualPhaseBlending)
            {
                float time = Time.time * turbulenceSpeed;
                phase0 = (time / cycleDuration) % 1.0f;
                phase1 = ((time / cycleDuration) + 0.5f) % 1.0f;

                float blend = Mathf.Abs((phase0 - 0.5f) * 2.0f);

                if (planetMaterial.HasProperty("_FlowBlend"))
                {
                    planetMaterial.SetFloat("_FlowBlend", blend);
                }
            }

            // 3. Atmosphere / Cloud Layer flow
            if (animateCloudLayer)
            {
                Vector2 currentOffset = planetMaterial.HasProperty("_BaseMap") ? planetMaterial.GetTextureOffset("_BaseMap") : (planetMaterial.HasProperty("_MainTex") ? planetMaterial.GetTextureOffset("_MainTex") : Vector2.zero);
                Vector2 cloudOffset = currentOffset + cloudFlowVelocity * dt;
                if (planetMaterial.HasProperty("_CloudOffset"))
                {
                    planetMaterial.SetVector("_CloudOffset", cloudOffset);
                }
            }
        }
    }
}
