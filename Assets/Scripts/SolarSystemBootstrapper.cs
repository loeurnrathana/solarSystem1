using UnityEngine;
using System.Collections.Generic;

namespace SolarSystemScope
{
    public class SolarSystemBootstrapper : MonoBehaviour
    {
        public static GameObject SolarSystemRootInstance;
        public static Camera SpaceCameraInstance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoSetupSolarSystemOnPlay()
        {
            if (GameObject.Find("SolarSystemRoot") != null) return;

            // Configure 4K Ultra HD Graphics Quality Settings
            QualitySettings.globalTextureMipmapLimit = 0; // Full 4K uncompressed texture resolution
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.antiAliasing = 8; // 8x MSAA Anti-Aliasing
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowDistance = 5000f;

            // Destroy single planet legacy objects & demo room structures
            string[] legacyNames = new string[] { 
                "Sun", "Mercury", "Venus", "Earth", "Moon", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune",
                "Mars Planet", "Atmosphere Glow", "Phobos", "Deimos", "Mars Showcase Canvas", "NASA Starfield", 
                "Canvas", "RightHand", "LeftHand", "RightHand Controller", "LeftHand Controller", "XR Origin (VR Rig)", 
                "XR Origin", "XR Rig",
                "Environment", "Pedestal", "Pedestals", "Floor", "Ground", "Base", "Room", "Demo Environment", 
                "Structure", "Pillars", "Target", "Targets", "SunCoronaInnerGlow", "SunCoronaAura", "SunOuterVolumetricHalo", "SunRadiantHalo", "SolarPlasmaFlares", "CosmicMilkyWaySkybox" 
            };
            foreach (string leg in legacyNames)
            {
                GameObject oldObj = GameObject.Find(leg);
                if (oldObj != null) SafeDestroy(oldObj);
            }

#pragma warning disable 0618
            // Thoroughly destroy ALL pre-baked static TextMesh objects and *_3DLabel objects saved in the scene file
            foreach (var tm in Object.FindObjectsOfType<TextMesh>(true))
            {
                if (tm != null) SafeDestroy(tm.gameObject);
            }

            // Destroy all label objects, TextMesh objects, and PlanetLabelsCanvas instances
            HashSet<string> targetPlanetNames = new HashSet<string> { "Sun", "Mercury", "Venus", "Earth", "Moon", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune" };
            foreach (var lbl in Object.FindObjectsOfType<PlanetLabelManager>(true)) SafeDestroy(lbl.gameObject);
            foreach (var go in Object.FindObjectsOfType<GameObject>(true))
            {
                if (go != null && (targetPlanetNames.Contains(go.name) || go.name.Contains("Label") || go.name == "PlanetLabelsCanvas"))
                {
                    SafeDestroy(go);
                }
            }
#pragma warning restore 0618

            // Destroy all AtmosphereAura rim glow shells
#pragma warning disable 0618
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (go != null && go.name.Contains("AtmosphereAura"))
                {
                    SafeDestroy(go);
                }
            }
#pragma warning restore 0618

            // Clean up legacy grid root if present
            GameObject oldGrid = GameObject.Find("EclipticGridRoot");
            if (oldGrid != null) SafeDestroy(oldGrid);

            // Remove legacy scripts from scene objects & fix EventSystem
#pragma warning disable 0618
            foreach (var rot in Object.FindObjectsOfType<MarsExplorer.PlanetRotator>()) SafeDestroy(rot);
            foreach (var cam in Object.FindObjectsOfType<MarsExplorer.PlanetCameraController>()) SafeDestroy(cam);
            foreach (var ui in Object.FindObjectsOfType<MarsExplorer.MarsUIManager>()) SafeDestroy(ui.gameObject);
#pragma warning restore 0618

            FixEventSystem();

            Debug.Log("<color=cyan>[Solar System Scope] Generating 3D Solar System...</color>");

            GameObject root = new GameObject("SolarSystemRoot");
            SolarSystemRootInstance = root;
            List<CelestialBody> bodies = new List<CelestialBody>();

            // 1. CREATE GIANT GLOWING GOLDEN SUN (GLOWING STAR CORE + VOLUMETRIC CORONA HALO)
            GameObject sunObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sunObj.name = "Sun";
            sunObj.transform.SetParent(root.transform);
            sunObj.transform.position = Vector3.zero;
            sunObj.transform.localScale = Vector3.one * 34f;

            Renderer sunRenderer = sunObj.GetComponent<Renderer>();
            Material sunMat = new Material(GetPlanetShader());
            ApplyTextureToMaterial(sunMat, ProceduralPlanetTextures.CreateSunTexture());
            if (sunMat.HasProperty("_EmissionColor"))
            {
                sunMat.EnableKeyword("_EMISSION");
                sunMat.SetColor("_EmissionColor", new Color(1.0f, 0.75f, 0.22f) * 6.5f);
            }
            sunRenderer.material = sunMat;

            // Sun Golden Point Light (Bright solar illumination across space)
            GameObject sunLightObj = new GameObject("SunPointLight");
            sunLightObj.transform.SetParent(sunObj.transform, false);
            Light sunLight = sunLightObj.AddComponent<Light>();
            sunLight.type = LightType.Point;
            sunLight.range = 3500f;
            sunLight.intensity = 8.5f;
            sunLight.color = new Color(1.0f, 0.88f, 0.65f);

            CelestialBody sunBody = sunObj.AddComponent<CelestialBody>();
            sunBody.bodyName = "Sun";
            sunBody.radiusKm = 696340f;
            sunBody.distanceFromSunAU = 0f;
            sunBody.orbitalPeriod = "N/A (Center of System)";
            sunBody.moonCount = "8 Planets + Moons";
            sunBody.surfaceTemp = "5,500°C (Core 15M°C)";
            sunBody.description = "The Sun stays at the center of the solar system. It does not orbit any planet. Instead, the Sun rotates on its own axis about once every 27 Earth days. All planets move around the Sun in elliptical paths called orbits.";
            sunBody.selfRotationSpeed = 13.33f; // ~27 Earth days rotation period relative scale
            sunBody.surfaceGravity = 274.0f;
            bodies.Add(sunBody);

            // Clean up any Barycenter Wobble so Sun is 100% stationary and rock-solid
#pragma warning disable 0618
            foreach (var wobble in Object.FindObjectsOfType<SolarBarycenterWobble>()) SafeDestroy(wobble);
#pragma warning restore 0618

            // 2. CREATE PLANETS DATA WITH LARGER VISUAL SCALES & ACCURATE ASTRONOMICAL DYNAMICS
            Color cyanOrbitColor = new Color(0.25f, 0.75f, 1.0f, 0.85f);

            CreatePlanetData(root.transform, sunObj.transform, bodies, "Mercury", 2.4f, 42f, 15f, 6.1f, 0.03f, 2440f, 0.39f, "88 Earth Days", "0 (Zero moons)", "-180°C to 430°C", 
                "Orbits the Sun once every 88 Earth days. It spins very slowly on its axis once every 59 Earth days. Mercury has zero moons, so there is no lunar motion.", ProceduralPlanetTextures.CreateMercuryTexture(), 0.02f, 0f, cyanOrbitColor, 3.70f, initialAngleDegrees: 83f);

            CreatePlanetData(root.transform, sunObj.transform, bodies, "Venus", 4.2f, 58f, 11f, -1.48f, 177.3f, 6051f, 0.72f, "225 Earth Days", "0 (Zero moons)", "465°C", 
                "Orbits the Sun once every 225 Earth days. It spins backwards (retrograde rotation) very slowly, taking 243 Earth days for one rotation. Venus has zero moons.", ProceduralPlanetTextures.CreateVenusTexture(), 0.01f, 0f, cyanOrbitColor, 8.87f, initialAngleDegrees: 10f);

            GameObject earthObj = CreatePlanetData(root.transform, sunObj.transform, bodies, "Earth", 4.8f, 76f, 8f, 15f, 23.4f, 6371f, 1.0f, "365.25 Earth Days", "1 (Moon)", "15°C", 
                "Orbits the Sun once a year (365.25 days) and spins on its axis once every 24 hours, creating day and night. One Moon orbits Earth once every 27.3 days, tidally locked so the same side always faces Earth.", ProceduralPlanetTextures.CreateEarthTexture(), 0.016f, 0.0f, cyanOrbitColor, 9.81f, initialAngleDegrees: 275f);

            // Destroy all unparented duplicate Moon objects at scene root
#pragma warning disable 0618
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (go != null && go.name.StartsWith("Moon") && go.transform.parent == null)
                {
                    SafeDestroy(go);
                }
            }
#pragma warning restore 0618

