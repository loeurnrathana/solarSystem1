using UnityEngine;
using UnityEngine.UI;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SolarSystemScope
{
    public class PlanetSurfaceExplorer : MonoBehaviour
    {
        public static PlanetSurfaceExplorer Instance { get; private set; }

        private GameObject surfaceRoot;
        private GameObject playerObj;
        private CharacterController characterController;
        private Camera surfaceCam;
        private RobotCompanion companion;
        private GameObject spaceRootRef;
        private GameObject hudCanvas;
        private Text hudTextComponent;
        private Text planetTitleComponent;

        // Spaceship & Navigation Variables
        private GameObject spaceshipObj;
        private Vector3 spaceshipPosition;
        private string currentTelemetryStr = "";

        // Cinematic Fade Variables
        private GameObject fadeCanvas;
        private Image fadeImage;
        private Text fadeText;
        private bool isTransitioning = false;
        private bool isExploringSurface = false;
        private string currentPlanetName = "";
        public string CurrentPlanetName => currentPlanetName;

        // First Person Movement Variables
        private float moveSpeed = 8f;
        private float sprintSpeed = 16f;
        private float gravity = -9.81f;
        private float jumpHeight = 2.0f;
        private Vector3 playerVelocity;
        private bool isGrounded;

        // Audio & SFX Variables
        private AudioSource playerAudioSource;
        private AudioClip walkStepClip;
        private AudioClip sprintStepClip;
        private AudioClip jumpClip;
        private float footstepTimer = 0f;

        // Mouse Look Variables
        private float cameraPitch = 0f;
        private float mouseSensitivity = 2.0f;

        // Day & Night Cycle Variables
        private Light directionalSunLight;
        private GameObject nightStarfieldObj;
        private GameObject skyDomeObj;
        private Light astronautFlashlight;
        private bool isVacuumSpace = false;
        private float timeOfDay = 0.28f; // Start at ~09:00 AM (Morning)
        private float dayCycleDurationSeconds = 60f; // Exactly 1 Minute (60s) full Day & Night cycle!
        private bool isTimePaused = false;
        private Color baseSkyColor;
        private Color baseFogColor;
        private Color baseGroundColor;
        private float baseFogDensity = 0.0018f;
        private GameObject skyMoonObj;
        private Material skyMoonMat;
        private Light skyMoonLight;
        private GameObject moonAuraObj;
        private Material moonAuraMat;
        private GameObject skySunObj;
        private Material skySunMat;
        private GameObject sunCoronaHaloObj;
        private Material sunCoronaHaloMat;
        private GameObject skyCloudsGroupObj;
        private System.Collections.Generic.List<Material> cloudMaterials = new System.Collections.Generic.List<Material>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void EnterPlanetSurface(CelestialBody body)
        {
            if (isExploringSurface || isTransitioning || body == null) return;
            StartCoroutine(TransitionToSurfaceRoutine(body));
        }

        public void ExitPlanetSurface(bool forceExit = false)
        {
            if (!isExploringSurface || isTransitioning) return;

            if (!forceExit)
            {
                if (UFOQuizManager.Instance == null)
                {
                    GameObject qmObj = new GameObject("UFOQuizManager");
                    qmObj.AddComponent<UFOQuizManager>();
                }

                if (UFOQuizManager.Instance != null)
                {
                    if (UFOQuizManager.Instance.IsPlanetQuizPassed(currentPlanetName))
                    {
                        if (!UFOQuizManager.Instance.IsQuizActive)
                        {
                            UFOQuizManager.Instance.ShowRevisitPrompt(currentPlanetName);
                        }
                        return;
                    }
                    else
                    {
                        if (!UFOQuizManager.Instance.IsQuizActive)
                        {
                            UFOQuizManager.Instance.StartQuiz(currentPlanetName);
                        }
                        return;
                    }
                }
            }

            StartCoroutine(TransitionToOrbitRoutine());
        }

        private IEnumerator TransitionToSurfaceRoutine(CelestialBody body)
        {
            isTransitioning = true;
            currentPlanetName = body.bodyName;

            // Instantly hide description card in 0s (no delay!)
            if (PlanetLabelManager.Instance != null)
            {
                PlanetLabelManager.Instance.SetFocusedBody(null);
                PlanetLabelManager.Instance.enabled = false; // Disable OnGUI component instantly
            }

            // 1. Setup Fade Canvas Overlay
            if (fadeCanvas == null) CreateFadeOverlay();
            fadeCanvas.SetActive(true);

            if (fadeText != null)
            {
                string subText = (body.bodyName == "Earth") ? "BORINGGGG" : "LET'S GOOOOOO";
                fadeText.text = $"{body.bodyName.ToUpper()} ?\n{subText}";
                fadeText.color = new Color(0.3f, 0.95f, 1.0f, 0f);
            }

            // 2. Slow Fade to Black over 3.5 Seconds
            float fadeDuration = 3.5f;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);

                if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, alpha);
                if (fadeText != null) fadeText.color = new Color(0.3f, 0.95f, 1.0f, Mathf.Min(alpha * 1.5f, 1f));

                yield return null;
            }

            if (fadeImage != null) fadeImage.color = Color.black;

            // 3. Switch View Behind Black Screen
            Debug.Log($"<color=green>[NASA Surface Explorer] Landed on surface of {currentPlanetName}!</color>");

            if (spaceRootRef == null) spaceRootRef = SolarSystemScope.SolarSystemBootstrapper.SolarSystemRootInstance;
            if (spaceRootRef == null) spaceRootRef = GameObject.Find("SolarSystemRoot");
            if (spaceRootRef != null) spaceRootRef.SetActive(false);

            if (SolarSystemScope.SolarSystemBootstrapper.SpaceCameraInstance != null)
            {
                SolarSystemScope.SolarSystemBootstrapper.SpaceCameraInstance.gameObject.SetActive(false);
            }
            else if (Camera.main != null)
            {
                Camera.main.enabled = false;
            }

            BuildSurfaceEnvironment(body);
            isExploringSurface = true;

            yield return new WaitForSeconds(0.4f);

            // 4. Fade In From Black over 2.0 Seconds
            elapsed = 0f;
            float fadeInDuration = 2.0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1.0f - Mathf.Clamp01(elapsed / fadeInDuration);

                if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, alpha);
                if (fadeText != null) fadeText.color = new Color(0.3f, 0.95f, 1.0f, alpha);

                yield return null;
            }

            if (fadeCanvas != null) fadeCanvas.SetActive(false);

            // 5. Lock Cursor for FPS Movement
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isTransitioning = false;

            // 6. ASTRO-BOT Speaks Welcome Message ONLY AFTER Black Screen Turns Off 100%
            if (companion != null)
            {
                companion.SpeakWelcomeMessage(currentPlanetName);
            }
        }

        private IEnumerator TransitionToOrbitRoutine()
        {
            isTransitioning = true;

            if (fadeCanvas == null) CreateFadeOverlay();
            fadeCanvas.SetActive(true);

            if (fadeText != null)
            {
                if (currentPlanetName == "Earth")
                {
                    fadeText.text = "ALIEN WHERE ARE YOUU :)";
                }
                else
                {
                    fadeText.text = "ALIEN : BYE BYE\n(O_O)/~";
                }
                fadeText.color = new Color(0.3f, 0.95f, 1.0f, 0f);
            }

            // 1. Fade to Black over 1.2 seconds
            float elapsed = 0f;
            float fadeDuration = 1.2f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);

                if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, alpha);
                if (fadeText != null) fadeText.color = new Color(0.3f, 0.95f, 1.0f, alpha);

                yield return null;
            }

            if (fadeImage != null) fadeImage.color = Color.black;

            // 2. Safely destroy surface environment root
            if (surfaceRoot != null)
            {
                Destroy(surfaceRoot);
                surfaceRoot = null;
            }
            surfaceCam = null;
            companion = null;
            hudCanvas = null;
            hudTextComponent = null;
            planetTitleComponent = null;

            isExploringSurface = false;

            // Turn OFF planetary atmospheric fog in space & reset cosmic ambient lighting
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.04f, 0.09f);

            // 3. Restore Space Root & Orbit Camera
            if (spaceRootRef == null) spaceRootRef = SolarSystemScope.SolarSystemBootstrapper.SolarSystemRootInstance;
            if (spaceRootRef == null) spaceRootRef = GameObject.Find("SolarSystemRoot");
            if (spaceRootRef != null)
            {
                spaceRootRef.SetActive(true);
            }

            // Restore / Create Main Orbit Space Camera
            EnsureMainSpaceCameraActive();

            if (PlanetLabelManager.Instance != null)
            {
                PlanetLabelManager.Instance.enabled = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

            yield return new WaitForSeconds(0.2f);

            // 4. Fade in from Black over 1.0 Second
            elapsed = 0f;
            while (elapsed < 1.0f)
            {
                elapsed += Time.deltaTime;
                float alpha = 1.0f - Mathf.Clamp01(elapsed / 1.0f);

                if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, alpha);
                if (fadeText != null) fadeText.color = new Color(0.3f, 0.95f, 1.0f, alpha);

                yield return null;
            }

            if (fadeCanvas != null) fadeCanvas.SetActive(false);
            isTransitioning = false;
        }

        private void EnsureMainSpaceCameraActive()
        {
            Camera targetCam = null;

            // 1. Check static SpaceCameraInstance from SolarSystemBootstrapper
            if (SolarSystemScope.SolarSystemBootstrapper.SpaceCameraInstance != null)
            {
                targetCam = SolarSystemScope.SolarSystemBootstrapper.SpaceCameraInstance;
            }

            // 2. If null, search all cameras in scene (including inactive)
            if (targetCam == null)
            {
                Camera[] cams = Resources.FindObjectsOfTypeAll<Camera>();
                foreach (Camera c in cams)
                {
                    if (c != null && c.gameObject != null && c.name == "Main Camera" && c.gameObject.hideFlags == HideFlags.None)
                    {
                        targetCam = c;
                        break;
                    }
                }
            }

            if (targetCam != null)
            {
                targetCam.gameObject.SetActive(true);
                targetCam.enabled = true;
#pragma warning disable 0618
                foreach (var al in Object.FindObjectsOfType<AudioListener>(true))
                {
                    if (al != null && al.gameObject != targetCam.gameObject)
                    {
                        al.enabled = false;
                    }
                }
#pragma warning restore 0618
                AudioListener spaceListener = targetCam.GetComponent<AudioListener>();
                if (spaceListener == null) spaceListener = targetCam.gameObject.AddComponent<AudioListener>();
                spaceListener.enabled = true;
            }

            // 3. If still null, create a brand new Main Space Camera!
            if (targetCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                targetCam = camObj.AddComponent<Camera>();
                targetCam.backgroundColor = new Color(0.015f, 0.008f, 0.030f);
                targetCam.clearFlags = CameraClearFlags.SolidColor;
                targetCam.farClipPlane = 5000f;
                targetCam.nearClipPlane = 0.1f;
                
                // Add AudioListener if missing
                if (Object.FindAnyObjectByType<AudioListener>() == null)
                {
                    camObj.AddComponent<AudioListener>();
                }
            }

            // 4. Activate Camera & GameObject
            targetCam.gameObject.SetActive(true);
            targetCam.enabled = true;
            targetCam.tag = "MainCamera";
            targetCam.backgroundColor = new Color(0.015f, 0.008f, 0.030f);
            targetCam.clearFlags = CameraClearFlags.SolidColor;

            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.04f, 0.09f);

            // 5. Ensure SolarSystemCameraController is attached and enabled
            SolarSystemScope.SolarSystemCameraController camCtrl = targetCam.GetComponent<SolarSystemScope.SolarSystemCameraController>();
            if (camCtrl == null)
            {
                camCtrl = targetCam.gameObject.AddComponent<SolarSystemScope.SolarSystemCameraController>();
            }
            camCtrl.enabled = true;

            // 6. Focus camera back on the planet you were exploring (or default to Sun)
            GameObject targetPlanetObj = null;
            if (!string.IsNullOrEmpty(currentPlanetName))
            {
                targetPlanetObj = GameObject.Find(currentPlanetName);
            }

            if (targetPlanetObj != null)
            {
                CelestialBody body = targetPlanetObj.GetComponent<CelestialBody>();
                float focusDist = (body != null) ? Mathf.Max(body.transform.localScale.y * 3.5f, 25f) : 40f;
                camCtrl.SetTarget(targetPlanetObj.transform, focusDist);
            }
            else
            {
                GameObject sunObj = GameObject.Find("Sun");
                if (sunObj != null)
                {
                    camCtrl.SetTarget(sunObj.transform, 320f);
                }
            }

            // Update static reference
            SolarSystemScope.SolarSystemBootstrapper.SpaceCameraInstance = targetCam;
        }

        private void CreateFadeOverlay()
        {
            fadeCanvas = new GameObject("CinematicFadeCanvas");
            Canvas canvas = fadeCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            fadeCanvas.AddComponent<CanvasScaler>();
            fadeCanvas.AddComponent<GraphicRaycaster>();

            GameObject imgObj = new GameObject("FadeImage");
            imgObj.transform.SetParent(fadeCanvas.transform, false);
            RectTransform rect = imgObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            fadeImage = imgObj.AddComponent<Image>();
            fadeImage.color = new Color(0f, 0f, 0f, 0f);

            GameObject textObj = new GameObject("FadeText");
            textObj.transform.SetParent(fadeCanvas.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.02f, 0.20f);
            textRect.anchorMax = new Vector2(0.98f, 0.80f);
            textRect.sizeDelta = Vector2.zero;

            fadeText = textObj.AddComponent<Text>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            fadeText.font = font;
            fadeText.fontSize = 72;
            fadeText.alignment = TextAnchor.MiddleCenter;
            fadeText.fontStyle = FontStyle.Bold;
            fadeText.lineSpacing = 1.15f;
            fadeText.color = new Color(0.3f, 0.95f, 1.0f, 0f);
        }

        private void Update()
        {
            if (!isExploringSurface || isTransitioning) return;

            // Ensure UFOQuizManager singleton exists
            if (UFOQuizManager.Instance == null)
            {
                GameObject qmObj = new GameObject("UFOQuizManager");
                qmObj.AddComponent<UFOQuizManager>();
            }

            // Pause movement & mouse look while taking UFO QCM quiz
            if (UFOQuizManager.Instance != null && UFOQuizManager.Instance.IsQuizActive)
            {
                return;
            }

            // Press ESC key to auto return to solar system space orbit
            if (WasEscPressedThisFrame())
            {
                ExitPlanetSurface(forceExit: true);
                return;
            }

            // Check E key press when near UFO
            if (playerObj != null && spaceshipObj != null)
            {
                Vector3 sPos = spaceshipObj.transform.position;
                float hDist = Vector2.Distance(new Vector2(playerObj.transform.position.x, playerObj.transform.position.z), new Vector2(sPos.x, sPos.z));
                float tDist = Vector3.Distance(playerObj.transform.position, sPos);

                if ((hDist <= 55f || tDist <= 130f) && WasEKeyPressedThisFrame())
                {
                    ExitPlanetSurface(forceExit: false);
                    return;
                }
            }

            // Continuous UFO Rotation & Sky Cloud Drift
            if (spaceshipObj != null)
            {
                spaceshipObj.transform.Rotate(Vector3.up, 20f * Time.deltaTime, Space.Self);
            }
            if (skyCloudsGroupObj != null)
            {
                skyCloudsGroupObj.transform.Rotate(Vector3.up, 0.4f * Time.deltaTime, Space.World);
            }

            UpdateDayNightCycle();
            HandlePlayerMovement();
            HandleMouseLook();
            UpdateHUDNavigation();
        }

        private void UpdateDayNightCycle()
        {
            if (!isExploringSurface || isTransitioning) return;

            // Advance time of day (1 full 60-second day-night cycle automatically)
            if (!isTimePaused)
            {
                timeOfDay += (Time.deltaTime / dayCycleDurationSeconds);
                if (timeOfDay >= 1.0f) timeOfDay -= 1.0f;
            }

            // Press [T] to fast-forward time phase (keeping R and M keys free for future gameplay!)
            if (WasTKeyPressedThisFrame())
            {
                timeOfDay = (timeOfDay + 0.25f) % 1.0f;
            }

            // Real-world astronomical sun elevation (-1 at midnight, 0 at sunrise/sunset, +1 at noon)
            float sunElevation = -Mathf.Cos(timeOfDay * 2f * Mathf.PI);

            // Calculate Sun pitch & yaw angles (Rises East 06:00, High Noon 12:00, Sets West 18:00, Midnight -75°)
            float sunPitch = Mathf.Sin(timeOfDay * 2f * Mathf.PI) * 75f;
            float sunYaw = (timeOfDay - 0.25f) * 360f;

            bool isSunAboveHorizon = sunPitch > -2.0f;

            if (directionalSunLight != null)
            {
                // Sun light rotates naturally across sky dome into earth at night
                directionalSunLight.transform.rotation = Quaternion.Euler(sunPitch, sunYaw, 0f);
                directionalSunLight.enabled = isSunAboveHorizon;
            }

            // Real-world smooth daylight factor (1.0 = High Noon, 0.5 = Sunset, 0.0 = Midnight)
            float sunVisibility = Mathf.Clamp01((sunPitch + 5.0f) / 25.0f); // Fades out as sun dips near horizon
            float sunsetFactor = Mathf.Clamp01(1.0f - Mathf.Abs(sunPitch) / 30.0f); // Peaks during sunset/sunrise near horizon
            float nightDarkness = 1.0f - sunVisibility; // 0.0 at day, 1.0 at night

            Vector3 camPos = (surfaceCam != null) ? surfaceCam.transform.position : ((playerObj != null) ? playerObj.transform.position : Vector3.zero);

            // Special Physics for Vacuum Space Worlds (Mercury & Moon) - No atmosphere, pure deep space sky & 24/7 visible stars!
            if (isVacuumSpace)
            {
                if (directionalSunLight != null && directionalSunLight.enabled)
                {
                    directionalSunLight.color = Color.white;
                    directionalSunLight.intensity = Mathf.Lerp(0.0f, 1.8f, sunVisibility);
                }

                Color vacuumSpaceSky = new Color(0.012f, 0.015f, 0.035f);
                if (surfaceCam != null) surfaceCam.backgroundColor = vacuumSpaceSky;

                RenderSettings.fog = false;
                RenderSettings.ambientSkyColor = vacuumSpaceSky * 2.5f + new Color(0.12f, 0.12f, 0.15f);
                RenderSettings.ambientEquatorColor = baseGroundColor * (0.85f - nightDarkness * 0.40f);
                RenderSettings.ambientGroundColor = baseGroundColor * (0.50f - nightDarkness * 0.30f);

                if (nightStarfieldObj != null)
                {
                    nightStarfieldObj.transform.position = camPos;
                    nightStarfieldObj.SetActive(true); // Stars visible 24/7 in vacuum space!
                }

                if (astronautFlashlight != null)
                {
                    astronautFlashlight.enabled = nightDarkness > 0.40f;
                }

                // Synchronize 3D Sun Position in Vacuum Space
                if (skySunObj != null && directionalSunLight != null)
                {
                    Vector3 sunDir = -directionalSunLight.transform.forward;
                    skySunObj.transform.position = camPos + (sunDir * 720f);
                    skySunObj.SetActive(isSunAboveHorizon);
                    if (sunCoronaHaloObj != null) sunCoronaHaloObj.SetActive(isSunAboveHorizon);

                    if (isSunAboveHorizon && skySunMat != null)
                    {
                        ApplyMaterialProps(skySunMat, new Color(5.5f, 5.5f, 5.0f, 1.0f)); // Intense Unscattered White Sun
                    }
                }
                return;
            }

            // 1. Real-World Atmospheric Sunlight Colors & Intensity
            Color daySunCol = new Color(1.0f, 0.96f, 0.85f);
            Color sunsetSunCol = new Color(1.0f, 0.45f, 0.12f); // Warm Fiery Amber Sunset
            Color duskSunCol = new Color(0.85f, 0.25f, 0.10f);   // Deep Crimson Red Horizon

            Color currentSunColor = Color.Lerp(daySunCol, sunsetSunCol, sunsetFactor);
            currentSunColor = Color.Lerp(currentSunColor, duskSunCol, Mathf.Clamp01((1.0f - sunVisibility) * sunsetFactor));

            if (directionalSunLight != null && directionalSunLight.enabled)
            {
                directionalSunLight.color = currentSunColor;
                // Sunlight smoothly dims from 1.6f down to 0.0f as the sun sets down below horizon!
                directionalSunLight.intensity = Mathf.Lerp(0.0f, 1.6f, sunVisibility);
            }

            // 2. Dynamic Real-World Atmosphere, Sky & Fog Illumination
            Color daySky = baseSkyColor;
            Color sunsetSky = new Color(0.92f, 0.42f, 0.18f);     // Fiery Sunset Sky
            Color duskSky = new Color(0.22f, 0.15f, 0.42f);       // Deep Twilight Violet (Blue Hour)
            Color nightSky = new Color(0.08f, 0.12f, 0.28f);      // Luminous Deep Space Blue/Navy (Vibrant Starlit Sky)

            Color currentSky = Color.Lerp(daySky, sunsetSky, sunsetFactor);
            currentSky = Color.Lerp(currentSky, duskSky, Mathf.Clamp01(nightDarkness * 0.65f));
            currentSky = Color.Lerp(currentSky, nightSky, nightDarkness);

            Color dayFog = baseFogColor;
            Color sunsetFog = new Color(0.88f, 0.42f, 0.20f);     // Evening Haze
            Color nightFog = new Color(0.04f, 0.07f, 0.18f);      // Soft Luminous Night Haze

            Color currentFog = Color.Lerp(dayFog, sunsetFog, sunsetFactor);
            currentFog = Color.Lerp(currentFog, nightFog, nightDarkness);

            RenderSettings.fogColor = currentFog;
            RenderSettings.fogDensity = baseFogDensity * (1.0f + sunsetFactor * 0.2f + nightDarkness * 0.15f);

            // Ambient Scene Lighting (Illuminates trees, rocks, and terrain naturally both day and night!)
            RenderSettings.ambientSkyColor = currentSky * (1.25f - nightDarkness * 0.35f);
            RenderSettings.ambientEquatorColor = baseGroundColor * (0.90f - nightDarkness * 0.30f);
            RenderSettings.ambientGroundColor = baseGroundColor * (0.60f - nightDarkness * 0.20f);

            if (surfaceCam != null)
            {
                surfaceCam.backgroundColor = currentSky;
            }

            // 3. Clouds Weather Darkening (Bright White -> Sunset Gold/Crimson -> Night Translucent Shadows)
            Color cloudDay = new Color(0.98f, 0.99f, 1.0f, 0.88f);
            Color cloudSunset = new Color(0.98f, 0.50f, 0.30f, 0.82f);
            Color cloudNight = new Color(0.12f, 0.16f, 0.28f, 0.35f);

            Color currentCloudCol = Color.Lerp(cloudDay, cloudSunset, sunsetFactor);
            currentCloudCol = Color.Lerp(currentCloudCol, cloudNight, nightDarkness);

            foreach (var cMat in cloudMaterials)
            {
                if (cMat != null) ApplyMaterialProps(cMat, currentCloudCol);
            }

            if (skyCloudsGroupObj != null)
            {
                skyCloudsGroupObj.SetActive(true);
            }

            // Astronaut Flashlight & Night Starfield
            if (skyDomeObj != null)
            {
                skyDomeObj.transform.position = camPos;

                // Live dynamic illumination of atmospheric sky dome sphere!
                Renderer skyRen = skyDomeObj.GetComponent<Renderer>();
                if (skyRen != null && skyRen.material != null)
                {
                    Color skyDomeCol = Color.Lerp(currentSky * 1.35f, new Color(0.10f, 0.15f, 0.32f), nightDarkness * 0.55f);
                    ApplyMaterialProps(skyRen.material, skyDomeCol);
                }
            }

            if (astronautFlashlight != null)
            {
                astronautFlashlight.enabled = nightDarkness > 0.40f;
            }

            if (nightStarfieldObj != null)
            {
                nightStarfieldObj.transform.position = camPos;
                nightStarfieldObj.SetActive(nightDarkness > 0.20f);
            }

            // Dynamic 180° Opposite Moon Celestial Orbit (As shown in Orbital Reference Diagram)
            // When Sun sets in the West (18:00), Moon rises in the East (18:00)!
            float moonTimeOfDay = (timeOfDay + 0.50f) % 1.0f; // Exactly 180 degrees opposite the Sun!
            float moonPitch = Mathf.Sin(moonTimeOfDay * 2f * Mathf.PI) * 75f;
            float moonYaw = (moonTimeOfDay - 0.25f) * 360f;

            bool isMoonAboveHorizon = moonPitch > -2.0f;

            if (skyMoonObj != null)
            {
                // Position Moon on celestial sky sphere exactly 180 degrees opposite the Sun
                Quaternion moonRot = Quaternion.Euler(moonPitch, moonYaw, 0f);
                Vector3 moonDirection = moonRot * Vector3.back;
                float moonDistance = 680f;

                skyMoonObj.transform.position = camPos + (moonDirection * moonDistance);
                skyMoonObj.SetActive(isMoonAboveHorizon);

                if (skyMoonMat != null)
                {
                    Color dayMoonCol = new Color(0.85f, 0.85f, 0.90f, 0.20f);
                    Color nightMoonGlowCol = new Color(3.5f, 3.5f, 3.8f, 1.0f); // Luminous Silver Glow at Night

                    Color currentMoonCol = Color.Lerp(dayMoonCol, nightMoonGlowCol, nightDarkness);
                    ApplyMaterialProps(skyMoonMat, currentMoonCol);
                }

                if (skyMoonLight != null)
                {
                    // Real Cool Blue Moonlight at Night!
                    skyMoonLight.color = new Color(0.35f, 0.55f, 0.95f);
                    skyMoonLight.enabled = isMoonAboveHorizon;
                    if (isMoonAboveHorizon)
                    {
                        skyMoonLight.transform.rotation = Quaternion.Euler(moonPitch, moonYaw, 0f);
                        skyMoonLight.intensity = Mathf.Lerp(0.0f, 0.45f, Mathf.Clamp01(moonPitch / 15.0f));
                    }
                }

                if (moonAuraMat != null)
                {
                    Color auraCol = new Color(0.85f, 0.92f, 1.0f, 0.75f * nightDarkness);
                    ApplyMaterialProps(moonAuraMat, auraCol);
                }

                if (moonAuraObj != null)
                {
                    moonAuraObj.SetActive(isMoonAboveHorizon && nightDarkness > 0.15f);
                }
            }

            // Synchronize 3D Luminous Sun position & Solar Corona with Directional Sunlight
            if (skySunObj != null && directionalSunLight != null)
            {
                // Vector pointing FROM player TOWARDS Sun direction in sky dome
                Vector3 sunDirection = -directionalSunLight.transform.forward;
                float sunDistance = 720f; // Sky dome distance

                skySunObj.transform.position = camPos + (sunDirection * sunDistance);

                // Hide Sun disc & corona completely when sun dips below horizon (isSunAboveHorizon = false)
                skySunObj.SetActive(isSunAboveHorizon);
                if (sunCoronaHaloObj != null) sunCoronaHaloObj.SetActive(isSunAboveHorizon);

                // Sunset vs Midday Sun Color Shift (HDR over-bright colors!)
                if (isSunAboveHorizon && skySunMat != null)
                {
                    Color sunDiscDayCol = new Color(5.0f, 4.8f, 3.5f, 1.0f); // Bright Golden Sun
                    Color sunDiscSunsetCol = new Color(5.5f, 2.0f, 0.5f, 1.0f); // Intense Fiery Red/Orange Sunset Sun

                    Color currentSunCol = Color.Lerp(sunDiscSunsetCol, sunDiscDayCol, Mathf.Clamp01(sunPitch / 15.0f));
                    ApplyMaterialProps(skySunMat, currentSunCol);
                }

                if (isSunAboveHorizon && sunCoronaHaloMat != null)
                {
                    Color dayCorona = new Color(1.0f, 0.92f, 0.65f, 0.65f);
                    Color sunsetCorona = new Color(1.0f, 0.45f, 0.15f, 0.75f);
                    Color currentCorona = Color.Lerp(sunsetCorona, dayCorona, Mathf.Clamp01(sunPitch / 15.0f));
                    ApplyMaterialProps(sunCoronaHaloMat, currentCorona);
                }
            }
        }

        private void UpdateHUDNavigation()
        {
            if (playerObj == null || spaceshipObj == null || hudTextComponent == null) return;

            Vector3 targetShipPos = (spaceshipObj != null) ? spaceshipObj.transform.position : spaceshipPosition;
            float distToShip = Vector3.Distance(playerObj.transform.position, targetShipPos);
            string distStr = (distToShip >= 1000f) ? $"{distToShip / 1000f:F2} km" : $"{distToShip:F0} m";

            Vector2 playerXZ = new Vector2(playerObj.transform.position.x, playerObj.transform.position.z);
            Vector2 shipXZ = new Vector2(targetShipPos.x, targetShipPos.z);
            float horizontalDistToShip = Vector2.Distance(playerXZ, shipXZ);

            bool isNearSpaceship = horizontalDistToShip <= 55f || distToShip <= 130f;
            bool isPassed = (UFOQuizManager.Instance != null && UFOQuizManager.Instance.IsPlanetQuizPassed(currentPlanetName));

            // Pressing [E] key near spaceship triggers planet QCM quiz check / launch
            if (isNearSpaceship && WasEKeyPressedThisFrame())
            {
                ExitPlanetSurface(forceExit: false);
                return;
            }

            int hours = Mathf.FloorToInt(timeOfDay * 24f);
            int minutes = Mathf.FloorToInt((timeOfDay * 24f - hours) * 60f);
            string phaseStr = (timeOfDay >= 0.22f && timeOfDay < 0.32f) ? "SUNRISE" :
                             (timeOfDay >= 0.32f && timeOfDay < 0.68f) ? "DAY" :
                             (timeOfDay >= 0.68f && timeOfDay < 0.78f) ? "SUNSET" : "NIGHT";

            string timeHUD = $"TIME: {hours:D2}:{minutes:D2} [{phaseStr}]";
            string walkStr = "WASD: Walk | Shift: Sprint | Space: Jump";
            
            string shipInteractStr;
            if (isNearSpaceship)
            {
                shipInteractStr = isPassed ? "[E] UFO Options (Retake Quiz or Leave Planet) | [ESC] Solar System" : "[E] Take UFO QCM Security Quiz (5 Qs) to Unlock UFO Launch | [ESC] Solar System";
            }
            else
            {
                shipInteractStr = isPassed ? $"UFO Target: {distStr} (Go under UFO & Press [E] for Options | [ESC] Solar System)" : $"UFO Target: {distStr} (Go under UFO & Press [E] for QCM Quiz | [ESC] Auto Return)";
            }

            hudTextComponent.text = $"{currentTelemetryStr}  ||  {timeHUD} (Auto Rise & Set Cycle | [T] Skip)  ||  {walkStr}  ||  {shipInteractStr}";
        }

        private void HandlePlayerMovement()
        {
            if (playerObj == null || characterController == null) return;

            isGrounded = characterController.isGrounded;
            if (isGrounded && playerVelocity.y < 0)
            {
                playerVelocity.y = -2f;
            }

            Vector2 input = GetMovementInput();
            bool isSprinting = IsSprintPressed();

            float speed = isSprinting ? sprintSpeed : moveSpeed;

            // Calculate movement relative to where the XR Headset / First Person Camera is facing!
            Transform moveRef = (surfaceCam != null) ? surfaceCam.transform : playerObj.transform;
            Vector3 fwd = moveRef.forward;
            Vector3 right = moveRef.right;
            fwd.y = 0f;
            right.y = 0f;
            fwd.Normalize();
            right.Normalize();

            Vector3 move = right * input.x + fwd * input.y;
            characterController.Move(move * speed * Time.deltaTime);

            // Play walk & run footstep sound effects (Minecraft sounds!)
            if (isGrounded && input.sqrMagnitude > 0.01f)
            {
                float stepInterval = isSprinting ? 0.26f : 0.42f;
                footstepTimer += Time.deltaTime;
                if (footstepTimer >= stepInterval)
                {
                    footstepTimer = 0f;
                    if (playerAudioSource != null)
                    {
                        AudioClip clipToPlay = isSprinting ? sprintStepClip : walkStepClip;
                        playerAudioSource.pitch = UnityEngine.Random.Range(0.88f, 1.15f); // Minecraft pitch variation!
                        playerAudioSource.PlayOneShot(clipToPlay, isSprinting ? 0.65f : 0.45f);
                    }
                }
            }
            else
            {
                footstepTimer = 0.3f;
            }

            // Handle Jump logic dynamically tuned for every planet & moon
            if (WasJumpPressedThisFrame() && isGrounded)
            {
                // Effective gravity clamped to minimum -3.0f so small moons don't float forever in deep space
                float effectiveGravity = Mathf.Min(gravity, -3.0f);

                // Planet/moon specific target jump height (in meters)
                float targetJumpHeight = 2.2f; // Earth default (2.2m)
                if (currentPlanetName == "Moon") targetJumpHeight = 5.2f;
                else if (currentPlanetName == "Mars" || currentPlanetName == "Mercury") targetJumpHeight = 3.8f;
                else if (currentPlanetName == "Venus" || currentPlanetName == "Saturn" || currentPlanetName == "Uranus" || currentPlanetName == "Neptune") targetJumpHeight = 2.4f;
                else if (currentPlanetName == "Jupiter") targetJumpHeight = 1.8f;
                else if (isVacuumSpace || Mathf.Abs(gravity) < 3.0f) targetJumpHeight = 6.5f; // Moons / Phobos / Deimos / Enceladus

                // Physics launch velocity formula: v_y = sqrt(2 * h * |g_eff|)
                playerVelocity.y = Mathf.Sqrt(2f * targetJumpHeight * Mathf.Abs(effectiveGravity));

                if (playerAudioSource != null && jumpClip != null)
                {
                    playerAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
                    playerAudioSource.PlayOneShot(jumpClip, 0.75f);
                }
            }

            // Apply gravity every frame (clamped so low-gravity worlds descend smoothly)
            float currentWorldGravity = Mathf.Min(gravity, -3.0f);
            playerVelocity.y += currentWorldGravity * Time.deltaTime;

            // Clamp max terminal falling speed to prevent terrain clipping
            if (playerVelocity.y < -50f) playerVelocity.y = -50f;

            characterController.Move(playerVelocity * Time.deltaTime);
        }

        private void HandleMouseLook()
        {
            if (surfaceCam == null || playerObj == null) return;
            if (UnityEngine.XR.XRSettings.isDeviceActive) return; // Allow XR Headset / Simulator to track head naturally!

            Vector2 mouseDelta = GetMouseDelta();
            float mouseX = mouseDelta.x * mouseSensitivity;
            float mouseY = mouseDelta.y * mouseSensitivity;

            cameraPitch -= mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, -85f, 85f);

            surfaceCam.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            float currentYaw = playerObj.transform.eulerAngles.y;
            playerObj.transform.rotation = Quaternion.Euler(0f, currentYaw + mouseX, 0f);
        }

        private static Shader GetSurfaceShader()
        {
            Shader s = null;
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
            {
                s = Shader.Find("Universal Render Pipeline/Lit");
                if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
            }
            if (s == null) s = Shader.Find("Unlit/Texture");
            if (s == null) s = Shader.Find("Unlit/Color");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
            return s;
        }

        private static void ApplyMaterialProps(Material mat, Color col, Texture2D tex = null, float tile = 1f)
        {
            if (mat == null) return;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
            if (mat.HasProperty("_UnlitColor")) mat.SetColor("_UnlitColor", col);
            try { mat.color = col; } catch {}

            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                try { mat.mainTexture = tex; } catch {}
                try { mat.mainTextureScale = new Vector2(tile, tile); } catch {}
            }
        }

        private GameObject CreateOrganicRockMesh(Transform parent, Vector3 pos, float scale, Color color, Texture2D texture)
        {
            GameObject rockObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rockObj.name = "NASABoulder";
            rockObj.transform.SetParent(parent, false);
            rockObj.transform.position = pos;
            rockObj.transform.rotation = UnityEngine.Random.rotation;
            rockObj.transform.localScale = new Vector3(scale * UnityEngine.Random.Range(0.45f, 0.75f), scale * UnityEngine.Random.Range(0.9f, 1.6f), scale * UnityEngine.Random.Range(0.45f, 0.75f));

            // Remove default MeshCollider to prevent CPU PhysX spikes when standing near rocks
            MeshCollider mc = rockObj.GetComponent<MeshCollider>();
            if (mc != null) Destroy(mc);

            MeshFilter mf = rockObj.GetComponent<MeshFilter>();
            if (mf != null && mf.mesh != null)
            {
                Mesh mesh = Instantiate(mf.mesh);
                Vector3[] verts = mesh.vertices;
                float seed = UnityEngine.Random.Range(0f, 100f);

                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 v = verts[i];
                    float noise = Mathf.PerlinNoise(v.x * 2.5f + seed, v.z * 2.5f + seed) * 0.35f;
                    verts[i] += v.normalized * noise;
                }

                mesh.vertices = verts;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                mf.mesh = mesh;
            }

            // Use fast primitive SphereCollider instead of complex MeshCollider
            SphereCollider sc = rockObj.AddComponent<SphereCollider>();
            sc.radius = 0.5f;

            Renderer ren = rockObj.GetComponent<Renderer>();
            Material mat = new Material(GetSurfaceShader());
            ApplyMaterialProps(mat, color, texture, 2f);
            ren.material = mat;

            // Turn off shadow casting on smaller rocks to save GPU fragment shading & shadow map fill-rate
            if (scale < 3.0f)
            {
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            return rockObj;
        }

        private void BuildSurfaceEnvironment(CelestialBody body)
        {
            surfaceRoot = new GameObject("PlanetSurfaceRoot");

            // NASA Scientific Telemetry Data Variables
            Color groundColor = new Color(0.7f, 0.35f, 0.15f);
            Color skyColor = new Color(0.85f, 0.45f, 0.25f);
            Color fogColor = new Color(0.8f, 0.4f, 0.2f);
            float planetGravity = -9.81f;
            float fogDensity = 0.008f;
            this.isVacuumSpace = false;
            string nasaTelemetryStr = "";

            switch (body.bodyName)
            {
                case "Mars":
                    groundColor = new Color(0.76f, 0.32f, 0.12f);
                    skyColor = new Color(0.88f, 0.52f, 0.30f);
                    fogColor = new Color(0.82f, 0.44f, 0.22f);
                    planetGravity = -3.72f;
                    fogDensity = 0.005f;
                    nasaTelemetryStr = "ATMOSPHERE: 95.3% CO₂ | PRESSURE: 0.006 bar (610 Pa) | TEMP: -63°C | GRAVITY: 3.72 m/s² (0.38g)";
                    break;

                case "Moon":
                    groundColor = new Color(0.38f, 0.38f, 0.41f);
                    skyColor = new Color(0.01f, 0.01f, 0.03f);
                    fogColor = Color.black;
                    planetGravity = -1.62f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = "ATMOSPHERE: Vacuum (0.00 bar) | TEMP: -130°C to +120°C | GRAVITY: 1.62 m/s² (0.17g) | SITE: Mare Tranquillitatis";
                    break;

                case "Mercury":
                    groundColor = new Color(0.28f, 0.28f, 0.31f);
                    skyColor = new Color(0.01f, 0.01f, 0.02f);
                    fogColor = Color.black;
                    planetGravity = -3.70f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = "ATMOSPHERE: Exosphere (Trace Na/He) | TEMP: -180°C to +430°C | GRAVITY: 3.70 m/s² (0.38g)";
                    break;

                case "Venus":
                    groundColor = new Color(0.55f, 0.42f, 0.16f);
                    skyColor = new Color(0.92f, 0.78f, 0.25f);
                    fogColor = new Color(0.88f, 0.72f, 0.20f);
                    planetGravity = -8.87f;
                    fogDensity = 0.016f;
                    nasaTelemetryStr = "ATMOSPHERE: 96.5% CO₂ (Dense Clouds) | PRESSURE: 92 bar | TEMP: +464°C | GRAVITY: 8.87 m/s² (0.90g)";
                    break;

                case "Earth":
                    groundColor = new Color(0.18f, 0.52f, 0.22f);
                    skyColor = new Color(0.35f, 0.68f, 0.98f);
                    fogColor = new Color(0.68f, 0.84f, 1.00f);
                    planetGravity = -9.81f;
                    fogDensity = 0.0018f;
                    nasaTelemetryStr = "ATMOSPHERE: 78% N₂, 21% O₂ | BIOSPHERE: Temperate Meadows & Liquid Oceans | TEMP: +15°C | GRAVITY: 9.81 m/s² (1.00g)";
                    break;

                case "Jupiter":
                    groundColor = new Color(0.78f, 0.58f, 0.38f);
                    skyColor = new Color(0.85f, 0.62f, 0.35f);
                    fogColor = new Color(0.80f, 0.55f, 0.30f);
                    planetGravity = -24.79f;
                    fogDensity = 0.012f;
                    nasaTelemetryStr = "ATMOSPHERE: 89% H₂, 10% He | UPPER CLOUD DECK | TEMP: -110°C | GRAVITY: 24.79 m/s² (2.53g)";
                    break;

                case "Saturn":
                    groundColor = new Color(0.85f, 0.72f, 0.48f);
                    skyColor = new Color(0.88f, 0.78f, 0.52f);
                    fogColor = new Color(0.82f, 0.72f, 0.45f);
                    planetGravity = -10.44f;
                    fogDensity = 0.010f;
                    nasaTelemetryStr = "ATMOSPHERE: 96% H₂, 3% He | SATURN RING DECK | TEMP: -140°C | GRAVITY: 10.44 m/s² (1.06g)";
                    break;

                case "Uranus":
                    groundColor = new Color(0.28f, 0.72f, 0.82f);
                    skyColor = new Color(0.35f, 0.80f, 0.88f);
                    fogColor = new Color(0.30f, 0.75f, 0.85f);
                    planetGravity = -8.69f;
                    fogDensity = 0.009f;
                    nasaTelemetryStr = "ATMOSPHERE: H₂/He/Methane Cloud Tops | TEMP: -195°C | GRAVITY: 8.69 m/s² (0.89g)";
                    break;

                case "Neptune":
                    groundColor = new Color(0.12f, 0.35f, 0.82f);
                    skyColor = new Color(0.18f, 0.42f, 0.90f);
                    fogColor = new Color(0.15f, 0.38f, 0.85f);
                    planetGravity = -11.15f;
                    fogDensity = 0.010f;
                    nasaTelemetryStr = "ATMOSPHERE: Methane Supersonic Winds (2,100 km/h) | TEMP: -200°C | GRAVITY: 11.15 m/s² (1.14g)";
                    break;

                case "Deimos":
                    groundColor = new Color(0.48f, 0.42f, 0.38f);
                    skyColor = new Color(0.01f, 0.01f, 0.03f);
                    fogColor = Color.black;
                    planetGravity = -0.15f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = "ATMOSPHERE: Vacuum (0.00 bar) | TEMP: -40°C to -110°C | GRAVITY: 0.003 m/s² (0.0003g) | MOON OF MARS";
                    break;

                case "Phobos":
                    groundColor = new Color(0.38f, 0.35f, 0.32f);
                    skyColor = new Color(0.01f, 0.01f, 0.03f);
                    fogColor = Color.black;
                    planetGravity = -0.25f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = "ATMOSPHERE: Vacuum (0.00 bar) | TEMP: -40°C to -112°C | GRAVITY: 0.0057 m/s² (0.0006g) | MOON OF MARS";
                    break;

                case "Titan":
                    groundColor = new Color(0.65f, 0.48f, 0.22f);
                    skyColor = new Color(0.88f, 0.58f, 0.22f);
                    fogColor = new Color(0.82f, 0.52f, 0.18f);
                    planetGravity = -1.35f;
                    fogDensity = 0.014f;
                    nasaTelemetryStr = "ATMOSPHERE: 95% N₂, 5% CH₄ | PRESSURE: 1.45 bar | TEMP: -179°C | GRAVITY: 1.35 m/s² (0.14g) | MOON OF SATURN";
                    break;

                case "Io":
                    groundColor = new Color(0.88f, 0.82f, 0.25f);
                    skyColor = new Color(0.01f, 0.01f, 0.03f);
                    fogColor = Color.black;
                    planetGravity = -1.80f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = "ATMOSPHERE: Thin SO₂ Exosphere | VOLCANIC BIOSPHERE | TEMP: -130°C to +1600°C | GRAVITY: 1.80 m/s² (0.18g) | MOON OF JUPITER";
                    break;

                case "Europa":
                    groundColor = new Color(0.88f, 0.92f, 0.96f);
                    skyColor = new Color(0.01f, 0.01f, 0.03f);
                    fogColor = Color.black;
                    planetGravity = -1.31f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = "ATMOSPHERE: Trace O₂ Exosphere | SUBSURFACE OCEAN | TEMP: -160°C | GRAVITY: 1.31 m/s² (0.13g) | MOON OF JUPITER";
                    break;

                case "Ganymede":
                    groundColor = new Color(0.55f, 0.55f, 0.58f);
                    skyColor = new Color(0.01f, 0.01f, 0.03f);
                    fogColor = Color.black;
                    planetGravity = -1.43f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = "ATMOSPHERE: Trace O₂ Exosphere | MAGNETOSPHERE | TEMP: -163°C | GRAVITY: 1.43 m/s² (0.15g) | LARGEST MOON IN SOLAR SYSTEM";
                    break;

                case "Callisto":
                    groundColor = new Color(0.42f, 0.40f, 0.45f);
                    skyColor = new Color(0.01f, 0.01f, 0.03f);
                    fogColor = Color.black;
                    planetGravity = -1.24f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = "ATMOSPHERE: Thin CO₂ Exosphere | ANCIENT CRATERED ICE | TEMP: -140°C | GRAVITY: 1.24 m/s² (0.13g) | MOON OF JUPITER";
                    break;

                case "Rhea":
                    groundColor = new Color(0.78f, 0.80f, 0.85f);
                    skyColor = new Color(0.01f, 0.01f, 0.03f);
                    fogColor = Color.black;
                    planetGravity = -0.26f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = "ATMOSPHERE: Trace O₂/CO₂ Exosphere | HEAVY WATER-ICE CRUST | TEMP: -174°C | GRAVITY: 0.26 m/s² (0.027g) | MOON OF SATURN";
                    break;

                case "Enceladus":
                    groundColor = new Color(0.95f, 0.97f, 1.0f);
                    skyColor = new Color(0.01f, 0.01f, 0.03f);
                    fogColor = Color.black;
                    planetGravity = -0.11f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = "ATMOSPHERE: Water Vapor Cryovolcano Plumes | OCEAN WORLD | TEMP: -201°C | GRAVITY: 0.11 m/s² (0.011g) | MOON OF SATURN";
                    break;

                case "Iapetus":
                    groundColor = new Color(0.25f, 0.24f, 0.26f);
                    skyColor = new Color(0.01f, 0.01f, 0.03f);
                    fogColor = Color.black;
                    planetGravity = -0.22f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = "ATMOSPHERE: Vacuum (0.00 bar) | TWO-TONE WALNUT CRUST & EQUATORIAL RIDGE | TEMP: -143°C | GRAVITY: 0.22 m/s² (0.022g) | MOON OF SATURN";
                    break;

                case "Titania":
                    groundColor = new Color(0.62f, 0.64f, 0.68f);
                    skyColor = new Color(0.01f, 0.01f, 0.03f);
                    fogColor = Color.black;
                    planetGravity = -0.38f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = "ATMOSPHERE: Thin CO₂ Exosphere | FAULT CANYONS & WATER-ICE | TEMP: -203°C | GRAVITY: 0.38 m/s² (0.039g) | MOON OF URANUS";
                    break;

                case "Oberon":
                    groundColor = new Color(0.48f, 0.46f, 0.50f);
                    skyColor = new Color(0.01f, 0.01f, 0.03f);
                    fogColor = Color.black;
                    planetGravity = -0.35f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = "ATMOSPHERE: Vacuum (0.00 bar) | HEAVILY CRATERED ICE-ROCK | TEMP: -203°C | GRAVITY: 0.35 m/s² (0.036g) | MOON OF URANUS";
                    break;

                case "Ariel":
                    groundColor = new Color(0.72f, 0.75f, 0.80f);
                    skyColor = new Color(0.01f, 0.01f, 0.03f);
                    fogColor = Color.black;
                    planetGravity = -0.27f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = "ATMOSPHERE: Thin CO₂ Exosphere | YOUNG BRIGHT RIFT VALLEYS | TEMP: -213°C | GRAVITY: 0.27 m/s² (0.028g) | MOON OF URANUS";
                    break;

                case "Miranda":
                    groundColor = new Color(0.68f, 0.70f, 0.75f);
                    skyColor = new Color(0.01f, 0.01f, 0.03f);
                    fogColor = Color.black;
                    planetGravity = -0.08f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = "ATMOSPHERE: Vacuum (0.00 bar) | CHAOTIC ICE SCARPS & 20KM VERONA RUPES CLIFFS | TEMP: -213°C | GRAVITY: 0.08 m/s² (0.008g) | MOON OF URANUS";
                    break;

                case "Triton":
                    groundColor = new Color(0.85f, 0.75f, 0.80f);
                    skyColor = new Color(0.08f, 0.12f, 0.25f);
                    fogColor = new Color(0.06f, 0.10f, 0.20f);
                    planetGravity = -0.78f;
                    fogDensity = 0.003f;
                    isVacuumSpace = false;
                    nasaTelemetryStr = "ATMOSPHERE: Thin Nitrogen Haze | CRYOVOLCANIC NITROGEN GEYSERS & RETROGRADE ORBIT | TEMP: -235°C | GRAVITY: 0.78 m/s² (0.08g) | MOON OF NEPTUNE";
                    break;

                default:
                    groundColor = new Color(0.42f, 0.40f, 0.38f);
                    skyColor = new Color(0.01f, 0.01f, 0.03f);
                    fogColor = Color.black;
                    planetGravity = -0.50f;
                    fogDensity = 0.0001f;
                    isVacuumSpace = true;
                    nasaTelemetryStr = $"ATMOSPHERE: Vacuum (0.00 bar) | LOW MICROGRAVITY | NATURAL SATELLITE: {body.bodyName.ToUpper()}";
                    break;
            }

            if (body != null && body.surfaceGravity > 0f)
            {
                planetGravity = -body.surfaceGravity;
            }
            gravity = planetGravity;

            // Get authentic procedural texture map from NASA planetary textures generator
            Texture2D surfaceTexture = null;
            switch (body.bodyName)
            {
                case "Mars": surfaceTexture = ProceduralPlanetTextures.CreateMarsTexture(); break;
                case "Moon": surfaceTexture = ProceduralPlanetTextures.CreateMoonTexture(); break;
                case "Mercury": surfaceTexture = ProceduralPlanetTextures.CreateMercuryTexture(); break;
                case "Venus": surfaceTexture = ProceduralPlanetTextures.CreateVenusTexture(); break;
                case "Earth": surfaceTexture = ProceduralPlanetTextures.CreateEarthTexture(); break;
                case "Jupiter": surfaceTexture = ProceduralPlanetTextures.CreateJupiterTexture(); break;
                case "Saturn": surfaceTexture = ProceduralPlanetTextures.CreateSaturnTexture(); break;
                case "Uranus": surfaceTexture = ProceduralPlanetTextures.CreateUranusTexture(); break;
                case "Neptune": surfaceTexture = ProceduralPlanetTextures.CreateNeptuneTexture(); break;
                default: surfaceTexture = ProceduralPlanetTextures.CreateMoonTexture(); break;
            }

            // 1. Create Procedural 3D Heightmap Terrain Mesh with Hills & Valleys
            CreateProcedural3DTerrain(surfaceRoot.transform, groundColor, surfaceTexture, body.bodyName);

            // 2. Photorealistic Multi-Layered Forest (Lush Trees & Fallen Logs - Earth Only!)
            if (body.bodyName == "Earth")
            {
                for (int i = 0; i < 500; i++)
                {
                    float tx = UnityEngine.Random.Range(-400f, 400f);
                    float tz = UnityEngine.Random.Range(-400f, 400f);

                    if (Vector2.Distance(new Vector2(tx, tz), Vector2.zero) < 10f) continue;
                    if (Vector2.Distance(new Vector2(tx, tz), new Vector2(0f, -50f)) < 18f) continue;

                    float groundY = GetTerrainHeight(tx, tz, body.bodyName);
                    
                    float sizeRoll = UnityEngine.Random.value;
                    float treeScale = (sizeRoll < 0.30f) ? UnityEngine.Random.Range(1.2f, 2.2f) :
                                      (sizeRoll < 0.75f) ? UnityEngine.Random.Range(2.8f, 5.2f) :
                                                           UnityEngine.Random.Range(6.0f, 10.5f);

                    CreateCartoonOakTree(surfaceRoot.transform, new Vector3(tx, groundY, tz), treeScale);
                }

                // 2b. Fallen Logs & Timber on Earth Forest Floor
                for (int l = 0; l < 70; l++)
                {
                    float lx = UnityEngine.Random.Range(-380f, 380f);
                    float lz = UnityEngine.Random.Range(-380f, 380f);
                    if (Vector2.Distance(new Vector2(lx, lz), Vector2.zero) < 12f) continue;

                    float groundY = GetTerrainHeight(lx, lz, body.bodyName);
                    CreateFallenLog(surfaceRoot.transform, new Vector3(lx, groundY, lz), UnityEngine.Random.Range(2.5f, 6.0f));
                }
            }

            // 2c. Multi-Size Randomized Rock Field (Slender Slate Stone Formations)
            Color rockColor = groundColor * 0.65f;
            if (body.bodyName == "Earth") rockColor = new Color(0.32f, 0.35f, 0.30f); // Mossy Slate Grey-Stone

            for (int r = 0; r < 200; r++)
            {
                float rx = UnityEngine.Random.Range(-400f, 400f);
                float rz = UnityEngine.Random.Range(-400f, 400f);

                if (Vector2.Distance(new Vector2(rx, rz), Vector2.zero) < 10f) continue;
                if (Vector2.Distance(new Vector2(rx, rz), new Vector2(0f, -50f)) < 18f) continue;

                float sizeRoll = UnityEngine.Random.value;
                float rScale = (sizeRoll < 0.55f) ? UnityEngine.Random.Range(0.5f, 1.4f) :
                               (sizeRoll < 0.85f) ? UnityEngine.Random.Range(1.8f, 3.8f) :
                                                    UnityEngine.Random.Range(5.0f, 9.0f);

                float groundY = GetTerrainHeight(rx, rz, body.bodyName);
                Vector3 pos = new Vector3(rx, groundY + rScale * 0.15f, rz);

                CreateOrganicRockMesh(surfaceRoot.transform, pos, rScale, rockColor, surfaceTexture);
            }

            // 2d. If Earth, build rich photorealistic Earth biosphere (Ocean lake, Fluffy clouds & Wildflowers)
            if (body.bodyName == "Earth")
            {
                CreateEarthBiosphere(surfaceRoot.transform);
            }

            // 3. Create Planet-Specific Celestial Objects & Moons in the Sky Dome
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
            Shader moonUnlitShader = unlitShader;

            string moonNameStr = "";
            Color moonTintCol = Color.white;
            Vector3 moonScale = Vector3.one * 35f;

            if (body.bodyName == "Moon")
            {
                // Earthrise view hanging in Lunar sky!
                GameObject earthInSky = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                earthInSky.name = "EarthInLunarSky";
                earthInSky.transform.SetParent(surfaceRoot.transform);
                earthInSky.transform.position = new Vector3(180f, 160f, 380f);
                earthInSky.transform.localScale = Vector3.one * 60f;
                Destroy(earthInSky.GetComponent<Collider>());

                Renderer earthRen = earthInSky.GetComponent<Renderer>();
                Material earthMat = new Material(unlitShader);
                Texture2D earthTex = ProceduralPlanetTextures.CreateEarthTexture();
                ApplyMaterialProps(earthMat, Color.white, earthTex);
                if (earthMat.HasProperty("_EmissionColor"))
                {
                    earthMat.EnableKeyword("_EMISSION");
                    earthMat.SetColor("_EmissionColor", new Color(0.15f, 0.45f, 0.85f) * 1.2f);
                }
                earthRen.material = earthMat;
            }
            else if (body.bodyName == "Earth")
            {
                moonNameStr = "MoonInEarthSky";
                moonTintCol = new Color(1.0f, 1.0f, 1.0f);
                moonScale = Vector3.one * 35f;
            }
            else if (body.bodyName == "Mars")
            {
                moonNameStr = "PhobosInMartianSky";
                moonTintCol = new Color(0.75f, 0.65f, 0.55f);
                moonScale = new Vector3(28f, 20f, 18f); // Asymmetrical Phobos body!
            }
            else if (body.bodyName == "Jupiter")
            {
                moonNameStr = "IoInJupiterSky";
                moonTintCol = new Color(0.95f, 0.85f, 0.40f); // Sulfur Yellow Io
                moonScale = Vector3.one * 42f;
            }
            else if (body.bodyName == "Saturn")
            {
                moonNameStr = "TitanInSaturnSky";
                moonTintCol = new Color(0.90f, 0.72f, 0.42f); // Golden Haze Titan
                moonScale = Vector3.one * 40f;
            }
            else if (body.bodyName == "Uranus")
            {
                moonNameStr = "TitaniaInUranusSky";
                moonTintCol = new Color(0.78f, 0.88f, 0.95f);
                moonScale = Vector3.one * 32f;
            }
            else if (body.bodyName == "Neptune")
            {
                moonNameStr = "TritonInNeptuneSky";
                moonTintCol = new Color(0.68f, 0.85f, 0.95f); // Ice Blue Triton
                moonScale = Vector3.one * 36f;
            }

            if (!string.IsNullOrEmpty(moonNameStr))
            {
                // Create Dynamic Sky Moon for Planet's Night Sky!
                skyMoonObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                skyMoonObj.name = moonNameStr;
                skyMoonObj.transform.SetParent(surfaceRoot.transform);
                skyMoonObj.transform.localScale = moonScale;
                Destroy(skyMoonObj.GetComponent<Collider>());

                Renderer moonRen = skyMoonObj.GetComponent<Renderer>();
                skyMoonMat = new Material(moonUnlitShader);
                Texture2D moonTex = ProceduralPlanetTextures.CreateMoonTexture();
                ApplyMaterialProps(skyMoonMat, moonTintCol, moonTex);
                moonRen.material = skyMoonMat;

                // Point Light for cool moonlight illumination
                GameObject moonLightObj = new GameObject("MoonPointLight");
                moonLightObj.transform.SetParent(skyMoonObj.transform, false);
                skyMoonLight = moonLightObj.AddComponent<Light>();
                skyMoonLight.type = LightType.Point;
                skyMoonLight.range = 1200f;
                skyMoonLight.intensity = 4.5f;
                skyMoonLight.color = new Color(0.85f, 0.92f, 1.0f);

                // Moon Luminous Glow Halo Shell
                moonAuraObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                moonAuraObj.name = "MoonLuminousAuraHalo";
                moonAuraObj.transform.SetParent(skyMoonObj.transform, false);
                moonAuraObj.transform.localScale = Vector3.one * 1.35f;
                Destroy(moonAuraObj.GetComponent<Collider>());

                Renderer auraRen = moonAuraObj.GetComponent<Renderer>();
                moonAuraMat = new Material(moonUnlitShader);
                moonAuraMat.color = new Color(0.85f, 0.92f, 1.0f, 0.70f);
                if (moonAuraMat.HasProperty("_BaseColor")) moonAuraMat.SetColor("_BaseColor", new Color(0.85f, 0.92f, 1.0f, 0.70f));
                moonAuraMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                moonAuraMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive Glow!
                moonAuraMat.SetInt("_ZWrite", 0);
                moonAuraMat.renderQueue = 3000;
                auraRen.material = moonAuraMat;
            }

            // 4. Create Deep Space Starfield for Vacuum Environments (Moon / Mercury)
            if (isVacuumSpace)
            {
                GameObject starfieldObj = new GameObject("NASA_VacuumStarfield");
                starfieldObj.transform.SetParent(surfaceRoot.transform);
                ParticleSystem ps = starfieldObj.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = 100000f;
                main.startSpeed = 0f;
                main.startSize = 0.4f;
                main.maxParticles = 3000;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 500f;

                ParticleSystemRenderer psRenderer = starfieldObj.GetComponent<ParticleSystemRenderer>();
                Material starMat = new Material(unlitShader);
                starMat.color = Color.white;
                psRenderer.material = starMat;

                for (int s = 0; s < 2500; s++)
                {
                    var emitParams = new ParticleSystem.EmitParams();
                    emitParams.startColor = Color.white;
                    emitParams.startSize = UnityEngine.Random.Range(0.2f, 0.6f);
                    ps.Emit(emitParams, 1);
                }
            }

            // 5. Setup 3D Atmospheric Sky Dome, Dynamic Sun Light & Ambient Environment Lighting
            baseSkyColor = skyColor;
            baseFogColor = fogColor;
            baseGroundColor = groundColor;
            timeOfDay = 0.28f; // Start at 09:00 AM Morning

            if (!isVacuumSpace)
            {
                CreateAtmosphericSkyDome(surfaceRoot.transform, skyColor, fogColor, new Color(1.0f, 0.96f, 0.75f));
            }

            GameObject lightObj = new GameObject("NASA_SunLight");
            lightObj.transform.SetParent(surfaceRoot.transform);
            directionalSunLight = lightObj.AddComponent<Light>();
            directionalSunLight.type = LightType.Directional;
            directionalSunLight.transform.rotation = Quaternion.Euler(isVacuumSpace ? 45f : 35f, -50f, 0f);
            directionalSunLight.color = isVacuumSpace ? Color.white : new Color(1.0f, 0.96f, 0.85f);
            directionalSunLight.intensity = isVacuumSpace ? 1.8f : 1.5f;

            // Create 3D Luminous Sun Sphere & Solar Corona Flare Halo in Sky
            skySunObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skySunObj.name = "NASA_3DSunInSky";
            skySunObj.transform.SetParent(surfaceRoot.transform, false);
            skySunObj.transform.localScale = Vector3.one * 55f; // Large visible Sun sphere in sky
            Destroy(skySunObj.GetComponent<Collider>());

            Shader sunShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            skySunMat = new Material(sunShader);
            Color sunHDRCol = new Color(5.0f, 4.8f, 3.2f, 1.0f); // Bright HDR Golden Sun Glow!
            ApplyMaterialProps(skySunMat, sunHDRCol);
            skySunObj.GetComponent<Renderer>().material = skySunMat;

            // Solar Corona Flare Halo Shell
            sunCoronaHaloObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sunCoronaHaloObj.name = "SolarCoronaHalo";
            sunCoronaHaloObj.transform.SetParent(skySunObj.transform, false);
            sunCoronaHaloObj.transform.localScale = Vector3.one * 1.55f;
            Destroy(sunCoronaHaloObj.GetComponent<Collider>());

            Renderer coronaRen = sunCoronaHaloObj.GetComponent<Renderer>();
            sunCoronaHaloMat = new Material(sunShader);
            Color coronaCol = new Color(1.0f, 0.90f, 0.60f, 0.65f);
            ApplyMaterialProps(sunCoronaHaloMat, coronaCol);
            sunCoronaHaloMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sunCoronaHaloMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive solar flare!
            sunCoronaHaloMat.SetInt("_ZWrite", 0);
            sunCoronaHaloMat.renderQueue = 3000;
            coronaRen.material = sunCoronaHaloMat;

            RenderSettings.fog = !isVacuumSpace;
            if (!isVacuumSpace)
            {
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogMode = FogMode.Exponential;
                baseFogDensity = fogDensity;
                RenderSettings.fogDensity = fogDensity;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = skyColor * 1.15f;
            RenderSettings.ambientEquatorColor = groundColor * 0.75f;
            RenderSettings.ambientGroundColor = groundColor * 0.40f;

            // Create Dynamic Night Starfield for atmospheric night hours
            if (!isVacuumSpace)
            {
                nightStarfieldObj = new GameObject("NASA_NightStarfield");
                nightStarfieldObj.transform.SetParent(surfaceRoot.transform);
                ParticleSystem psNight = nightStarfieldObj.AddComponent<ParticleSystem>();
                var mainN = psNight.main;
                mainN.startLifetime = 100000f;
                mainN.startSpeed = 0f;
                mainN.startSize = 0.35f;
                mainN.maxParticles = 2500;
                var shapeN = psNight.shape;
                shapeN.shapeType = ParticleSystemShapeType.Sphere;
                shapeN.radius = 600f;

                ParticleSystemRenderer psRendererN = nightStarfieldObj.GetComponent<ParticleSystemRenderer>();
                Material starMatN = new Material(unlitShader);
                starMatN.color = Color.white;
                psRendererN.material = starMatN;

                for (int s = 0; s < 2000; s++)
                {
                    var emitParams = new ParticleSystem.EmitParams();
                    emitParams.startColor = new Color(0.9f, 0.95f, 1.0f, 0.9f);
                    emitParams.startSize = UnityEngine.Random.Range(0.2f, 0.5f);
                    psNight.Emit(emitParams, 1);
                }
                nightStarfieldObj.SetActive(false); // Initially off during morning/day
            }

            // 6. Create Player Character (Astronaut) - Explicit Upright Orientation (0 Roll Tilt)
            playerObj = new GameObject("NASA_AstronautPlayer");
            playerObj.transform.SetParent(surfaceRoot.transform);
            playerObj.transform.position = new Vector3(0f, 3f, 0f);
            playerObj.transform.rotation = Quaternion.identity;

            characterController = playerObj.AddComponent<CharacterController>();
            characterController.height = 2.0f;
            characterController.radius = 0.5f;
            characterController.center = new Vector3(0f, 1f, 0f);

            // Add First Person Camera
            GameObject camObj = new GameObject("FirstPersonCamera");
            camObj.transform.SetParent(playerObj.transform);
            camObj.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            cameraPitch = 5f;
            camObj.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            surfaceCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            surfaceCam.backgroundColor = skyColor;
            surfaceCam.clearFlags = isVacuumSpace ? CameraClearFlags.SolidColor : CameraClearFlags.SolidColor;
            surfaceCam.nearClipPlane = 0.05f;
            surfaceCam.farClipPlane = 600000f; // Expanded to 600 km for 200 km spaceship & sky beacon visibility
#pragma warning disable 0618
            foreach (var al in Object.FindObjectsOfType<AudioListener>(true))
            {
                if (al != null && al.gameObject != camObj)
                {
                    al.enabled = false;
                }
            }
#pragma warning restore 0618
            AudioListener surfaceListener = camObj.GetComponent<AudioListener>();
            if (surfaceListener == null) surfaceListener = camObj.AddComponent<AudioListener>();
            surfaceListener.enabled = true;

            // Add Astronaut Night Exploration Flashlight Spot Light
            GameObject flashLightObj = new GameObject("AstronautFlashlight");
            flashLightObj.transform.SetParent(camObj.transform, false);
            astronautFlashlight = flashLightObj.AddComponent<Light>();
            astronautFlashlight.type = LightType.Spot;
            astronautFlashlight.range = 65f;
            astronautFlashlight.spotAngle = 60f;
            astronautFlashlight.intensity = 2.8f;
            astronautFlashlight.color = new Color(1.0f, 0.95f, 0.85f);
            astronautFlashlight.enabled = false; // Turned on automatically during night hours

            // Setup Player Audio Source & Generate Procedural Sound Effects (Walk, Sprint, Jump)
            playerAudioSource = playerObj.AddComponent<AudioSource>();
            playerAudioSource.playOnAwake = false;
            playerAudioSource.spatialBlend = 0f; // Crisp 2D Player Audio

            walkStepClip = CreateMinecraftWalkAudioClip();
            sprintStepClip = CreateMinecraftRunAudioClip();
            jumpClip = CreateMinecraftJumpAudioClip();

            // Hide Proximity Canvas if active
            GameObject proxCanvas = GameObject.Find("ProximityUICanvas");
            if (proxCanvas != null) proxCanvas.SetActive(false);

            // 6.5 Spawn Sci-Fi AI Companion Robot (ASTRO-BOT-9)
            GameObject robotObj = new GameObject("ASTRO-BOT-Companion");
            robotObj.transform.SetParent(surfaceRoot.transform);
            robotObj.transform.position = playerObj.transform.position + new Vector3(5.5f, 0.5f, 3.5f);
            companion = robotObj.AddComponent<RobotCompanion>();
            companion.SetTarget(playerObj.transform);

            // 6.5b Spawn 3D Minecraft Villager Surface Companion (Appears when landing into the planet!)
            GameObject villagerObj = new GameObject("MinecraftVillagerSurfaceCompanion");
            villagerObj.transform.SetParent(surfaceRoot.transform);
            MinecraftVillager villager = villagerObj.AddComponent<MinecraftVillager>();
            villager.SpawnOnSurface(body.bodyName, playerObj.transform);

            // 6.6 Spawn Alien UFO Flying Saucer
            CreateSpaceship(surfaceRoot.transform, body.bodyName);

            // 7. Create NASA Telemetry Surface HUD UI (Disabled)
            currentTelemetryStr = nasaTelemetryStr;
            // BuildSurfaceHUD(body, nasaTelemetryStr);
        }

        public static float GetTerrainHeight(float posX, float posZ, string planetName)
        {
            int hash = Mathf.Abs(planetName.GetHashCode());
            float seedX = 10f + (hash % 80);
            float seedZ = 10f + ((hash * 31) % 80);

            float heightScale = 6.0f;
            if (planetName == "Moon" || planetName == "Mercury") heightScale = 9.0f;
            else if (planetName == "Mars") heightScale = 8.0f;
            else if (planetName == "Earth") heightScale = 7.0f;

            float n1 = Mathf.PerlinNoise((posX + seedX * 100f) * 0.015f, (posZ + seedZ * 100f) * 0.015f) * heightScale;
            float n2 = Mathf.PerlinNoise((posX + seedX * 100f) * 0.05f, (posZ + seedZ * 100f) * 0.05f) * (heightScale * 0.3f);
            float posY = n1 + n2;

            float distFromCenter = Vector2.Distance(new Vector2(posX, posZ), Vector2.zero);
            if (distFromCenter < 30f)
            {
                posY *= (distFromCenter / 30f);
            }
            return posY;
        }

        private void CreateProcedural3DTerrain(Transform parent, Color groundColor, Texture2D surfaceTexture, string planetName)
        {
            GameObject terrainObj = new GameObject("NASA_Procedural3DTerrain");
            terrainObj.transform.SetParent(parent, false);
            terrainObj.transform.position = Vector3.zero;

            MeshFilter mf = terrainObj.AddComponent<MeshFilter>();
            MeshRenderer mr = terrainObj.AddComponent<MeshRenderer>();
            MeshCollider mc = terrainObj.AddComponent<MeshCollider>();

            int gridSize = 120;
            float width = 3000f;
            int vertCount = (gridSize + 1) * (gridSize + 1);

            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[gridSize * gridSize * 6];

            float halfW = width * 0.5f;
            float step = width / gridSize;

            int hash = Mathf.Abs(planetName.GetHashCode());
            float seedX = 10f + (hash % 80);
            float seedZ = 10f + ((hash * 31) % 80);

            float heightScale = 6.0f;
            if (planetName == "Moon" || planetName == "Mercury") heightScale = 9.0f;
            else if (planetName == "Mars") heightScale = 8.0f;
            else if (planetName == "Earth") heightScale = 7.0f;

            for (int z = 0, i = 0; z <= gridSize; z++)
            {
                for (int x = 0; x <= gridSize; x++, i++)
                {
                    float posX = -halfW + x * step;
                    float posZ = -halfW + z * step;

                    float n1 = Mathf.PerlinNoise((posX + seedX * 100f) * 0.015f, (posZ + seedZ * 100f) * 0.015f) * heightScale;
                    float n2 = Mathf.PerlinNoise((posX + seedX * 100f) * 0.05f, (posZ + seedZ * 100f) * 0.05f) * (heightScale * 0.3f);
                    float posY = n1 + n2;

                    float distFromCenter = Vector2.Distance(new Vector2(posX, posZ), Vector2.zero);
                    if (distFromCenter < 30f)
                    {
                        posY *= (distFromCenter / 30f);
                    }

                    vertices[i] = new Vector3(posX, posY, posZ);
                    uvs[i] = new Vector2((float)x / gridSize * 30f, (float)z / gridSize * 30f);
                }
            }

            int tris = 0;
            for (int z = 0, vert = 0; z < gridSize; z++, vert++)
            {
                for (int x = 0; x < gridSize; x++, vert++)
                {
                    triangles[tris + 0] = vert;
                    triangles[tris + 1] = vert + gridSize + 1;
                    triangles[tris + 2] = vert + 1;
                    triangles[tris + 3] = vert + 1;
                    triangles[tris + 4] = vert + gridSize + 1;
                    triangles[tris + 5] = vert + gridSize + 2;
                    tris += 6;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "NASA_3DTerrainMesh";
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.mesh = mesh;
            mc.sharedMesh = mesh;

            Material groundMat = new Material(GetSurfaceShader());
            ApplyMaterialProps(groundMat, groundColor, surfaceTexture, 30f);
            groundMat.SetFloat("_Glossiness", 0.08f);
            mr.material = groundMat;
        }

        private void BuildSurfaceHUD(CelestialBody body, string telemetryStr)
        {
            hudCanvas = new GameObject("SurfaceHUDCanvas");
            hudCanvas.transform.SetParent(surfaceRoot.transform);
            Canvas canvas = hudCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.AddComponent<CanvasScaler>();
            hudCanvas.AddComponent<GraphicRaycaster>();

            GameObject panel = new GameObject("HUDHeader");
            panel.transform.SetParent(hudCanvas.transform, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.01f, 0.90f);
            rect.anchorMax = new Vector2(0.99f, 0.99f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.08f, 0.16f, 0.88f);

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.02f, 0.50f);
            titleRect.anchorMax = new Vector2(0.70f, 0.95f);
            titleRect.sizeDelta = Vector2.zero;

            planetTitleComponent = titleObj.AddComponent<Text>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            planetTitleComponent.font = font;
            planetTitleComponent.fontSize = 22;
            planetTitleComponent.fontStyle = FontStyle.Bold;
            planetTitleComponent.color = new Color(0.3f, 0.95f, 1.0f);
            planetTitleComponent.text = $"[NASA TELEMETRY LOG] {body.bodyName.ToUpper()} SURFACE EXPLORATION";

            GameObject detailsObj = new GameObject("DetailsText");
            detailsObj.transform.SetParent(panel.transform, false);
            RectTransform detailsRect = detailsObj.AddComponent<RectTransform>();
            detailsRect.anchorMin = new Vector2(0.02f, 0.08f);
            detailsRect.anchorMax = new Vector2(0.70f, 0.50f);
            detailsRect.sizeDelta = Vector2.zero;

            hudTextComponent = detailsObj.AddComponent<Text>();
            hudTextComponent.font = font;
            hudTextComponent.fontSize = 14;
            hudTextComponent.color = new Color(0.92f, 0.95f, 1.0f);
            hudTextComponent.text = $"{telemetryStr}  ||  [CONTROLS] WASD: Walk | Shift: Sprint | Space: Jump | Near UFO [E]: QCM Quiz | ESC: Auto Return to Solar System";

            // Create Interactive [🚀 RETURN TO SOLAR SYSTEM] Button
            GameObject btnObj = new GameObject("ReturnToSolarSystemBtn");
            btnObj.transform.SetParent(panel.transform, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.72f, 0.12f);
            btnRect.anchorMax = new Vector2(0.98f, 0.88f);
            btnRect.sizeDelta = Vector2.zero;

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.08f, 0.35f, 0.65f, 0.92f);

            Button returnBtn = btnObj.AddComponent<Button>();
            ColorBlock cb = returnBtn.colors;
            cb.normalColor = new Color(0.08f, 0.35f, 0.65f, 0.92f);
            cb.highlightedColor = new Color(0.18f, 0.65f, 0.98f, 1.0f);
            cb.pressedColor = new Color(0.04f, 0.20f, 0.45f, 1.0f);
            returnBtn.colors = cb;

            returnBtn.onClick.AddListener(() =>
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                ExitPlanetSurface();
            });

            GameObject btnTextObj = new GameObject("ButtonText");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;

            Text btnText = btnTextObj.AddComponent<Text>();
            btnText.font = font;
            btnText.fontSize = 14;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.fontStyle = FontStyle.Bold;
            btnText.color = new Color(0.4f, 0.98f, 1.0f);
            btnText.text = "🚀 RETURN TO SOLAR SYSTEM";
        }

        // Input System Helper Methods
        private Vector2 GetMovementInput()
        {
            float x = 0f, y = 0f;

            // 1. Check XR Simulator & VR Left Controller Thumbstick
            try
            {
                var leftHandDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
                if (leftHandDevice.isValid && leftHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 xrAxis))
                {
                    if (xrAxis.sqrMagnitude > 0.05f)
                    {
                        return xrAxis.normalized;
                    }
                }
            }
            catch {}

