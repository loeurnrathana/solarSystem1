using UnityEngine;

namespace SolarSystemScope
{
    public class AsteroidBelt : MonoBehaviour
    {
        public int asteroidCount = 500;
        public float innerRadius = 66f;
        public float outerRadius = 74f;
        public float beltHeight = 3.5f;

        private void Start()
        {
            GameObject beltRoot = new GameObject("AsteroidBeltRoot");
            beltRoot.transform.SetParent(transform, false);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Mobile/Diffuse");
            Material asteroidMat = new Material(litShader);
            asteroidMat.color = new Color(0.45f, 0.42f, 0.40f);
            if (asteroidMat.HasProperty("_BaseColor")) asteroidMat.SetColor("_BaseColor", new Color(0.45f, 0.42f, 0.40f));
            if (asteroidMat.HasProperty("_Color")) asteroidMat.SetColor("_Color", new Color(0.45f, 0.42f, 0.40f));

            // Create Asteroid Belt Rotator
            CelestialBody beltBody = beltRoot.AddComponent<CelestialBody>();
            beltBody.bodyName = "Asteroid Belt";
            beltBody.showOrbitLine = false;
            beltBody.selfRotationSpeed = 1.8f;

            for (int i = 0; i < asteroidCount; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(innerRadius, outerRadius);
                float yOffset = Random.Range(-beltHeight, beltHeight);

                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, yOffset, Mathf.Sin(angle) * radius);

                GameObject ast = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ast.name = "Asteroid_" + i;
                ast.transform.SetParent(beltRoot.transform, false);
                ast.transform.localPosition = pos;

                // Deform scale to give irregular rocky shape
                float scaleX = Random.Range(0.2f, 0.6f);
                float scaleY = Random.Range(0.18f, 0.5f);
                float scaleZ = Random.Range(0.2f, 0.6f);
                ast.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                ast.transform.localRotation = Random.rotation;

                SafeDestroy(ast.GetComponent<Collider>());
                ast.GetComponent<Renderer>().material = asteroidMat;
            }
        }

        private void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