            // Earth's Moon (Tidally locked: same side always faces Earth)
            CreateMoonData(root.transform, earthObj.transform, bodies, "Moon", 0.30f, 8.0f, 35f, 35f, 1737f, 1.0f, "27.3 Earth Days", "-130°C to 120°C", 
                "One Moon orbits Earth once every 27.3 days. Because it rotates at the same speed (tidally locked), the same side of the Moon always faces Earth.", ProceduralPlanetTextures.CreateMoonTexture(), tidallyLocked: true, surfaceGravity: 1.62f);

            // Mars & Moons (Phobos orbits faster than Mars rotates!)
            GameObject marsObj = CreatePlanetData(root.transform, sunObj.transform, bodies, "Mars", 3.2f, 98f, 6f, 14.6f, 25.2f, 3389f, 1.52f, "687 Earth Days", "2 (Phobos, Deimos)", "-63°C", 
                "Orbits the Sun every 687 Earth days and spins on its axis in 24.6 hours (very close to an Earth day). Two small moons, Phobos and Deimos, orbit close to Mars. Phobos orbits fast—faster than Mars rotates—rising in the west and setting in the east.", ProceduralPlanetTextures.CreateMarsTexture(), 0.03f, 0f, cyanOrbitColor, 3.72f, initialAngleDegrees: 51f);