#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.wKey.isPressed) y += 1f;
                    if (Keyboard.current.sKey.isPressed) y -= 1f;
                    if (Keyboard.current.aKey.isPressed) x -= 1f;
                    if (Keyboard.current.dKey.isPressed) x += 1f;
                }
            }
            catch {}
#endif
            try
            {
                if (x == 0f && y == 0f)
                {
                    x = Input.GetAxisRaw("Horizontal");
                    y = Input.GetAxisRaw("Vertical");
                }
            }
            catch {}

            return new Vector2(x, y).normalized;
        }

        private Vector2 GetMouseDelta()
        {
#if ENABLE_INPUT_SYSTEM
            try { if (Mouse.current != null) return Mouse.current.delta.ReadValue() * 0.15f; } catch {}
#endif
            try { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); } catch { return Vector2.zero; }
        }

        private bool IsSprintPressed()
        {
#if ENABLE_INPUT_SYSTEM
            try { if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) return true; } catch {}
#endif
            try { return Input.GetKey(KeyCode.LeftShift); } catch { return false; }
        }

        private bool WasJumpPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            try { if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true; } catch {}
#endif
            try { return Input.GetKeyDown(KeyCode.Space); } catch { return false; }
        }

        private bool WasEscPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            try { if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true; } catch {}
