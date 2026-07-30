using UnityEngine;

namespace MarsExplorer
{
    public class OrbitingMoon : MonoBehaviour
    {
        [Header("Orbit Config")]
        public Transform planetCenter;
        public float orbitDistance = 6.5f;
        public float orbitSpeed = 25f;
        public float selfRotationSpeed = 40f;
        public Vector3 orbitAxis = Vector3.up;

        private float currentAngle = 0f;

        private void Start()
        {
            if (planetCenter == null)
            {
                planetCenter = transform.parent;
            }
            currentAngle = Random.Range(0f, 360f);
        }

        private void Update()
        {
            if (planetCenter == null) return;

            currentAngle += orbitSpeed * Time.deltaTime;
            Quaternion rot = Quaternion.AngleAxis(currentAngle, orbitAxis);
            Vector3 pos = planetCenter.position + rot * (Vector3.forward * orbitDistance);

            transform.position = pos;
            transform.Rotate(Vector3.up, selfRotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