            CreateMoonData(root.transform, marsObj.transform, bodies, "Phobos", 0.20f, 4.2f, 52f, 52f, 11f, 1.52f, "7.65 Hours (Fast Orbit)", "-40°C", 
                "Phobos orbits close to Mars and fast—faster than Mars rotates—rising in the west and setting in the east.", ProceduralPlanetTextures.CreateMoonTexture(), surfaceGravity: 0.0057f);

            CreateMoonData(root.transform, marsObj.transform, bodies, "Deimos", 0.15f, 6.5f, 24f, 24f, 6f, 1.52f, "30.3 Hours", "-40°C", 
                "Deimos is the smaller outer moon of Mars, orbiting close to Mars in 30.3 hours.", ProceduralPlanetTextures.CreateMoonTexture(), surfaceGravity: 0.003f);

            // Jupiter & Galilean Moons (Orbiting rapidly around Jupiter's equator)
            GameObject jupiterObj = CreatePlanetData(root.transform, sunObj.transform, bodies, "Jupiter", 11.5f, 140f, 3.5f, 36f, 3.1f, 69911f, 5.20f, "11.9 Earth Years", "95 Recognized Moons", "-110°C", 
                "Orbits the Sun once every 11.9 Earth years and spins very fast, taking only about 10 hours for one rotation. Jupiter has 95 recognized moons, including the four large Galilean moons (Io, Europa, Ganymede, Callisto) that orbit rapidly around its equator.", ProceduralPlanetTextures.CreateJupiterTexture(), 0.02f, 0f, cyanOrbitColor, 24.79f, initialAngleDegrees: 97f);

            CreateMoonData(root.transform, jupiterObj.transform, bodies, "Io", 0.35f, 14.5f, 45f, 45f, 1821f, 5.20f, "1.77 Earth Days", "-130°C", 
                "Galilean moon of Jupiter orbiting rapidly around its equator, famous for intense volcanic activity.", ProceduralPlanetTextures.CreateMoonTexture(), useParentEquatorialPlane: true, surfaceGravity: 1.796f);

            CreateMoonData(root.transform, jupiterObj.transform, bodies, "Europa", 0.30f, 18.0f, 34f, 34f, 1560f, 5.20f, "3.55 Earth Days", "-160°C", 
                "Galilean moon of Jupiter orbiting rapidly around its equator with a smooth icy ocean crust.", ProceduralPlanetTextures.CreateMoonTexture(), useParentEquatorialPlane: true, surfaceGravity: 1.315f);

            CreateMoonData(root.transform, jupiterObj.transform, bodies, "Ganymede", 0.45f, 22.0f, 25f, 25f, 2634f, 5.20f, "7.15 Earth Days", "-160°C", 
                "The largest moon in the Solar System, orbiting rapidly around Jupiter's equator.", ProceduralPlanetTextures.CreateMoonTexture(), useParentEquatorialPlane: true, surfaceGravity: 1.428f);

            CreateMoonData(root.transform, jupiterObj.transform, bodies, "Callisto", 0.40f, 26.5f, 18f, 18f, 2410f, 5.20f, "16.7 Earth Days", "-140°C", 
                "The outermost Galilean moon, heavily cratered and orbiting rapidly around Jupiter's equator.", ProceduralPlanetTextures.CreateMoonTexture(), useParentEquatorialPlane: true, surfaceGravity: 1.235f);