#endif
            try { return Input.GetKeyDown(KeyCode.Escape); } catch { return false; }
        }

        private bool WasTKeyPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            try { if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame) return true; } catch {}
#endif
            try { return Input.GetKeyDown(KeyCode.T); } catch { return false; }
        }

        public static AudioClip CreateMinecraftWalkAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.11f; // Short punchy Minecraft block step
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            System.Random rand = new System.Random();
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;

                // 1. Initial sharp crunchy impact click (0 - 0.02s)
                float clickEnvelope = Mathf.Exp(-t * 220f);
                float crunchyNoise = (float)(rand.NextDouble() * 2.0 - 1.0);
                float bandpassClick = (crunchyNoise * 0.7f + Mathf.Sin(2f * Mathf.PI * 1400f * t) * 0.3f) * clickEnvelope;

                // 2. Low-frequency block thud body (0 - 0.11s)
                float thudEnvelope = Mathf.Exp(-t * 55f);
                float thudTone = Mathf.Sin(2f * Mathf.PI * 105f * t) * 0.8f;
                float dirtGrit = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.25f * thudEnvelope;

                samples[i] = (bandpassClick * 0.65f + (thudTone + dirtGrit) * 0.35f) * 0.5f;
            }

            AudioClip clip = AudioClip.Create("Minecraft_WalkStep", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip CreateMinecraftRunAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.08f; // Faster, punchier sprint step
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            System.Random rand = new System.Random();
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;

                // Punchier impact click & higher pitch dirt crunch
                float clickEnvelope = Mathf.Exp(-t * 260f);
                float crunchyNoise = (float)(rand.NextDouble() * 2.0 - 1.0);
                float bandpassClick = (crunchyNoise * 0.8f + Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.35f) * clickEnvelope;

                float thudEnvelope = Mathf.Exp(-t * 65f);
                float thudTone = Mathf.Sin(2f * Mathf.PI * 135f * t) * 0.85f;
                float dirtGrit = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.3f * thudEnvelope;

                samples[i] = (bandpassClick * 0.70f + (thudTone + dirtGrit) * 0.30f) * 0.6f;
            }

            AudioClip clip = AudioClip.Create("Minecraft_RunStep", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip CreateMinecraftJumpAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.16f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            System.Random rand = new System.Random();
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Signature Minecraft jump pitch sweep: 140Hz -> 240Hz -> 90Hz
                float pitchFreq = (progress < 0.3f) 
                    ? Mathf.Lerp(140f, 240f, progress / 0.3f)
                    : Mathf.Lerp(240f, 90f, (progress - 0.3f) / 0.7f);

                float envelope = Mathf.Sin(progress * Mathf.PI) * Mathf.Exp(-progress * 2.5f);
                float tone = Mathf.Sin(2f * Mathf.PI * pitchFreq * t);
                float noise = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.2f * envelope;

                samples[i] = (tone * 0.8f + noise * 0.2f) * envelope * 0.65f;
            }

            AudioClip clip = AudioClip.Create("Minecraft_Jump", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip CreateHumanJumpAudioClip()
        {
            return CreateMinecraftJumpAudioClip();
        }

        public static AudioClip CreateMinecraftHurtAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.24f; // Classic Minecraft player hurt grunt ("OOF!")
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            System.Random rand = new System.Random();
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                float attack = Mathf.Clamp01(t / 0.02f);
                float decay = Mathf.Exp(-progress * 4.0f);
                float envelope = attack * decay;

                // Vocal pitch drop: 185Hz down to 95Hz
                float pitch = Mathf.Lerp(185f, 95f, Mathf.Pow(progress, 0.6f));

                float phase = (t * pitch) % 1.0f;
                float voiceWave = (phase < 0.4f) ? (phase / 0.4f) : (1.0f - (phase - 0.4f) / 0.6f);
                voiceWave = (voiceWave * 2.0f - 1.0f);

                float formant = Mathf.Sin(2f * Mathf.PI * 650f * t) * 0.35f;
                float noise = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.25f * envelope;

                float sample = (voiceWave * (1f + formant) * 0.7f + noise * 0.3f) * envelope * 0.75f;
                samples[i] = Mathf.Clamp(sample, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("Minecraft_Hurt", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip CreateMinecraftLevelUpAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.45f; // Minecraft Experience Level-Up 5-note chime
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            // Minecraft level up notes: F4 (349Hz), G4 (392Hz), A4 (440Hz), C5 (523Hz), E5 (659Hz)
            float[] frequencies = new float[] { 349.23f, 392.00f, 440.00f, 523.25f, 659.25f };
            float noteDuration = 0.09f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Clamp((int)(t / noteDuration), 0, frequencies.Length - 1);
                float noteTime = t - (noteIndex * noteDuration);
                float freq = frequencies[noteIndex];

                float noteEnvelope = Mathf.Exp(-noteTime * 28f);
                float tone1 = Mathf.Sin(2f * Mathf.PI * freq * t);
                float tone2 = Mathf.Sin(2f * Mathf.PI * (freq * 2.0f) * t) * 0.35f;

                samples[i] = (tone1 + tone2) * noteEnvelope * 0.40f;
            }

            AudioClip clip = AudioClip.Create("Minecraft_LevelUp", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip CreateMinecraftBlockPlaceAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.14f; // Minecraft solid block placement thud
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            System.Random rand = new System.Random();
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;

                float envelope = Mathf.Exp(-t * 60f);
                float click = (float)(rand.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 300f) * 0.8f;
                float thud = Mathf.Sin(2f * Mathf.PI * 95f * t) * envelope * 0.7f;

                samples[i] = (click + thud) * 0.55f;
            }

            AudioClip clip = AudioClip.Create("Minecraft_BlockPlace", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private bool WasEKeyPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            try { if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true; } catch {}
#endif
            try { return Input.GetKeyDown(KeyCode.E); } catch { return false; }
        }


        private void CreateSpaceship(Transform parent, string planetName)
        {
            float shipZ = -50f; // Positioned 50 meters behind player spawn
            float shipX = 0f;
            float groundY = GetTerrainHeight(shipX, shipZ, planetName);
            float hoverHeight = 75f; // Fly high 75 meters in the upper sky, soaring high above the forest and clouds!
            spaceshipPosition = new Vector3(shipX, groundY + hoverHeight, shipZ);

            spaceshipObj = new GameObject("Alien_UFO_FlyingSaucer");
            spaceshipObj.transform.SetParent(parent, false);
            spaceshipObj.transform.position = spaceshipPosition;
            spaceshipObj.transform.rotation = Quaternion.identity;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");

            Texture2D hullTex = CreateSolidColorTexture(new Color(0.18f, 0.22f, 0.30f));
            Texture2D cyanTex = CreateSolidColorTexture(new Color(0.1f, 0.95f, 1.0f));
            Texture2D darkTex = CreateSolidColorTexture(new Color(0.05f, 0.07f, 0.10f));

            // UFO Premium Alien Materials (Dark Metallic Saucer Hull & Glowing Cyan Energy Dome)
            Material metallicHullMat = new Material(litShader);
            Color hullCol = new Color(0.18f, 0.22f, 0.30f, 1.0f);
            ApplyMaterialProps(metallicHullMat, hullCol, hullTex);
            if (metallicHullMat.HasProperty("_Smoothness")) metallicHullMat.SetFloat("_Smoothness", 0.92f);
            if (metallicHullMat.HasProperty("_Metallic")) metallicHullMat.SetFloat("_Metallic", 0.95f);

            Material cyanEnergyMat = new Material(unlitShader);
            Color cyanCol = new Color(0.1f, 0.95f, 1.0f, 0.95f);
            ApplyMaterialProps(cyanEnergyMat, cyanCol, cyanTex);
            if (cyanEnergyMat.HasProperty("_EmissionColor"))
            {
                cyanEnergyMat.EnableKeyword("_EMISSION");
                cyanEnergyMat.SetColor("_EmissionColor", new Color(0.1f, 0.95f, 1.0f) * 3.5f); // Bright neon cyan glow!
            }

            Material darkPlateMat = new Material(litShader);
            Color darkCol = new Color(0.05f, 0.07f, 0.10f, 1.0f);
            ApplyMaterialProps(darkPlateMat, darkCol, darkTex);
            if (darkPlateMat.HasProperty("_Smoothness")) darkPlateMat.SetFloat("_Smoothness", 0.85f);
            if (darkPlateMat.HasProperty("_Metallic")) darkPlateMat.SetFloat("_Metallic", 0.90f);

            // 1. Generate Smooth Continuous 3D Flying Saucer UFO Mesh
            CreateSmoothUFOMesh(spaceshipObj.transform, metallicHullMat, cyanEnergyMat, darkPlateMat);

            // 3. Perimeter Alien Plasma Light Nodes (12 Glowing Nodes around Saucer Edge)
            int nodeCount = 12;
            for (int i = 0; i < nodeCount; i++)
            {
                float angle = i * (360f / nodeCount);
                float rad = angle * Mathf.Deg2Rad;
                Vector3 nodePos = new Vector3(Mathf.Cos(rad) * 8.4f, 0.15f, Mathf.Sin(rad) * 8.4f);

                GameObject node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                node.name = "UFO_PlasmaNode_" + i;
                node.transform.SetParent(spaceshipObj.transform, false);
                node.transform.localPosition = nodePos;
                node.transform.localScale = Vector3.one * 0.55f;
                Destroy(node.GetComponent<Collider>());
                node.GetComponent<Renderer>().material = cyanEnergyMat;
            }

            // 4. Underside Long Anti-Gravity Tractor Beam (Downward Volumetric Energy Cone to Ground)
            GameObject tractorBeam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tractorBeam.name = "UFO_AntiGravityTractorBeam";
            tractorBeam.transform.SetParent(spaceshipObj.transform, false);
            tractorBeam.transform.localPosition = new Vector3(0f, -hoverHeight * 0.5f, 0f);
            tractorBeam.transform.localScale = new Vector3(12.0f, hoverHeight * 0.5f, 12.0f);
            Destroy(tractorBeam.GetComponent<Collider>());

            Renderer beamRen = tractorBeam.GetComponent<Renderer>();
            Material beamMat = new Material(unlitShader);
            Color beamCol = new Color(0.2f, 0.95f, 1.0f, 0.35f);
            beamMat.color = beamCol;
            if (beamMat.HasProperty("_BaseColor")) beamMat.SetColor("_BaseColor", beamCol);
            if (beamMat.HasProperty("_Surface")) beamMat.SetFloat("_Surface", 1f);
            beamMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            beamMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive glowing tractor beam!
            beamMat.SetInt("_ZWrite", 0);
            beamMat.renderQueue = 3000;
            beamRen.material = beamMat;

            // Downward Anti-Gravity Spot Light onto Ground
            GameObject spotLightObj = new GameObject("UFO_TractorSpotLight");
            spotLightObj.transform.SetParent(spaceshipObj.transform, false);
            spotLightObj.transform.localPosition = new Vector3(0f, -1.0f, 0f);
            spotLightObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Pointing straight down
            Light spotLight = spotLightObj.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.range = hoverHeight + 25f;
            spotLight.spotAngle = 65f;
            spotLight.intensity = 15.0f;
            spotLight.color = new Color(0.2f, 0.95f, 1.0f);

            // Ground Energy Impact Ring at Surface Level
            GameObject glowDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glowDisc.name = "TractorGroundImpactDisc";
            glowDisc.transform.SetParent(spaceshipObj.transform, false);
            glowDisc.transform.localPosition = new Vector3(0f, -hoverHeight + 0.05f, 0f);
            glowDisc.transform.localScale = new Vector3(24f, 0.02f, 24f);
            Destroy(glowDisc.GetComponent<Collider>());

            Renderer glowRen = glowDisc.GetComponent<Renderer>();
            glowRen.material = beamMat;
        }

        private static GameObject CreateSmoothUFOMesh(Transform parent, Material hullMat, Material energyMat, Material darkMat)
        {
            GameObject ufo = new GameObject("SpaceX_UFO_SmoothSaucerMesh");
            ufo.transform.SetParent(parent, false);

            MeshFilter mf = ufo.AddComponent<MeshFilter>();
            MeshRenderer mr = ufo.AddComponent<MeshRenderer>();

            int radialSteps = 48; // Smooth 360-degree circle
            System.Collections.Generic.List<Vector3> profile = new System.Collections.Generic.List<Vector3>();

            // 1. Underside Core Cap (y: -1.2 to -0.8)
            for (int i = 0; i <= 6; i++)
            {
                float t = i / 6f;
                float y = Mathf.Lerp(-1.2f, -0.8f, t);
                float r = 2.5f * Mathf.Sqrt(Mathf.Clamp01(1f - Mathf.Pow((y + 0.8f) / 0.4f, 2f)));
                profile.Add(new Vector3(r, y, 0f));
            }

            // 2. Lower Saucer Swept Disc (y: -0.8 to 0.0)
            for (int i = 1; i <= 10; i++)
            {
                float t = i / 10f;
                float y = Mathf.Lerp(-0.8f, 0.0f, t);
                float r = Mathf.Lerp(2.5f, 8.5f, Mathf.Pow(t, 0.7f));
                profile.Add(new Vector3(r, y, 0f));
            }

            // 3. Outer Rim Edge (y: 0.0 to 0.3)
            for (int i = 1; i <= 4; i++)
            {
                float t = i / 4f;
                float y = Mathf.Lerp(0.0f, 0.3f, t);
                float r = 8.5f;
                profile.Add(new Vector3(r, y, 0f));
            }

            // 4. Upper Saucer Slope (y: 0.3 to 1.1)
            for (int i = 1; i <= 10; i++)
            {
                float t = i / 10f;
                float y = Mathf.Lerp(0.3f, 1.1f, t);
                float r = Mathf.Lerp(8.5f, 3.5f, Mathf.Pow(t, 0.8f));
                profile.Add(new Vector3(r, y, 0f));
            }

            // 5. Top Energy Cockpit Dome (y: 1.1 to 2.6)
            for (int i = 1; i <= 12; i++)
            {
                float t = i / 12f;
                float y = Mathf.Lerp(1.1f, 2.6f, t);
                float r = 3.5f * Mathf.Sqrt(Mathf.Clamp01(1f - Mathf.Pow((y - 1.1f) / 1.5f, 2f)));
                profile.Add(new Vector3(r, y, 0f));
            }

            int numP = profile.Count;
            int vertCount = numP * (radialSteps + 1);
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];

            for (int p = 0; p < numP; p++)
            {
                float r = profile[p].x;
                float y = profile[p].y;
                float v = (float)p / (numP - 1);

                for (int rIndex = 0; rIndex <= radialSteps; rIndex++)
                {
                    float u = (float)rIndex / radialSteps;
                    float angle = u * Mathf.PI * 2f;

                    float vx = Mathf.Cos(angle) * r;
                    float vz = Mathf.Sin(angle) * r;

                    int vertIndex = p * (radialSteps + 1) + rIndex;
                    vertices[vertIndex] = new Vector3(vx, y, vz);
                    uvs[vertIndex] = new Vector2(u, v);
                }
            }

            // Submesh 0 (Hull), Submesh 1 (Energy Dome top), Submesh 2 (Bottom Core)
            System.Collections.Generic.List<int> trisHull = new System.Collections.Generic.List<int>();
            System.Collections.Generic.List<int> trisEnergy = new System.Collections.Generic.List<int>();
            System.Collections.Generic.List<int> trisBottomCore = new System.Collections.Generic.List<int>();

            int bottomCoreCutoffP = 6;
            int energyDomeStartP = 30;

            for (int p = 0; p < numP - 1; p++)
            {
                var targetTriList = (p < bottomCoreCutoffP) ? trisBottomCore :
                                    (p >= energyDomeStartP) ? trisEnergy : trisHull;

                for (int rIndex = 0; rIndex < radialSteps; rIndex++)
                {
                    int current = p * (radialSteps + 1) + rIndex;
                    int next = current + (radialSteps + 1);

                    targetTriList.Add(current);
                    targetTriList.Add(next);
                    targetTriList.Add(current + 1);

                    targetTriList.Add(current + 1);
                    targetTriList.Add(next);
                    targetTriList.Add(next + 1);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "SmoothUFOSaucerMesh";
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.subMeshCount = 3;
            mesh.SetTriangles(trisHull.ToArray(), 0);
            mesh.SetTriangles(trisEnergy.ToArray(), 1);
            mesh.SetTriangles(trisBottomCore.ToArray(), 2);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.mesh = mesh;
            SphereCollider sphereCol = ufo.AddComponent<SphereCollider>();
            sphereCol.radius = 8.5f;
            sphereCol.center = new Vector3(0f, 0.5f, 0f);
            mr.materials = new Material[] { hullMat, energyMat, darkMat };

            return ufo;
        }

        private void CreateEarthBiosphere(Transform parent)
        {
            // 1. Sparkling Blue Water Ocean Lake at Sea Level
            GameObject oceanObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            oceanObj.name = "Earth_RealisticOcean";
            oceanObj.transform.SetParent(parent, false);
            oceanObj.transform.position = new Vector3(0f, 0.4f, 0f); // Lower sea level to prevent terrain clipping
            oceanObj.transform.localScale = new Vector3(300f, 1f, 300f); // 3km ocean plane
            Destroy(oceanObj.GetComponent<Collider>());

            Renderer oceanRen = oceanObj.GetComponent<Renderer>();
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            Material oceanMat = new Material(unlitShader);
            Color oceanCol = new Color(0.08f, 0.42f, 0.85f, 0.82f);
            ApplyMaterialProps(oceanMat, oceanCol);
            
            if (oceanMat.HasProperty("_Glossiness")) oceanMat.SetFloat("_Glossiness", 0.95f);
            if (oceanMat.HasProperty("_Smoothness")) oceanMat.SetFloat("_Smoothness", 0.95f);
            if (oceanMat.HasProperty("_Metallic")) oceanMat.SetFloat("_Metallic", 0.1f);
            
            oceanMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            oceanMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            oceanMat.SetInt("_ZWrite", 0);
            oceanMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            oceanMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            oceanRen.material = oceanMat;

            // 2. Procedural 3D Forest & Trees
            UnityEngine.Random.InitState(12345);
            for (int t = 0; t < 220; t++)
            {
                float tx = UnityEngine.Random.Range(-1100f, 1100f);
                float tz = UnityEngine.Random.Range(-1100f, 1100f);

                // Keep player spawn & ship landing clear
                if (Vector2.Distance(new Vector2(tx, tz), Vector2.zero) < 18f) continue;
                if (Vector2.Distance(new Vector2(tx, tz), new Vector2(0f, -50f)) < 30f) continue;

                float groundY = GetTerrainHeight(tx, tz, "Earth");
                // Only spawn trees on land above water level
                if (groundY > 1.8f)
                {
                    float treeScale = UnityEngine.Random.Range(1.8f, 4.2f);
                    CreateCartoonOakTree(parent, new Vector3(tx, groundY, tz), treeScale);
                }
            }

            // 3. Realistic 3D Organic Cumulus Cloud Formations (High Altitude & Drift)
            cloudMaterials.Clear();
            skyCloudsGroupObj = new GameObject("Earth_SkyClouds");
            skyCloudsGroupObj.transform.SetParent(parent, false);

            for (int cluster = 0; cluster < 16; cluster++)
            {
                float cx = UnityEngine.Random.Range(-1100f, 1100f);
                float cy = UnityEngine.Random.Range(340f, 480f); // High up in sky!
                float cz = UnityEngine.Random.Range(-1100f, 1100f);

                int puffCount = UnityEngine.Random.Range(5, 8);
                for (int p = 0; p < puffCount; p++)
                {
                    GameObject cloudObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    cloudObj.name = $"SkyCloudCluster_{cluster}_Puff_{p}";
                    cloudObj.transform.SetParent(skyCloudsGroupObj.transform, false);

                    Vector3 puffOffset = new Vector3(
                        (p == 0) ? 0f : UnityEngine.Random.Range(-50f, 50f),
                        (p == 0) ? 0f : UnityEngine.Random.Range(-12f, 18f),
                        (p == 0) ? 0f : UnityEngine.Random.Range(-50f, 50f)
                    );
                    cloudObj.transform.position = new Vector3(cx, cy, cz) + puffOffset;
                    float scaleW = UnityEngine.Random.Range(55f, 110f);
                    float scaleH = UnityEngine.Random.Range(28f, 48f);
                    cloudObj.transform.localScale = new Vector3(scaleW, scaleH, scaleW);
                    Destroy(cloudObj.GetComponent<Collider>());

                    Renderer cloudRen = cloudObj.GetComponent<Renderer>();
                    Shader cloudShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                    Material cloudMat = new Material(cloudShader);
                    Color cloudCol = new Color(0.98f, 0.99f, 1.0f, 0.88f);
                    ApplyMaterialProps(cloudMat, cloudCol);

                    if (cloudMat.HasProperty("_Surface")) cloudMat.SetFloat("_Surface", 1);
                    cloudMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    cloudMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    cloudMat.SetInt("_ZWrite", 0);
                    cloudMat.renderQueue = 3000;
                    cloudRen.material = cloudMat;

                    cloudMaterials.Add(cloudMat);
                }
            }

            // 4. Wildflower Fields & Grass Vegetation
            for (int f = 0; f < 140; f++)
            {
                float fx = UnityEngine.Random.Range(-350f, 350f);
                float fz = UnityEngine.Random.Range(-350f, 350f);

                float groundY = GetTerrainHeight(fx, fz, "Earth");
                if (groundY > 2.0f)
                {
                    GameObject flowerObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    flowerObj.name = "WildflowerPatch";
                    flowerObj.transform.SetParent(parent, false);
                    flowerObj.transform.position = new Vector3(fx, groundY + 0.3f, fz);
                    flowerObj.transform.localScale = new Vector3(UnityEngine.Random.Range(0.6f, 1.2f), UnityEngine.Random.Range(0.4f, 0.8f), UnityEngine.Random.Range(0.6f, 1.2f));
                    Destroy(flowerObj.GetComponent<Collider>());

                    Renderer flowerRen = flowerObj.GetComponent<Renderer>();
                    Material flowerMat = new Material(GetSurfaceShader());
                    Color[] flowerColors = new Color[] {
                        new Color(1.0f, 0.85f, 0.20f), // Golden Yellow
                        new Color(0.35f, 0.70f, 1.0f), // Sky Blue
                        new Color(0.98f, 0.98f, 1.0f), // White Daisy
                        new Color(0.95f, 0.40f, 0.70f)  // Pink Blossom
                    };
                    Color fCol = flowerColors[UnityEngine.Random.Range(0, flowerColors.Length)];
                    ApplyMaterialProps(flowerMat, fCol);
                    flowerRen.material = flowerMat;
                }
            }
        }

        private void CreateCartoonOakTree(Transform parent, Vector3 pos, float scale)
        {
            GameObject treeObj = new GameObject("CartoonOakTree");
            treeObj.transform.SetParent(parent, false);
            treeObj.transform.position = pos;

            Color trunkColor = new Color(0.48f, 0.32f, 0.18f); // Warm Oak Brown Bark from Reference

            // 1. Wide Flared Base Roots extending deep into ground (Y = -scale * 0.2f)
            GameObject baseRoot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseRoot.name = "RootBase";
            baseRoot.transform.SetParent(treeObj.transform, false);
            baseRoot.transform.localPosition = new Vector3(0f, -scale * 0.2f, 0f);
            baseRoot.transform.localScale = new Vector3(scale * 0.75f, scale * 0.5f, scale * 0.75f);

            Renderer baseRen = baseRoot.GetComponent<Renderer>();
            Material baseMat = new Material(GetSurfaceShader());
            ApplyMaterialProps(baseMat, trunkColor);
            baseRen.material = baseMat;

            // 2. Main Thick Central Trunk (extends down into ground from Y = -scale * 0.4f)
            GameObject mainTrunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mainTrunk.name = "MainTrunk";
            mainTrunk.transform.SetParent(treeObj.transform, false);
            float trunkH = scale * 1.4f;
            float trunkW = scale * 0.38f;
            mainTrunk.transform.localPosition = new Vector3(0f, trunkH * 0.75f, 0f);
            mainTrunk.transform.localScale = new Vector3(trunkW, trunkH * 1.25f, trunkW);

            Renderer trunkRen = mainTrunk.GetComponent<Renderer>();
            Material trunkMat = new Material(GetSurfaceShader());
            ApplyMaterialProps(trunkMat, trunkColor);
            trunkRen.material = trunkMat;

            // 3. V-Shaped Forked Main Branches (Left & Right Arms)
            for (int b = -1; b <= 1; b += 2)
            {
                GameObject branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                branch.name = "ForkBranch_" + b;
                branch.transform.SetParent(treeObj.transform, false);
                branch.transform.localPosition = new Vector3(b * scale * 0.35f, trunkH * 1.8f, 0f);
                branch.transform.localRotation = Quaternion.Euler(0f, 0f, b * -28f); // Angled outward fork!
                branch.transform.localScale = new Vector3(scale * 0.22f, scale * 0.9f, scale * 0.22f);

                Renderer branchRen = branch.GetComponent<Renderer>();
                Material branchMat = new Material(GetSurfaceShader());
                ApplyMaterialProps(branchMat, trunkColor);
                branchRen.material = branchMat;
            }

            // 4. Big Fluffy Cloud Canopy (10 Overlapping Lush Green Sphere Puffs forming Oak Crown)
            Color[] oakGreenTones = new Color[] {
                new Color(0.24f, 0.62f, 0.24f), // Vibrant Oak Green
                new Color(0.28f, 0.68f, 0.28f), // Bright Emerald Leaf
                new Color(0.32f, 0.72f, 0.30f), // Lush Green Puff
                new Color(0.20f, 0.55f, 0.20f), // Shadow Leaf Green
                new Color(0.36f, 0.76f, 0.32f)  // Sunlit Soft Leaf
            };

            Vector3 canopyCenter = new Vector3(0f, trunkH * 2.3f, 0f);

            Vector3[] puffOffsets = new Vector3[] {
                new Vector3(0f, 0.4f, 0f),                      // Top Center Puff
                new Vector3(-scale * 0.6f, 0.1f, 0f),           // Left Puff
                new Vector3(scale * 0.6f, 0.1f, 0f),            // Right Puff
                new Vector3(0f, -0.2f, scale * 0.5f),           // Front Puff
                new Vector3(0f, -0.2f, -scale * 0.5f),          // Back Puff
                new Vector3(-scale * 0.4f, scale * 0.5f, 0.2f), // Top Left Puff
                new Vector3(scale * 0.4f, scale * 0.5f, -0.2f), // Top Right Puff
                new Vector3(-scale * 0.75f, -0.3f, 0.1f),       // Bottom Left Fluff
                new Vector3(scale * 0.75f, -0.3f, -0.1f),      // Bottom Right Fluff
                new Vector3(0f, scale * 0.75f, 0f)              // Very Top Crown Peak
            };

            for (int p = 0; p < puffOffsets.Length; p++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "OakCanopyPuff_" + p;
                puff.transform.SetParent(treeObj.transform, false);
                Destroy(puff.GetComponent<Collider>()); // Fast performance: Destroy leaf colliders!

                puff.transform.localPosition = canopyCenter + puffOffsets[p];
                float puffSize = scale * UnityEngine.Random.Range(1.1f, 1.6f);
                puff.transform.localScale = new Vector3(puffSize, puffSize * 0.9f, puffSize);

                Renderer puffRen = puff.GetComponent<Renderer>();
                Material puffMat = new Material(GetSurfaceShader());
                Color pCol = oakGreenTones[p % oakGreenTones.Length];
                ApplyMaterialProps(puffMat, pCol);
                puffRen.material = puffMat;
            }
        }

        private void CreateRealisticForestTree(Transform parent, Vector3 pos, float scale)
        {
            GameObject treeObj = new GameObject("RealisticForestTree");
            treeObj.transform.SetParent(parent, false);
            treeObj.transform.position = pos;
            treeObj.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-4f, 4f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-4f, 4f));

            // Slender Dark Bark Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(treeObj.transform, false);
            float trunkHeight = scale * 2.2f;
            float trunkWidth = scale * 0.12f;
            trunk.transform.localPosition = new Vector3(0f, trunkHeight, 0f);
            trunk.transform.localScale = new Vector3(trunkWidth, trunkHeight, trunkWidth);

            Renderer trunkRen = trunk.GetComponent<Renderer>();
            Material trunkMat = new Material(GetSurfaceShader());
            ApplyMaterialProps(trunkMat, new Color(0.18f, 0.12f, 0.08f)); // Dark Wood Bark from Reference Photo
            trunkRen.material = trunkMat;

            // Organic Leaf Branch Clusters (5 Offset Clusters forming natural leafy canopy cover)
            Color[] leafTones = new Color[] {
                new Color(0.12f, 0.38f, 0.14f), // Dark Forest Shadow Green
                new Color(0.18f, 0.52f, 0.20f), // Emerald Green
                new Color(0.25f, 0.62f, 0.28f), // Sunlit Leaf Green
                new Color(0.35f, 0.68f, 0.22f), // Lime Gold Leaf
                new Color(0.15f, 0.44f, 0.18f)  // Moss Green
            };

            int clusterCount = 5;
            for (int c = 0; c < clusterCount; c++)
            {
                GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                canopy.name = "FoliageCluster_" + c;
                canopy.transform.SetParent(treeObj.transform, false);
                Destroy(canopy.GetComponent<Collider>()); // Fast performance: Destroy leaf colliders!

                float heightRatio = 0.65f + (float)c / clusterCount * 0.55f;
                float offsetY = trunkHeight * 2f * heightRatio;
                float offsetX = (c == 0) ? 0f : UnityEngine.Random.Range(-scale * 0.45f, scale * 0.45f);
                float offsetZ = (c == 0) ? 0f : UnityEngine.Random.Range(-scale * 0.45f, scale * 0.45f);

                float clusterW = scale * UnityEngine.Random.Range(0.8f, 1.4f);
                float clusterH = scale * UnityEngine.Random.Range(0.7f, 1.2f);

                canopy.transform.localPosition = new Vector3(offsetX, offsetY, offsetZ);
                canopy.transform.localScale = new Vector3(clusterW, clusterH, clusterW);

                Renderer canopyRen = canopy.GetComponent<Renderer>();
                Material canopyMat = new Material(GetSurfaceShader());
                Color col = leafTones[UnityEngine.Random.Range(0, leafTones.Length)];
                ApplyMaterialProps(canopyMat, col);
                canopyRen.material = canopyMat;
            }
        }

        private void CreateFallenLog(Transform parent, Vector3 pos, float scale)
        {
            GameObject logObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            logObj.name = "FallenLog";
            logObj.transform.SetParent(parent, false);
            logObj.transform.position = pos + new Vector3(0f, 0.2f, 0f);
            logObj.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(85f, 95f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-5f, 5f));
            logObj.transform.localScale = new Vector3(scale * 0.18f, scale * 1.8f, scale * 0.18f);

            Renderer ren = logObj.GetComponent<Renderer>();
            Material mat = new Material(GetSurfaceShader());
            ApplyMaterialProps(mat, new Color(0.16f, 0.11f, 0.07f));
            ren.material = mat;
        }

        private void CreateAtmosphericSkyDome(Transform parent, Color topCol, Color horizonCol, Color sunCol)
        {
            GameObject skyDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skyDome.name = "NASA_AtmosphericSkyDome";
            skyDome.transform.SetParent(parent, false);
            skyDome.transform.position = Vector3.zero;
            skyDome.transform.localScale = Vector3.one * 8000f; // Enclose entire 3km surface area
            Destroy(skyDome.GetComponent<Collider>());

            Renderer ren = skyDome.GetComponent<Renderer>();
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
            Material skyMat = new Material(unlitShader);

            Texture2D skyTex = CreateAtmosphericSkyTexture(topCol, horizonCol);
            ApplyMaterialProps(skyMat, Color.white, skyTex);

            if (skyMat.HasProperty("_Cull")) skyMat.SetInt("_Cull", 0); // Render sky texture inside sky dome!
            if (skyMat.HasProperty("_CullMode")) skyMat.SetInt("_CullMode", 0);

            ren.material = skyMat;
            skyDomeObj = skyDome;
        }

        private static Texture2D CreateAtmosphericSkyTexture(Color topCol, Color horizonCol)
        {
            int w = 512, h = 512;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
            Color[] cols = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / h; // 0 = horizon, 1 = zenith
                for (int x = 0; x < w; x++)
                {
                    // Clean atmospheric sky gradient from horizon to zenith (3D sun rendered separately in space)
                    Color c = Color.Lerp(horizonCol, topCol, Mathf.Pow(v, 0.45f));
                    cols[y * w + x] = c;
                }
            }

            tex.SetPixels(cols);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSolidColorTexture(Color col)
        {
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color[] cols = new Color[16 * 16];
            for (int i = 0; i < cols.Length; i++) cols[i] = col;
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }
    }
}
