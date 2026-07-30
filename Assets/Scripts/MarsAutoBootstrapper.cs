using UnityEngine;
using UnityEngine.UI;
using MarsExplorer;

namespace MarsExplorer
{
    public class MarsAutoBootstrapper : MonoBehaviour
    {
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoSetupMarsOnPlay()
        {
            // Yield if SolarSystemRoot is present or if single Mars scene is replaced by full Solar System
            if (GameObject.Find("SolarSystemRoot") != null) return;
            if (GameObject.Find("Mars Planet") != null) return;


            Debug.Log("<color=orange>[NASA Mars Showcase Bootstrapper] Initializing 3D Planet...</color>");

            // 1. Create Mars Planet Sphere
            GameObject marsObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marsObj.name = "Mars Planet";
            marsObj.transform.position = Vector3.zero;
            marsObj.transform.localScale = new Vector3(5f, 5f, 5f);

            Renderer marsRenderer = marsObj.GetComponent<Renderer>();
            Material marsMat = new Material(Shader.Find("Standard"));
            marsMat.color = new Color(0.85f, 0.42f, 0.2f);
            marsMat.SetFloat("_Glossiness", 0.15f);
            marsRenderer.material = marsMat;

            // Attach Scripts (Rotator with mouse drag, Procedural texture, Landmark Manager)
            PlanetRotator rotator = marsObj.AddComponent<PlanetRotator>();
            rotator.allowDirectDrag = true;
            rotator.dragSensitivity = 0.4f;

            ProceduralMarsTexture procTex = marsObj.AddComponent<ProceduralMarsTexture>();
            LandmarkManager landmarkMgr = marsObj.AddComponent<LandmarkManager>();

            // 2. Create Atmosphere Glow Sphere
            GameObject atmosObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            atmosObj.name = "Atmosphere Glow";
            atmosObj.transform.SetParent(marsObj.transform, false);
            atmosObj.transform.localScale = Vector3.one * 1.04f;
            SafeDestroy(atmosObj.GetComponent<Collider>());

            Renderer atmosRenderer = atmosObj.GetComponent<Renderer>();
            Material atmosMat = new Material(Shader.Find("Unlit/Color"));
            atmosMat.color = new Color(0.85f, 0.35f, 0.15f, 0.25f);
            atmosRenderer.material = atmosMat;

            // 3. Create Moons (Phobos & Deimos)
            GameObject phobos = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            phobos.name = "Phobos";
            phobos.transform.localScale = new Vector3(0.3f, 0.25f, 0.2f);
            OrbitingMoon phobosOrbit = phobos.AddComponent<OrbitingMoon>();
            phobosOrbit.planetCenter = marsObj.transform;
            phobosOrbit.orbitDistance = 6.5f;

            GameObject deimos = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            deimos.name = "Deimos";
            deimos.transform.localScale = new Vector3(0.18f, 0.16f, 0.14f);
            OrbitingMoon deimosOrbit = deimos.AddComponent<OrbitingMoon>();
            deimosOrbit.planetCenter = marsObj.transform;
            deimosOrbit.orbitDistance = 10.5f;

            // 4. Setup Camera & Orbit Controls
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
            }
            mainCam.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
            mainCam.transform.position = new Vector3(0, 2, -12);
            mainCam.transform.LookAt(marsObj.transform);

            PlanetCameraController camCtrl = mainCam.gameObject.GetComponent<PlanetCameraController>();
            if (camCtrl == null) camCtrl = mainCam.gameObject.AddComponent<PlanetCameraController>();
            camCtrl.target = marsObj.transform;
            camCtrl.distance = 12f;

            // 5. Setup Directional Sun Light
#pragma warning disable 0618
            Light sunLight = UnityEngine.Object.FindObjectOfType<Light>();
#pragma warning restore 0618
            if (sunLight == null)
            {
                GameObject lightObj = new GameObject("Sun Light");
                sunLight = lightObj.AddComponent<Light>();
                sunLight.type = LightType.Directional;
            }
            sunLight.transform.rotation = Quaternion.Euler(20, -35, 0);
            sunLight.intensity = 1.6f;
            sunLight.color = new Color(1f, 0.96f, 0.92f);

            // 6. Setup Deep Space Starfield Particles
            GameObject starfieldObj = new GameObject("NASA Starfield");
            ParticleSystem ps = starfieldObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1000f;
            main.startSpeed = 0f;
            main.startSize = 0.06f;
            main.maxParticles = 2000;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 150f;
            var emit = ps.emission;
            emit.rateOverTime = 0;
            ps.Emit(1500);

            Debug.Log("<color=lime>[NASA Mars Showcase Ready] Drag mouse to rotate planet! Scroll to zoom.</color>");
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