            // Saturn & Ring Moons (Titan orbiting outside ring system, inner prograde moons)
            GameObject saturnObj = CreatePlanetData(root.transform, sunObj.transform, bodies, "Saturn", 9.2f, 184f, 2.5f, 33.6f, 26.7f, 58232f, 9.58f, "29.5 Earth Years", "146 Moons", "-140°C", 
                "Orbits the Sun once every 29.5 Earth years and rotates in about 10.7 hours. Saturn has 146 moons, including Titan, which orbits outside its ring system every 16 days. Most inner moons move prograde.", ProceduralPlanetTextures.CreateSaturnTexture(), 0.02f, 0f, cyanOrbitColor, 10.44f, initialAngleDegrees: 18f);

            CreateSaturnRings(saturnObj);

            CreateMoonData(root.transform, saturnObj.transform, bodies, "Titan", 0.45f, 24.0f, 20f, 20f, 2574f, 9.58f, "16 Earth Days", "-179°C", 
                "Saturn's largest moon, Titan, orbits outside its ring system every 16 days in a prograde orbit.", ProceduralPlanetTextures.CreateMoonTexture(), useParentEquatorialPlane: true, surfaceGravity: 1.352f);

            CreateMoonData(root.transform, saturnObj.transform, bodies, "Enceladus", 0.22f, 14.0f, 40f, 40f, 252f, 9.58f, "1.37 Earth Days", "-201°C", 
                "Inner prograde moon moving in the same direction as Saturn spins, known for ice plumes.", ProceduralPlanetTextures.CreateMoonTexture(), useParentEquatorialPlane: true, surfaceGravity: 0.113f);

            CreateMoonData(root.transform, saturnObj.transform, bodies, "Rhea", 0.28f, 18.0f, 28f, 28f, 764f, 9.58f, "4.5 Earth Days", "-174°C", 
                "Inner prograde moon moving in the same direction as Saturn's spin.", ProceduralPlanetTextures.CreateMoonTexture(), useParentEquatorialPlane: true, surfaceGravity: 0.264f);

            // Uranus & Tilted Equatorial Moons (97.8° tilt, moons orbit in tilted equatorial plane)
            GameObject uranusObj = CreatePlanetData(root.transform, sunObj.transform, bodies, "Uranus", 6.8f, 226f, 1.8f, 21.1f, 97.8f, 25362f, 19.22f, "84 Earth Years", "28 Known Moons", "-195°C", 
                "Orbits the Sun once every 84 Earth years. Uranus is tilted completely on its side, rotating on its axis once every 17 hours. It has 28 known moons that orbit in line with the planet's tilted equatorial plane.", ProceduralPlanetTextures.CreateUranusTexture(), 0.02f, 0f, cyanOrbitColor, 8.69f, initialAngleDegrees: 42f);
            uranusObj.transform.rotation = Quaternion.Euler(82f, 0f, 15f);

            CreateUranusRings(uranusObj);

            CreateMoonData(root.transform, uranusObj.transform, bodies, "Miranda", 0.20f, 9.5f, 38f, 38f, 235f, 19.22f, "1.4 Earth Days", "-213°C", 
                "Orbits in line with Uranus's 98° tilted equatorial plane.", ProceduralPlanetTextures.CreateMoonTexture(), useParentEquatorialPlane: true, surfaceGravity: 0.079f);

            CreateMoonData(root.transform, uranusObj.transform, bodies, "Ariel", 0.26f, 12.0f, 29f, 29f, 578f, 19.22f, "2.5 Earth Days", "-213°C", 
                "Orbits in line with Uranus's tilted equatorial plane.", ProceduralPlanetTextures.CreateMoonTexture(), useParentEquatorialPlane: true, surfaceGravity: 0.269f);

            CreateMoonData(root.transform, uranusObj.transform, bodies, "Titania", 0.32f, 17.5f, 16f, 16f, 788f, 19.22f, "8.7 Earth Days", "-213°C", 
                "The largest moon of Uranus, orbiting in line with the planet's tilted equatorial plane.", ProceduralPlanetTextures.CreateMoonTexture(), useParentEquatorialPlane: true, surfaceGravity: 0.379f);

            CreateMoonData(root.transform, uranusObj.transform, bodies, "Oberon", 0.30f, 20.5f, 12f, 12f, 761f, 19.22f, "13.5 Earth Days", "-213°C", 
                "Outermost major moon orbiting in line with Uranus's 98° tilted equatorial plane.", ProceduralPlanetTextures.CreateMoonTexture(), useParentEquatorialPlane: true, surfaceGravity: 0.346f);

