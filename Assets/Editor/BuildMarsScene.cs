using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MarsExplorer;

namespace MarsExplorer.Editor
{
    public class BuildMarsScene : EditorWindow
    {
        [MenuItem("Mars/Build Mars Scene", false, 1)]
        public static void GenerateMarsScene()
        {
            // 1. Create New Scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Setup Directional Sun Light
            GameObject sunLightObj = new GameObject("Sun Light");
            Light sunLight = sunLightObj.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1.0f, 0.96f, 0.92f);
            sunLight.intensity = 1.6f;
            sunLightObj.transform.rotation = Quaternion.Euler(20, -35, 0);

            // 3. Create Mars Planet Sphere
            GameObject marsObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marsObj.name = "Mars Planet";
            marsObj.transform.position = Vector3.zero;
            marsObj.transform.localScale = new Vector3(5f, 5f, 5f);

            Renderer marsRenderer = marsObj.GetComponent<Renderer>();
            Material marsMat = new Material(Shader.Find("Standard"));
            marsMat.name = "MarsMaterial";
            marsMat.color = new Color(0.85f, 0.42f, 0.2f);
            marsMat.SetFloat("_Glossiness", 0.15f);
            marsRenderer.sharedMaterial = marsMat;

            // Attach Scripts (Rotator with Direct Mouse Drag Rotation)
            PlanetRotator rotator = marsObj.AddComponent<PlanetRotator>();
            rotator.allowDirectDrag = true;
            rotator.dragSensitivity = 0.4f;

            ProceduralMarsTexture procTex = marsObj.AddComponent<ProceduralMarsTexture>();
            LandmarkManager landmarkMgr = marsObj.AddComponent<LandmarkManager>();

            // 4. Create Atmosphere Glow Sphere
            GameObject atmosObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            atmosObj.name = "Atmosphere Glow";
            atmosObj.transform.SetParent(marsObj.transform, false);
            atmosObj.transform.localScale = Vector3.one * 1.04f;
            DestroyImmediate(atmosObj.GetComponent<Collider>());

            Renderer atmosRenderer = atmosObj.GetComponent<Renderer>();
            Material atmosMat = new Material(Shader.Find("Unlit/Color"));
            atmosMat.color = new Color(0.85f, 0.35f, 0.15f, 0.25f);
            atmosRenderer.sharedMaterial = atmosMat;

            // 5. Create Moons (Phobos & Deimos)
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

            // 6. Create Camera & Orbit Controller
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            camObj.AddComponent<AudioListener>();

            PlanetCameraController camCtrl = camObj.AddComponent<PlanetCameraController>();
            camCtrl.target = marsObj.transform;
            camCtrl.distance = 12f;

            // 7. Create Deep Space Starfield Particles
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

            // 8. Create EventSystem & UI Canvas
            GameObject eventSys = new GameObject("EventSystem");
            eventSys.AddComponent<EventSystem>();
            eventSys.AddComponent<StandaloneInputModule>();

            GameObject canvasObj = new GameObject("NASA Mars UI Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            MarsUIManager uiManager = canvasObj.AddComponent<MarsUIManager>();
            uiManager.planetRotator = rotator;
            uiManager.textureGenerator = procTex;
            uiManager.landmarkManager = landmarkMgr;
            uiManager.cameraController = camCtrl;

            // Header Panel
            GameObject headerObj = CreateUIObject("HeaderPanel", canvasObj.transform);
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1);
            headerRt.sizeDelta = new Vector2(-40, 60);
            headerRt.anchoredPosition = new Vector2(0, -20);
            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color(0.04f, 0.06f, 0.10f, 0.85f);

            Text titleTxt = CreateUIText("TitleText", headerObj.transform, "🚀 NASA MARS SHOWCASE • SOL SYSTEM", 20, TextAnchor.MiddleLeft, Color.white);
            RectTransform titleRt = titleTxt.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 0); titleRt.anchorMax = new Vector2(0.5f, 1);
            titleRt.offsetMin = new Vector2(20, 0); titleRt.offsetMax = new Vector2(0, 0);
            uiManager.titleText = titleTxt;

            Text telemetryTxt = CreateUIText("TelemetryText", headerObj.transform, "ROTATION ANGLE: 0°", 15, TextAnchor.MiddleRight, new Color(0f, 0.94f, 1f));
            RectTransform telemetryRt = telemetryTxt.GetComponent<RectTransform>();
            telemetryRt.anchorMin = new Vector2(0.5f, 0); telemetryRt.anchorMax = new Vector2(1, 1);
            telemetryRt.offsetMin = new Vector2(0, 0); telemetryRt.offsetMax = new Vector2(-20, 0);
            uiManager.telemetryText = telemetryTxt;

            // Bottom Control Deck
            GameObject controlsObj = CreateUIObject("ControlDeck", canvasObj.transform);
            RectTransform controlsRt = controlsObj.GetComponent<RectTransform>();
            controlsRt.anchorMin = new Vector2(0, 0);
            controlsRt.anchorMax = new Vector2(1, 0);
            controlsRt.pivot = new Vector2(0.5f, 0);
            controlsRt.sizeDelta = new Vector2(-40, 70);
            controlsRt.anchoredPosition = new Vector2(0, 20);
            Image controlsBg = controlsObj.AddComponent<Image>();
            controlsBg.color = new Color(0.04f, 0.06f, 0.10f, 0.85f);

