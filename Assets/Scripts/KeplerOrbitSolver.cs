using UnityEngine;

namespace SolarSystem
{
    /// <summary>
    /// Computes deterministic planetary positions using Keplerian orbital elements.
    /// Eliminates numerical integration drift and supports time-scaling.
    /// </summary>
    [DisallowMultipleComponent]
    public class KeplerOrbitSolver : MonoBehaviour
    {
        [Header("Central Attractor")]
        [Tooltip("The central body (e.g., Sun) around which this object orbits.")]
        public Transform centralBody;

        [Header("Keplerian Orbital Elements")]
        [Tooltip("Semi-major axis 'a' in Unity distance units.")]
        public float semiMajorAxis = 10f;

        [Tooltip("Eccentricity 'e' (0 = circular, 0 < e < 1 = elliptical).")]
        [Range(0f, 0.99f)]
        public float eccentricity = 0.0167f;

        [Tooltip("Inclination 'i' relative to orbital plane (degrees).")]
        public float inclination = 0f;

        [Tooltip("Longitude of Ascending Node 'Ω' (degrees).")]
        public float longitudeOfAscendingNode = 0f;

        [Tooltip("Argument of Periapsis 'ω' (degrees).")]
        public float argumentOfPeriapsis = 0f;

        [Tooltip("Full orbital period in seconds.")]
        public float orbitalPeriodSeconds = 60f;

        [Header("Gizmos & Visualization")]
        public bool drawOrbitPath = true;
        public Color orbitColor = new Color(0.2f, 0.7f, 1f, 0.5f);
        [Range(36, 360)]
        public int orbitSegments = 100;

        private float meanAnomaly = 0f;

        private void Update()
        {
            if (orbitalPeriodSeconds <= 0f) return;

            // Advance Mean Anomaly M = M0 + n * t
            float meanMotion = (2f * Mathf.PI) / orbitalPeriodSeconds;
            meanAnomaly += meanMotion * Time.deltaTime;
            meanAnomaly %= (2f * Mathf.PI);

            Vector3 computedPosition = GetPositionAtMeanAnomaly(meanAnomaly);

            if (centralBody != null)
            {
                computedPosition += centralBody.position;
            }

            transform.position = computedPosition;
        }

        public Vector3 GetPositionAtMeanAnomaly(float M)
        {
            // Solve Kepler's Equation: M = E - e * sin(E) for Eccentric Anomaly E
            float E = SolveKeplerEquation(M, eccentricity);

            // Calculate True Anomaly nu
            float sinE2 = Mathf.Sin(E / 2f);
            float cosE2 = Mathf.Cos(E / 2f);
            float nu = 2f * Mathf.Atan2(Mathf.Sqrt(1f + eccentricity) * sinE2, Mathf.Sqrt(1f - eccentricity) * cosE2);

            // Radius distance r
            float r = semiMajorAxis * (1f - eccentricity * Mathf.Cos(E));

            // 2D position in orbital plane
            Vector3 orbitalPos = new Vector3(r * Mathf.Cos(nu), 0f, r * Mathf.Sin(nu));

            // Apply 3D Keplerian rotations (inclination, LAN, arg of periapsis)
            Quaternion orbitRotation = Quaternion.Euler(inclination, longitudeOfAscendingNode, argumentOfPeriapsis);
            return orbitRotation * orbitalPos;
        }

        private float SolveKeplerEquation(float M, float e)
        {
            float E = M;
            for (int i = 0; i < 12; i++) // Newton-Raphson numerical solver
            {
                float f = E - e * Mathf.Sin(E) - M;
                float fPrime = 1f - e * Mathf.Cos(E);
                E -= f / fPrime;
            }
            return E;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawOrbitPath || semiMajorAxis <= 0f) return;

            Gizmos.color = orbitColor;
            Vector3 center = centralBody != null ? centralBody.position : Vector3.zero;

            Vector3 prevPoint = center + GetPositionAtMeanAnomaly(0f);
            for (int i = 1; i <= orbitSegments; i++)
            {
                float M = (i / (float)orbitSegments) * 2f * Mathf.PI;
                Vector3 currentPoint = center + GetPositionAtMeanAnomaly(M);
                Gizmos.DrawLine(prevPoint, currentPoint);
                prevPoint = currentPoint;
            }
        }
    }
}