            // Neptune & Triton (Retrograde orbit!)
            GameObject neptuneObj = CreatePlanetData(root.transform, sunObj.transform, bodies, "Neptune", 6.4f, 268f, 1.2f, 22.5f, 28.3f, 24622f, 30.05f, "165 Earth Years", "16 Moons", "-200°C", 
                "Orbits the Sun once every 165 Earth years and rotates once every 16 hours. Neptune has 16 moons. Its largest moon, Triton, has a retrograde orbit, meaning it travels in the opposite direction of Neptune's rotation.", ProceduralPlanetTextures.CreateNeptuneTexture(), 0.01f, 0f, cyanOrbitColor, 11.15f, initialAngleDegrees: 225f);

            CreateMoonData(root.transform, neptuneObj.transform, bodies, "Triton", 0.38f, 12.5f, -26f, 26f, 1353f, 30.05f, "5.88 Earth Days (Retrograde)", "-235°C", 
                "Neptune's largest moon, Triton, has a retrograde orbit, meaning it travels in the opposite direction of Neptune's rotation.", ProceduralPlanetTextures.CreateMoonTexture(), retrogradeOrbit: true, surfaceGravity: 0.779f);

            // Pluto (Dwarf Planet)
            GameObject plutoObj = CreatePlanetData(root.transform, sunObj.transform, bodies, "Pluto", 1.4f, 310f, 0.8f, 3.8f, 57.5f, 1188f, 39.48f, "248 Earth Years", "5 Moons", "-230°C",
                "Pluto is a dwarf planet in the Kuiper belt, a ring of bodies beyond Neptune. It has an eccentric and inclined orbit.", ProceduralPlanetTextures.CreateMoonTexture(), 0.15f, 17f, cyanOrbitColor, 0.62f, initialAngleDegrees: 135f);

            // 3. SETUP ASTEROID BELT, ECLIPTIC GRID & CONSTELLATIONS
            root.AddComponent<AsteroidBelt>();
            root.AddComponent<EclipticGrid>();
            root.AddComponent<ConstellationMap>();

            // Clean up any old Comet or extra LabelManager instances
#pragma warning disable 0618
            foreach (var lbl in Object.FindObjectsOfType<PlanetLabelManager>()) SafeDestroy(lbl.gameObject);
            GameObject oldComet = GameObject.Find("Comet Halley");
            if (oldComet != null) SafeDestroy(oldComet);
#pragma warning restore 0618

            GalacticVortexMotion vortex = root.AddComponent<GalacticVortexMotion>();
            vortex.enableGalacticVortexTrails = false;

            // 4. SETUP CAMERA CONTROLLER
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
            }
            SpaceCameraInstance = mainCam;
            mainCam.nearClipPlane = 0.1f;
            mainCam.farClipPlane = 10000f;
            mainCam.backgroundColor = new Color(0.015f, 0.008f, 0.030f);
            mainCam.clearFlags = CameraClearFlags.Skybox;

            // Configure Cosmic Space Ambient Lighting for high-contrast planetary shadows
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.04f, 0.09f);

            var oldCamCtrl = mainCam.GetComponent<MarsExplorer.PlanetCameraController>();
            if (oldCamCtrl != null) SafeDestroy(oldCamCtrl);

            SolarSystemCameraController camCtrl = mainCam.gameObject.GetComponent<SolarSystemCameraController>();
            if (camCtrl == null) camCtrl = mainCam.gameObject.AddComponent<SolarSystemCameraController>();
            camCtrl.SetTarget(sunObj.transform, 140f);

            // Clean up extra AudioListeners to ensure strictly 1 AudioListener
#pragma warning disable 0618
            AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>();
            for (int i = 1; i < listeners.Length; i++) SafeDestroy(listeners[i]);