            Button playBtn = CreateUIButton("PlayPauseBtn", controlsObj.transform, "Pause", new Vector2(20, 15), new Vector2(100, 40));
            uiManager.playPauseButton = playBtn;
            uiManager.playPauseText = playBtn.GetComponentInChildren<Text>();

            Button revBtn = CreateUIButton("ReverseBtn", controlsObj.transform, "Prograde", new Vector2(135, 15), new Vector2(100, 40));
            uiManager.reverseButton = revBtn;
            uiManager.reverseText = revBtn.GetComponentInChildren<Text>();

            // Speed Slider
            GameObject sliderObj = CreateUIObject("SpeedSlider", controlsObj.transform);
            RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
            sliderRt.anchoredPosition = new Vector2(260, 25);
            sliderRt.sizeDelta = new Vector2(140, 20);
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0.1f;
            slider.maxValue = 5.0f;
            slider.value = 1.0f;
            uiManager.speedSlider = slider;

            Text speedTxt = CreateUIText("SpeedText", controlsObj.transform, "1.0x", 14, TextAnchor.MiddleLeft, Color.white);
            RectTransform speedTxtRt = speedTxt.GetComponent<RectTransform>();
            speedTxtRt.anchoredPosition = new Vector2(410, 15);
            speedTxtRt.sizeDelta = new Vector2(50, 40);
            uiManager.speedText = speedTxt;

            // HUD Card Panel
            GameObject hudCardObj = CreateUIObject("HUDCard", canvasObj.transform);
            RectTransform hudRt = hudCardObj.GetComponent<RectTransform>();
            hudRt.anchorMin = new Vector2(1, 0.5f);
            hudRt.anchorMax = new Vector2(1, 0.5f);
            hudRt.pivot = new Vector2(1, 0.5f);
            hudRt.sizeDelta = new Vector2(280, 320);
            hudRt.anchoredPosition = new Vector2(-20, 0);
            Image hudBg = hudCardObj.AddComponent<Image>();
            hudBg.color = new Color(0.06f, 0.08f, 0.14f, 0.95f);

            uiManager.hudPanel = hudCardObj;
            uiManager.hudTitleText = CreateUIText("HUDTitle", hudCardObj.transform, "Jezero Crater", 18, TextAnchor.UpperLeft, Color.white);
            uiManager.hudTitleText.rectTransform.anchoredPosition = new Vector2(15, -15);
            uiManager.hudTitleText.rectTransform.sizeDelta = new Vector2(220, 30);

            uiManager.hudTypeText = CreateUIText("HUDType", hudCardObj.transform, "NASA Perseverance Rover", 12, TextAnchor.UpperLeft, new Color(1f, 0.5f, 0.2f));
            uiManager.hudTypeText.rectTransform.anchoredPosition = new Vector2(15, -45);
            uiManager.hudTypeText.rectTransform.sizeDelta = new Vector2(220, 20);

            uiManager.hudCoordsText = CreateUIText("HUDCoords", hudCardObj.transform, "Lat: 18.38° | Lon: 77.58°", 13, TextAnchor.UpperLeft, new Color(0.8f, 0.8f, 0.8f));
            uiManager.hudCoordsText.rectTransform.anchoredPosition = new Vector2(15, -75);
            uiManager.hudCoordsText.rectTransform.sizeDelta = new Vector2(250, 20);

            uiManager.hudElevationText = CreateUIText("HUDElevation", hudCardObj.transform, "Elevation: -2.5 km", 13, TextAnchor.UpperLeft, new Color(0f, 0.94f, 1f));
            uiManager.hudElevationText.rectTransform.anchoredPosition = new Vector2(15, -100);
            uiManager.hudElevationText.rectTransform.sizeDelta = new Vector2(250, 20);

            uiManager.hudDiameterText = CreateUIText("HUDDiameter", hudCardObj.transform, "Size: 45 km crater", 13, TextAnchor.UpperLeft, new Color(0.8f, 0.8f, 0.8f));
            uiManager.hudDiameterText.rectTransform.anchoredPosition = new Vector2(15, -125);
            uiManager.hudDiameterText.rectTransform.sizeDelta = new Vector2(250, 20);

            uiManager.hudDetailsText = CreateUIText("HUDDetails", hudCardObj.transform, "NASA Perseverance Rover landing site searching for signs of ancient biosignatures.", 12, TextAnchor.UpperLeft, Color.white);
            uiManager.hudDetailsText.rectTransform.anchoredPosition = new Vector2(15, -155);
            uiManager.hudDetailsText.rectTransform.sizeDelta = new Vector2(250, 110);

            Button closeHudBtn = CreateUIButton("HUDCloseBtn", hudCardObj.transform, "✕", new Vector2(240, 280), new Vector2(30, 30));
            uiManager.hudCloseButton = closeHudBtn;

            // 9. Save Scene
            string scenePath = "Assets/Scenes/MarsScene.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            Debug.Log($"<color=lime>Successfully generated NASA Mars Scene at: {scenePath}</color>");

            AssetDatabase.Refresh();
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static Text CreateUIText(string name, Transform parent, string content, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            Text txt = go.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color;
            txt.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            return txt;
        }

        private static Button CreateUIButton(string name, Transform parent, string label, Vector2 pos, Vector2 size)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = go.AddComponent<Image>();
            img.color = new Color(0.85f, 0.35f, 0.15f, 0.9f);

            Button btn = go.AddComponent<Button>();

            Text txt = CreateUIText("Label", go.transform, label, 14, TextAnchor.MiddleCenter, Color.white);
            RectTransform txtRt = txt.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            return btn;
        }
    }
}
