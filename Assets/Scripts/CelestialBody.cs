using UnityEngine;

namespace SolarSystemScope
{
    public class CelestialBody : MonoBehaviour
    {
        [Header("Celestial Info")]
        public string bodyName = "Celestial Body";
        public float radiusKm = 1000f;
        public float distanceFromSunAU = 1.0f;
        public string orbitalPeriod = "365 Days";
        public string moonCount = "1";
        public string surfaceTemp = "15°C";
        public float surfaceGravity = 9.81f; // m/s^2
        [TextArea(2, 4)]
        public string description = "A celestial body in the solar system.";

        [Header("Motion Settings")]
        public Transform orbitCenter;
        public float orbitRadius = 10f;
        public float orbitSpeed = 10f; // degrees per second base
        public float selfRotationSpeed = 20f;
        public float axialTilt = 0f;

        [Header("Keplerian Elliptical Orbit Dynamics")]
        public float initialOrbitAngleDegrees = -1f;
        [Range(0f, 0.8f)]
        public float eccentricity = 0.05f;
        public float orbitInclination = 0f;
        public bool useKeplerianSpeedScaling = true;
        public bool isTidallyLocked = false;
        public bool useParentEquatorialPlane = false;
        public Quaternion orbitPlaneRotation = Quaternion.identity;

        [Header("Orbit Visuals")]
        public bool showOrbitLine = true;
        public Color orbitColor = new Color(0.25f, 0.75f, 1.0f, 0.85f);

        private float currentOrbitAngle = 0f;
        private LineRenderer orbitLineRenderer;

        public static float globalTimeScale = 1f;

        private void Start()
        {
            if (initialOrbitAngleDegrees >= 0f) currentOrbitAngle = initialOrbitAngleDegrees;
            else currentOrbitAngle = Random.Range(0f, 360f);

            transform.localRotation = Quaternion.Euler(axialTilt, 0, 0);

            if (orbitCenter != null && orbitRadius > 0.1f && orbitLineRenderer == null && showOrbitLine)
            {
                SetupOrbitLine();
                UpdatePosition(currentOrbitAngle);
            }
        }

        private void Update()
        {
            // Auto setup orbit line if orbitCenter was assigned dynamically after Start()
            if (showOrbitLine && orbitLineRenderer == null && orbitCenter != null && orbitRadius > 0.1f)
            {
                SetupOrbitLine();
            }

            // Self Rotation (unless tidally locked to orbit center)
            if (!isTidallyLocked)
            {
                transform.Rotate(Vector3.up, selfRotationSpeed * globalTimeScale * Time.deltaTime, Space.Self);
            }

            // Orbit Revolution
            if (orbitCenter != null)
            {
                float currentSpeed = orbitSpeed;
                if (useKeplerianSpeedScaling)
                {
                    // Speed is inversely proportional to distance squared (Kepler's Second Law)
                    float rad = currentOrbitAngle * Mathf.Deg2Rad;
                    float currentDistance = CalculateEllipticalRadius(rad);
                    float speedFactor = (orbitRadius / Mathf.Max(0.1f, currentDistance));
                    currentSpeed *= speedFactor * speedFactor;
                }

                currentOrbitAngle += currentSpeed * globalTimeScale * Time.deltaTime;
                if (currentOrbitAngle >= 360f) currentOrbitAngle -= 360f;
                if (currentOrbitAngle < 0f) currentOrbitAngle += 360f;

                UpdatePosition(currentOrbitAngle);

                // Handle Tidal Locking (same face always facing parent planet)
                if (isTidallyLocked)
                {
                    transform.LookAt(orbitCenter);
                }
            }
        }

        private float CalculateEllipticalRadius(float angleRad)
        {
            float semiMajorAxis = orbitRadius;
            float e = eccentricity;
            return semiMajorAxis * (1f - e * e) / (1f + e * Mathf.Cos(angleRad));
        }

        public Vector3 CalculateOrbitPosition(float angleRad)
        {
            float r = CalculateEllipticalRadius(angleRad);
            float x = Mathf.Cos(angleRad) * r;
            float z = Mathf.Sin(angleRad) * r;
            
            Vector3 rawPos = new Vector3(x, 0f, z);
            Quaternion inclinationRot = Quaternion.Euler(orbitInclination, 0f, 0f);
            Vector3 localTilted = inclinationRot * rawPos;

            if (useParentEquatorialPlane && orbitCenter != null)
            {
                return orbitCenter.rotation * localTilted;
            }
            if (orbitPlaneRotation != Quaternion.identity)
            {
                return orbitPlaneRotation * localTilted;
            }

            return localTilted;
        }

        private void UpdatePosition(float angleDegrees)
        {
            if (orbitCenter == null) return;
            float rad = angleDegrees * Mathf.Deg2Rad;
            Vector3 centerPos = orbitCenter.position;
            Vector3 targetPos = centerPos + CalculateOrbitPosition(rad);
            transform.position = targetPos;
        }

        private void SetupOrbitLine()
        {
            GameObject lineObj = new GameObject(name + "_OrbitLine");
            lineObj.transform.SetParent(orbitCenter != null ? orbitCenter.parent : null, true);
            lineObj.transform.position = Vector3.zero;
            lineObj.transform.rotation = Quaternion.identity;
            
            orbitLineRenderer = lineObj.AddComponent<LineRenderer>();
            
            int segments = 240;
            orbitLineRenderer.positionCount = segments + 1;
            orbitLineRenderer.useWorldSpace = true;
            orbitLineRenderer.loop = true;
            
            float lineW = 0.22f;
            orbitLineRenderer.startWidth = lineW;
            orbitLineRenderer.endWidth = lineW;
            
            Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            Material lineMat = new Material(lineShader);
            Color col = orbitColor;
            col.a = 0.70f;
            lineMat.color = col;
            if (lineMat.HasProperty("_BaseColor")) lineMat.SetColor("_BaseColor", col);
            if (lineMat.HasProperty("_Color")) lineMat.SetColor("_Color", col);
            
            if (lineMat.HasProperty("_Surface")) lineMat.SetFloat("_Surface", 1);
            if (lineMat.HasProperty("_Blend")) lineMat.SetFloat("_Blend", 1);
            lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            lineMat.SetInt("_ZWrite", 0);
            lineMat.renderQueue = 3000;

            orbitLineRenderer.material = lineMat;
            UpdateOrbitLinePositions();
        }

        private void LateUpdate()
        {
            if (showOrbitLine && orbitLineRenderer != null && orbitCenter != null)
            {
                UpdateOrbitLinePositions();
            }
        }

        private void UpdateOrbitLinePositions()
        {
            if (orbitLineRenderer == null || orbitCenter == null) return;
            int segments = orbitLineRenderer.positionCount - 1;
            Vector3 centerPos = orbitCenter.position;

            for (int i = 0; i <= segments; i++)
            {
                float rad = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 localPos = CalculateOrbitPosition(rad);
                orbitLineRenderer.SetPosition(i, centerPos + localPos);
            }
        }

        public void SetOrbitLineVisible(bool visible)
        {
            if (orbitLineRenderer != null)
            {
                orbitLineRenderer.enabled = visible;
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (orbitLineRenderer != null)
                {
                    GameObject go = orbitLineRenderer.gameObject;
                    if (go != null)
                    {
                        if (Application.isPlaying) Destroy(go);
                        else DestroyImmediate(go);
                    }
                }
            }
            catch { }
        }
    }
}