#pragma warning restore 0618

            // 4. SETUP MILKY WAY COSMIC SKYBOX (Native RenderSettings Skybox at Infinity - Zero Mesh Clipping)
            Shader skyboxShader = Shader.Find("Skybox/Panoramic") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
            Material skyboxMat = new Material(skyboxShader);
            Texture2D milkyWayTex = ProceduralPlanetTextures.CreateMilkyWaySkyboxTexture();
            skyboxMat.mainTexture = milkyWayTex;
            if (skyboxMat.HasProperty("_Tex")) skyboxMat.SetTexture("_Tex", milkyWayTex);
            if (skyboxMat.HasProperty("_BaseMap")) skyboxMat.SetTexture("_BaseMap", milkyWayTex);
            if (skyboxMat.HasProperty("_ImageType")) skyboxMat.SetInt("_ImageType", 0);
            if (skyboxMat.HasProperty("_MirrorOnBack")) skyboxMat.SetInt("_MirrorOnBack", 0);
            RenderSettings.skybox = skyboxMat;

            GameObject oldSkybox = GameObject.Find("CosmicMilkyWaySkybox");
            if (oldSkybox != null) SafeDestroy(oldSkybox);

            // 5. SETUP DEEP SPACE MULTI-COLORED STARFIELD
            GameObject starfield = new GameObject("CosmicStarfield");
            starfield.transform.SetParent(root.transform);
            ParticleSystem ps = starfield.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 100000f;
            main.startSpeed = 0f;
            main.startSize = 0.35f;
            main.maxParticles = 6000;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 650f;

            ParticleSystemRenderer psRenderer = starfield.GetComponent<ParticleSystemRenderer>();
            Material starMat = new Material(GetPlanetShader());
            starMat.color = Color.white;
            if (starMat.HasProperty("_BaseColor")) starMat.SetColor("_BaseColor", Color.white);
            if (starMat.HasProperty("_Color")) starMat.SetColor("_Color", Color.white);
            psRenderer.material = starMat;

            // Emit particles with randomized colors (blue giants, warm yellow stars, white dwarfs) and sizes
            Color[] starColors = new Color[] {
                new Color(0.95f, 0.98f, 1.0f, 0.95f),
                new Color(0.60f, 0.85f, 1.0f, 0.90f),
                new Color(1.0f, 0.85f, 0.55f, 0.90f),
                new Color(0.85f, 0.70f, 1.0f, 0.85f)
            };

            for (int i = 0; i < 5000; i++)
            {
                var emitParams = new ParticleSystem.EmitParams();
                emitParams.startColor = starColors[Random.Range(0, starColors.Length)];
                emitParams.startSize = Random.Range(0.15f, 0.50f);
                ps.Emit(emitParams, 1);
            }

            // 5. SETUP FUTURISTIC HUD UI MANAGER & 3D FLOATING PLANET LABELS
            GameObject uiObj = new GameObject("SolarSystemUI");
            uiObj.transform.SetParent(root.transform);
            PlanetLabelManager labelMgr = uiObj.AddComponent<PlanetLabelManager>();
            labelMgr.Initialize(bodies);
            SolarSystemUIManager uiMgr = uiObj.AddComponent<SolarSystemUIManager>();
            uiMgr.Initialize(bodies, camCtrl, labelMgr);
        }

        private static GameObject CreatePlanetData(Transform root, Transform sun, List<CelestialBody> bodies, string name, float scale, float orbitDist, 
            float orbitSpeed, float rotSpeed, float tilt, float radiusKm, float distAU, string period, string moons, string temp, string desc, Texture2D tex,
            float eccentricity = 0.05f, float inclination = 0f, Color? orbitColor = null, float surfaceGravity = 9.81f, float initialAngleDegrees = -1f)
        {
            GameObject planetObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planetObj.name = name;
            planetObj.transform.SetParent(root);
            planetObj.transform.localScale = Vector3.one * scale;

            Renderer renderer = planetObj.GetComponent<Renderer>();
            Material mat = new Material(GetPlanetShader());
            ApplyTextureToMaterial(mat, tex);
            renderer.material = mat;

            // Attach Planetary Flow Shader for dynamic atmospheric cloud/surface fluid flow motion
            var flowShader = planetObj.AddComponent<MarsExplorer.PlanetaryFlowShader>();
            flowShader.flowSpeedU = Random.Range(0.005f, 0.02f);
            flowShader.flowSpeedV = Random.Range(0.002f, 0.008f);

            CelestialBody body = planetObj.AddComponent<CelestialBody>();
            body.bodyName = name;
            body.orbitCenter = sun;
            body.orbitRadius = orbitDist;
            body.orbitSpeed = orbitSpeed;
            body.selfRotationSpeed = rotSpeed;
            body.axialTilt = tilt;
            body.radiusKm = radiusKm;
            body.distanceFromSunAU = distAU;
            body.orbitalPeriod = period;
            body.moonCount = moons;
            body.surfaceTemp = temp;
            body.surfaceGravity = surfaceGravity;
            body.eccentricity = eccentricity;
            body.orbitInclination = inclination;
            body.initialOrbitAngleDegrees = initialAngleDegrees;
            if (orbitColor.HasValue) body.orbitColor = orbitColor.Value;

            float startAngle = initialAngleDegrees >= 0f ? initialAngleDegrees * Mathf.Deg2Rad : Random.Range(0f, Mathf.PI * 2f);
            planetObj.transform.position = sun.position + new Vector3(Mathf.Cos(startAngle) * orbitDist, 0f, Mathf.Sin(startAngle) * orbitDist);

            bodies.Add(body);
            return planetObj;
        }

        private static GameObject CreateMoonData(Transform root, Transform parentPlanet, List<CelestialBody> bodies, string name, float scale, float orbitDist, 
            float orbitSpeed, float selfRotSpeed, float radiusKm, float distAU, string period, string temp, string desc, Texture2D tex, 
            bool tidallyLocked = false, bool retrogradeOrbit = false, bool useParentEquatorialPlane = false, Color? orbitColor = null, float surfaceGravity = 1.62f)
        {
            GameObject moonObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moonObj.name = name;
            moonObj.transform.SetParent(parentPlanet, false);
            moonObj.transform.localScale = Vector3.one * scale;

            Renderer renderer = moonObj.GetComponent<Renderer>();
            Material mat = new Material(GetPlanetShader());
            ApplyTextureToMaterial(mat, tex);
            renderer.material = mat;

            CelestialBody moonBody = moonObj.AddComponent<CelestialBody>();
            moonBody.bodyName = name;
            moonBody.orbitCenter = parentPlanet;
            moonBody.orbitRadius = orbitDist;
            moonBody.orbitSpeed = retrogradeOrbit ? -Mathf.Abs(orbitSpeed) : orbitSpeed;
            moonBody.selfRotationSpeed = selfRotSpeed;
            moonBody.radiusKm = radiusKm;
            moonBody.distanceFromSunAU = distAU;
            moonBody.orbitalPeriod = period;
            moonBody.moonCount = "Natural Satellite";
            moonBody.surfaceTemp = temp;
            moonBody.surfaceGravity = surfaceGravity;
            moonBody.description = desc;
            moonBody.isTidallyLocked = tidallyLocked;
            moonBody.useParentEquatorialPlane = useParentEquatorialPlane;
            moonBody.showOrbitLine = false;
            moonBody.orbitColor = orbitColor ?? new Color(0.25f, 0.75f, 1.0f, 0.45f);

            bodies.Add(moonBody);
            return moonObj;
        }

        private static void SetupXROriginVRRig(GameObject root, Camera mainCam)
        {
            GameObject xrOriginObj = new GameObject("XR Origin (VR Rig)");
            xrOriginObj.transform.SetParent(root.transform, false);

            GameObject cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(xrOriginObj.transform, false);

            mainCam.transform.SetParent(cameraOffset.transform, false);

            // LeftHand Controller
            GameObject leftController = new GameObject("LeftHand Controller");
            leftController.transform.SetParent(mainCam.transform, false);
            leftController.transform.localPosition = new Vector3(-0.35f, -0.45f, 0.60f);
            leftController.transform.localRotation = Quaternion.Euler(20f, 12f, 0f);
            leftController.transform.localScale = Vector3.one * 1.0f;
            XRHandMeshVisualizer leftVis = leftController.AddComponent<XRHandMeshVisualizer>();
            leftVis.isLeftHand = true;
            leftVis.showHandMesh = false;

            // RightHand Controller
            GameObject rightController = new GameObject("RightHand Controller");
            rightController.transform.SetParent(mainCam.transform, false);
            rightController.transform.localPosition = new Vector3(0.35f, -0.45f, 0.60f);
            rightController.transform.localRotation = Quaternion.Euler(20f, -12f, 0f);
            rightController.transform.localScale = Vector3.one * 1.0f;
            XRHandMeshVisualizer rightVis = rightController.AddComponent<XRHandMeshVisualizer>();
            rightVis.isLeftHand = false;
            rightVis.showHandMesh = false;
        }

        private static Color GetAtmosphereColor(string name)
        {
            switch (name)
            {
                case "Earth": return new Color(0.20f, 0.70f, 1.0f, 0.22f);
                case "Venus": return new Color(1.0f, 0.85f, 0.45f, 0.18f);
                case "Mars": return new Color(1.0f, 0.42f, 0.20f, 0.16f);
                case "Jupiter": return new Color(0.95f, 0.75f, 0.45f, 0.14f);
                case "Saturn": return new Color(0.90f, 0.82f, 0.52f, 0.14f);
                case "Uranus": return new Color(0.20f, 0.92f, 1.0f, 0.22f);
                case "Neptune": return new Color(0.15f, 0.48f, 1.0f, 0.22f);
                default: return Color.clear;
            }
        }

        private static void CreateSaturnRings(GameObject saturn)
        {
            GameObject ringsObj = new GameObject("SaturnRings");
            ringsObj.transform.SetParent(saturn.transform, false);
            ringsObj.transform.localScale = new Vector3(2.4f, 0.01f, 2.4f);

            GameObject ringDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringDisc.transform.SetParent(ringsObj.transform, false);
            ringDisc.transform.localScale = new Vector3(1f, 0.001f, 1f);
            SafeDestroy(ringDisc.GetComponent<Collider>());

            Renderer ringRenderer = ringDisc.GetComponent<Renderer>();
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
            Material ringMat = new Material(unlitShader);
            ApplyTextureToMaterial(ringMat, ProceduralPlanetTextures.CreateSaturnRingsTexture());
            ConfigureRingMaterial(ringMat);
            ringRenderer.material = ringMat;
        }

        private static void CreateUranusRings(GameObject uranus)
        {
            GameObject ringsObj = new GameObject("UranusRings");
            ringsObj.transform.SetParent(uranus.transform, false);
            ringsObj.transform.localRotation = Quaternion.Euler(82f, 0f, 15f); // Tilted 82 degrees vertically (Matching reference photo)
            ringsObj.transform.localScale = new Vector3(2.2f, 0.01f, 2.2f);

            GameObject ringDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringDisc.transform.SetParent(ringsObj.transform, false);
            ringDisc.transform.localScale = new Vector3(1f, 0.001f, 1f);
            SafeDestroy(ringDisc.GetComponent<Collider>());

            Renderer ringRenderer = ringDisc.GetComponent<Renderer>();
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
            Material ringMat = new Material(unlitShader);
            ApplyTextureToMaterial(ringMat, ProceduralPlanetTextures.CreateUranusRingsTexture());
            ConfigureRingMaterial(ringMat);
            ringRenderer.material = ringMat;
        }

        private static void ConfigureRingMaterial(Material mat)
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1.0f); // Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0.0f);     // Alpha Blend
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off); // Double-Sided!
            if (mat.HasProperty("_CullMode")) mat.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private static void CreateComet(Transform root, Transform sun)
        {
            GameObject cometObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cometObj.name = "Comet Halley";
            cometObj.transform.SetParent(root, false);
            cometObj.transform.localScale = Vector3.one * 1.5f;

            Renderer renderer = cometObj.GetComponent<Renderer>();
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material mat = new Material(unlitShader);
            mat.color = new Color(0.65f, 0.90f, 1.0f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.65f, 0.90f, 1.0f));
            renderer.material = mat;

            CelestialBody cometBody = cometObj.AddComponent<CelestialBody>();
            cometBody.bodyName = "Comet Halley";
            cometBody.orbitCenter = sun;
            cometBody.orbitRadius = 140f;
            cometBody.orbitSpeed = 12f;
            cometBody.eccentricity = 0.65f;
            cometBody.orbitInclination = 18f;
            cometBody.showOrbitLine = true;
            cometBody.orbitColor = new Color(0.40f, 0.85f, 1.0f, 0.70f);

            HelicalTrajectoryTrail trail = cometObj.AddComponent<HelicalTrajectoryTrail>();
            trail.trailTime = 18f;
            trail.startWidth = 0.55f;
            trail.endWidth = 0.05f;
            trail.trailColor = new Color(0.55f, 0.35f, 0.95f, 0.85f);
        }

        private static Shader GetPlanetShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Unlit/Texture");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Sprites/Default");
            return s;
        }

        private static void ApplyTextureToMaterial(Material mat, Texture2D tex)
        {
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);

            // Set low smoothness and zero specularity so planet poles never render white glare circles
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.05f);
            if (mat.HasProperty("_SpecColor")) mat.SetColor("_SpecColor", Color.black);
        }

        private static void ConfigureTransparentMaterial(Material mat, Color col)
        {
            mat.color = col;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 1);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private static void FixEventSystem()
        {
#pragma warning disable 0618
            var eventSystems = Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystems == null || eventSystems.Length == 0)
            {
                GameObject esObj = new GameObject("EventSystem");
                var es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#elif ENABLE_LEGACY_INPUT_MANAGER
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }
            else
            {
                // Destroy all extra EventSystems so there is strictly 1 EventSystem in scene
                for (int i = 1; i < eventSystems.Length; i++)
                {
                    if (eventSystems[i] != null)
                    {
                        SafeDestroy(eventSystems[i].gameObject);
                    }
                }

                if (eventSystems[0] != null)
                {
                    var es = eventSystems[0];
                    var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    if (standalone != null) SafeDestroy(standalone);

#if ENABLE_INPUT_SYSTEM
                    if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                    {
                        es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    }
#endif
                }
            }
#pragma warning restore 0618
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }
    }
}
