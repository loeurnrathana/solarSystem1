using UnityEngine;
using System.Collections.Generic;

namespace SolarSystem
{
    [DisallowMultipleComponent]
    public class NBodyBody : MonoBehaviour
    {
        [Tooltip("Mass of the body for gravitational calculations.")]
        public float mass = 100f;

        [Tooltip("Initial velocity vector.")]
        public Vector3 initialVelocity;

        [HideInInspector]
        public Vector3 currentVelocity;

        [HideInInspector]
        public Vector3 currentAcceleration;

        private void Awake()
        {
            currentVelocity = initialVelocity;
        }
    }

    /// <summary>
    /// N-Body gravitational simulator using Velocity-Verlet integration.
    /// Provides physical energy preservation for multi-body orbital mechanics.
    /// </summary>
    public class NBodySimulation : MonoBehaviour
    {
        [Header("Simulation Parameters")]
        [Tooltip("Gravitational constant scalar.")]
        public float gravitationalConstant = 1.0f;

        [Tooltip("Softening factor to prevent infinite forces during close encounters.")]
        public float softeningLength = 0.1f;

        private List<NBodyBody> bodies = new List<NBodyBody>();

        private void Start()
        {
            RefreshBodiesList();
        }

        public void RefreshBodiesList()
        {
            bodies.Clear();
            bodies.AddRange(FindObjectsByType<NBodyBody>(FindObjectsSortMode.None));
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // Step 1: Position update (Velocity-Verlet half-step)
            foreach (var b in bodies)
            {
                if (b == null || !b.enabled) continue;
                b.transform.position += b.currentVelocity * dt + 0.5f * b.currentAcceleration * dt * dt;
            }

            // Step 2: Calculate new gravitational accelerations
            Vector3[] newAccelerations = new Vector3[bodies.Count];
            for (int i = 0; i < bodies.Count; i++)
            {
                if (bodies[i] == null || !bodies[i].enabled) continue;

                for (int j = i + 1; j < bodies.Count; j++)
                {
                    if (bodies[j] == null || !bodies[j].enabled) continue;

                    Vector3 dir = bodies[j].transform.position - bodies[i].transform.position;
                    float distSqr = dir.sqrMagnitude + (softeningLength * softeningLength);
                    float forceMag = gravitationalConstant * (bodies[i].mass * bodies[j].mass) / distSqr;
                    Vector3 force = dir.normalized * forceMag;

                    newAccelerations[i] += force / bodies[i].mass;
                    newAccelerations[j] -= force / bodies[j].mass;
                }
            }

            // Step 3: Velocity update
            for (int i = 0; i < bodies.Count; i++)
            {
                if (bodies[i] == null || !bodies[i].enabled) continue;

                bodies[i].currentVelocity += 0.5f * (bodies[i].currentAcceleration + newAccelerations[i]) * dt;
                bodies[i].currentAcceleration = newAccelerations[i];
            }
        }
    }
}
