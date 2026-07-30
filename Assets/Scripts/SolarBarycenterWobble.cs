using UnityEngine;

namespace SolarSystemScope
{
    /// <summary>
    /// Simulates the Solar Barycentric Wobble: the Sun's gravitational motion around the Solar System's center of mass,
    /// driven primarily by Jupiter and Saturn's orbital positions.
    /// </summary>
    public class SolarBarycenterWobble : MonoBehaviour
    {
        [Header("Wobble Multipliers")]
        [Tooltip("Strength factor for Jupiter's gravitational pull on the Sun")]
        public float jupiterPullWeight = 0.045f;

        [Tooltip("Strength factor for Saturn's gravitational pull on the Sun")]
        public float saturnPullWeight = 0.018f;

        [Tooltip("Smoothing factor for fluid barycentric movement")]
        public float smoothSpeed = 2.0f;

        private Transform jupiterTransform;
        private Transform saturnTransform;
        private Vector3 targetSunPos = Vector3.zero;

        private void Start()
        {
            FindGasGiants();
        }

        private void Update()
        {
            if (jupiterTransform == null || saturnTransform == null)
            {
                FindGasGiants();
            }

            Vector3 displacement = Vector3.zero;

            if (jupiterTransform != null)
            {
                displacement += (jupiterTransform.position - transform.position) * jupiterPullWeight;
            }

            if (saturnTransform != null)
            {
                displacement += (saturnTransform.position - transform.position) * saturnPullWeight;
            }

            // Offset the Sun's position towards the Solar System Barycenter
            targetSunPos = displacement;
            transform.position = Vector3.Lerp(transform.position, targetSunPos, Time.deltaTime * smoothSpeed);
        }

        private void FindGasGiants()
        {
#pragma warning disable 0618
            foreach (var body in Object.FindObjectsOfType<CelestialBody>())
            {
                if (body.bodyName == "Jupiter") jupiterTransform = body.transform;
                if (body.bodyName == "Saturn") saturnTransform = body.transform;
            }
#pragma warning restore 0618
        }
    }
}
