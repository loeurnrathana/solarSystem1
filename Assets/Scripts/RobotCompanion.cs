using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SolarSystemScope
{
    public class RobotCompanion : MonoBehaviour
    {
        public enum RobotState { Follow, OrbitPlayer, Scanning, Emote }

        [Header("Target & Movement Settings")]
        public Transform playerTarget;
        public float followDistance = 6.5f;
        public float followAngleOffset = 45f; // Side-by-side offset angle
        public float moveSpeed = 8f;
        public float sprintSpeed = 16f;
        public float rotationSpeed = 8f;
        public float terrainOffset = 0.42f;

        [Header("Random Wander Settings")]
        public float wanderInterval = 2.0f; // Pick a new spot every 2 seconds
        private float wanderTimer = 0f;
        private Vector3 currentWanderOffset;

        [Header("State")]
        public RobotState currentState = RobotState.Follow;
        private string currentPlanetName = "";
        private float orbitAngle = 0f;
        private float stateTimer = 0f;
        private float scanAngle = 0f;

        [Header("Procedural Visual Parts")]
        private Transform headTransform;
        private Transform visorTransform;
        private Transform antennaTransform;
        private Transform scannerBeamTransform;
        private Light scannerLight;
        private Light antennaLight;
        private Material visorMat;
        private Material scannerMat;
        private Transform[] legs = new Transform[4];
        private Vector3[] legOriginalPos = new Vector3[4];
        private Text hudStatusText;
        private GameObject nameplateCanvas;

        // Animation & Walk Cycle Variables
        private float walkCycleTimer = 0f;
        private Vector3 lastPosition;
        private float currentSpeed = 0f;
        private bool isGrounded = true;

        // Audio & SFX Variables
        private AudioSource robotAudioSource;
        private AudioClip robotWalkClip;
        private AudioClip robotSprintClip;
        private AudioClip robotHopClip;
        private float robotStepTimer = 0f;

        // Speech & Dialogue Variables
        private GameObject speechCanvas;
        private Text speechText;
        private Image speechBgImage;
        private Coroutine speechCoroutine;

        private void Awake()
        {
            lastPosition = transform.position;
        }

        private void Start()
        {
            // Find player if target not assigned
            if (playerTarget == null)
            {
                GameObject p = GameObject.Find("NASA_AstronautPlayer");
                if (p != null) playerTarget = p.transform;
                else if (Camera.main != null) playerTarget = Camera.main.transform;
            }

            // Create Robot Visuals if child hierarchy doesn't exist
            if (transform.childCount == 0)
            {
                BuildProceduralRobotMesh();
            }

            // Setup 3D Spatialized Audio Source & Metallic SFX Clips
            robotAudioSource = gameObject.AddComponent<AudioSource>();
            robotAudioSource.playOnAwake = false;
            robotAudioSource.spatialBlend = 1.0f; // 3D spatial audio
            robotAudioSource.maxDistance = 35f;

            robotWalkClip = CreateMetallicStepClip(1.0f);
            robotSprintClip = CreateMetallicStepClip(1.5f);
            robotHopClip = PlanetSurfaceExplorer.CreateMinecraftJumpAudioClip();
        }

        public void SpeakWelcomeMessage(string planetName)
        {
            currentPlanetName = planetName;
            Speak($"Welcome to {planetName}!");
        }

        public void SetPlanetName(string name)
        {
            currentPlanetName = name;
        }

        public void SetTarget(Transform target)
        {
            playerTarget = target;
        }

        private void Update()
        {
            if (playerTarget == null)
            {
                GameObject p = GameObject.Find("NASA_AstronautPlayer");
                if (p != null) playerTarget = p.transform;
                else return;
            }

            HandleInputCommands();

            // Calculate current speed
            currentSpeed = (transform.position - lastPosition).magnitude / Mathf.Max(Time.deltaTime, 0.001f);
            lastPosition = transform.position;

            // Execute current state machine
            switch (currentState)
            {
                case RobotState.Follow:
                    UpdateFollowBehavior();
                    break;
                case RobotState.OrbitPlayer:
                    UpdateOrbitBehavior();
                    break;
                case RobotState.Scanning:
                    UpdateScanBehavior();
                    break;
                case RobotState.Emote:
                    UpdateEmoteBehavior();
                    break;
            }

            // Update Procedural Walk Animation & Head Look
            AnimateLegsAndBody();
            UpdateHeadTargeting();
            UpdateVisualFX();
            UpdateNameplateUI();
        }

        private void HandleInputCommands()
        {
            // F Key: Cycle Modes
            if (WasFPressedThisFrame())
            {
                CycleNextState();
            }

            // R Key: Trigger Scan / Emote pulse
            if (WasRPressedThisFrame())
            {
                TriggerScanOrEmote();
            }
        }

        public void CycleNextState()
        {
            if (currentState == RobotState.Follow)
            {
                currentState = RobotState.OrbitPlayer;
                Speak("Mode: ORBITING. Circling perimeter, commander!");
            }
            else if (currentState == RobotState.OrbitPlayer)
            {
                currentState = RobotState.Scanning;
                Speak("Mode: SCANNING. Sensor array active!");
            }
            else
            {
                currentState = RobotState.Follow;
                Speak("Mode: FOLLOWING. Telemetry locked.");
            }

            stateTimer = 0f;
            Debug.Log($"<color=cyan>[ASTRO-BOT] State changed to: {currentState}</color>");
        }

        public void TriggerScanOrEmote()
        {
            stateTimer = 0f;
            if (currentState == RobotState.Scanning)
            {
                currentState = RobotState.Emote;
                Speak("All actuators & thrusters operational! 100% nominal!");
                if (robotAudioSource != null && robotHopClip != null) robotAudioSource.PlayOneShot(robotHopClip, 0.65f);
            }
            else
            {
                currentState = RobotState.Scanning;
                Speak("Initiating multi-spectral surface scan...");
            }
        }

        private void UpdateFollowBehavior()
        {
            wanderTimer += Time.deltaTime;

            float distFromPlayer = Vector3.Distance(transform.position, playerTarget.position);

            // Pick a new random spot around player every ~2 seconds or if player moved far away
            if (wanderTimer >= wanderInterval || distFromPlayer > 10f || currentWanderOffset == Vector3.zero)
            {
                PickNewRandomWanderOffset();
                wanderTimer = 0f;
            }

            Vector3 desiredPos = playerTarget.position + currentWanderOffset;
            desiredPos.y = GetTerrainHeightAt(desiredPos.x, desiredPos.z) + terrainOffset;

            float distToTarget = Vector3.Distance(transform.position, desiredPos);

            if (distToTarget > 0.2f)
            {
                float targetSpeed = distFromPlayer > 10f ? sprintSpeed : moveSpeed;
                transform.position = Vector3.MoveTowards(transform.position, desiredPos, targetSpeed * Time.deltaTime);

                // Face movement direction
                Vector3 moveDir = (desiredPos - transform.position);
                moveDir.y = 0;
                if (moveDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                // Turn towards player while resting at waypoint
                Vector3 lookDir = playerTarget.position - transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * 0.5f * Time.deltaTime);
                }
            }
        }

        private void PickNewRandomWanderOffset()
        {
            if (playerTarget == null) return;
            // Pick a random angle around player (-135 to +135 degrees relative to player facing)
            float randomAngle = UnityEngine.Random.Range(-135f, 135f);
            float randomDist = UnityEngine.Random.Range(4.5f, 7.5f);

            Vector3 dir = Quaternion.Euler(0, randomAngle, 0) * playerTarget.forward;
            currentWanderOffset = dir.normalized * randomDist;

            // Random chance to speak wandering lines
            string[] wanderLines = new string[]
            {
                "New waypoint targeted! Inspecting terrain.",
                "Scanning rock formations for trace minerals.",
                "Surface elevation & atmospheric pressure nominal.",
                "Relocating to optimal survey vector.",
                "Keeping pace with commander."
            };
            if (UnityEngine.Random.value < 0.40f)
            {
                Speak(wanderLines[UnityEngine.Random.Range(0, wanderLines.Length)]);
            }
        }

        private void UpdateOrbitBehavior()
        {
            orbitAngle += 45f * Time.deltaTime; // 45 deg/sec circle speed
            if (orbitAngle > 360f) orbitAngle -= 360f;

            Vector3 orbitOffset = new Vector3(Mathf.Sin(orbitAngle * Mathf.Deg2Rad), 0, Mathf.Cos(orbitAngle * Mathf.Deg2Rad)) * followDistance;
            Vector3 desiredPos = playerTarget.position + orbitOffset;
            desiredPos.y = GetTerrainHeightAt(desiredPos.x, desiredPos.z) + terrainOffset;

            transform.position = Vector3.MoveTowards(transform.position, desiredPos, moveSpeed * Time.deltaTime);

            // Face target position direction
            Vector3 lookDir = playerTarget.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        private void UpdateScanBehavior()
        {
            stateTimer += Time.deltaTime;
            scanAngle += 120f * Time.deltaTime;

            // Turn towards player or rock target
            Vector3 lookDir = playerTarget.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), rotationSpeed * Time.deltaTime);
            }

            // Return to follow state after 6 seconds of scanning
            if (stateTimer > 6.0f)
            {
                currentState = RobotState.Follow;
            }
        }

        private void UpdateEmoteBehavior()
        {
            stateTimer += Time.deltaTime;

            // Hop / Spin emote
            float hopY = Mathf.Abs(Mathf.Sin(stateTimer * 8f)) * 0.6f;
            Vector3 currentPos = transform.position;
            float baseGroundY = GetTerrainHeightAt(currentPos.x, currentPos.z) + terrainOffset;
            transform.position = new Vector3(currentPos.x, baseGroundY + hopY, currentPos.z);

            transform.Rotate(Vector3.up, 360f * Time.deltaTime);

            if (stateTimer > 2.5f)
            {
                currentState = RobotState.Follow;
            }
        }

        private void AnimateLegsAndBody()
        {
            if (currentSpeed > 0.1f && isGrounded)
            {
                walkCycleTimer += Time.deltaTime * currentSpeed * 2.5f;

                // Play metallic footstep audio clip
                float stepInterval = currentSpeed > moveSpeed + 1.0f ? 0.26f : 0.42f;
                robotStepTimer += Time.deltaTime;
                if (robotStepTimer >= stepInterval)
                {
                    robotStepTimer = 0f;
                    if (robotAudioSource != null)
                    {
                        AudioClip clipToPlay = currentSpeed > moveSpeed + 1.0f ? robotSprintClip : robotWalkClip;
                        robotAudioSource.PlayOneShot(clipToPlay, 0.45f);
                    }
                }

                // Alternate leg step movement
                for (int i = 0; i < 4; i++)
                {
                    if (legs[i] == null) continue;
                    float legOffsetPhase = (i == 0 || i == 3) ? 0f : Mathf.PI;
                    float legLift = Mathf.Max(0, Mathf.Sin(walkCycleTimer + legOffsetPhase)) * 0.35f;
                    float legSwing = Mathf.Cos(walkCycleTimer + legOffsetPhase) * 0.25f;

                    Vector3 orig = legOriginalPos[i];
                    legs[i].localPosition = orig + new Vector3(0, legLift, legSwing);
                }

                // Subtle chassis body bobbing
                if (headTransform != null)
                {
                    float bob = Mathf.Sin(walkCycleTimer * 2f) * 0.08f;
                    headTransform.localPosition = new Vector3(0, 0.95f + bob, 0);
                }
            }
            else
            {
                // Reset legs to original stance
                for (int i = 0; i < 4; i++)
                {
                    if (legs[i] != null)
                    {
                        legs[i].localPosition = Vector3.Lerp(legs[i].localPosition, legOriginalPos[i], 10f * Time.deltaTime);
                    }
                }

                // Firm grounded stance when standing still
                if (headTransform != null)
                {
                    headTransform.localPosition = Vector3.Lerp(headTransform.localPosition, new Vector3(0, 0.95f, 0), 5f * Time.deltaTime);
                }
            }
        }

        private void UpdateHeadTargeting()
        {
            if (headTransform == null || playerTarget == null) return;

            // Head smoothly tracks the player's position
            Vector3 lookTarget = playerTarget.position + Vector3.up * 1.5f;
            Vector3 dirToTarget = (lookTarget - headTransform.position).normalized;

            if (dirToTarget != Vector3.zero)
            {
                Quaternion headRot = Quaternion.LookRotation(dirToTarget, Vector3.up);
                headTransform.rotation = Quaternion.Slerp(headTransform.rotation, headRot, 6f * Time.deltaTime);
            }
        }

        private void UpdateVisualFX()
        {
            // Visor color according to state
            if (visorMat != null)
            {
                Color visorColor = Color.cyan;
                if (currentState == RobotState.OrbitPlayer) visorColor = new Color(0.2f, 1.0f, 0.4f); // Green
                else if (currentState == RobotState.Scanning) visorColor = new Color(0.3f, 0.7f, 1.0f); // Bright Blue
                else if (currentState == RobotState.Emote) visorColor = new Color(1.0f, 0.8f, 0.1f); // Golden Yellow

                visorMat.color = visorColor;
                if (visorMat.HasProperty("_EmissionColor"))
                {
                    visorMat.EnableKeyword("_EMISSION");
                    visorMat.SetColor("_EmissionColor", visorColor * 2.5f);
                }
            }

            // Scanner Beam FX during Scanning State
            bool isScanning = (currentState == RobotState.Scanning);
            if (scannerBeamTransform != null) scannerBeamTransform.gameObject.SetActive(isScanning);
            if (scannerLight != null)
            {
                scannerLight.enabled = isScanning;
                if (isScanning)
                {
                    scannerLight.spotAngle = 45f + Mathf.Sin(scanAngle * 0.1f) * 10f;
                    scannerLight.intensity = 4.0f + Mathf.Sin(scanAngle * 0.2f) * 1.5f;
                }
            }

            // Blinking antenna light
            if (antennaLight != null)
            {
                antennaLight.intensity = Mathf.PingPong(Time.time * 3f, 1.5f) + 0.5f;
            }
        }

        private void UpdateNameplateUI()
        {
            if (nameplateCanvas == null || playerTarget == null) return;

            // Make holographic nameplate billboard towards main camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                Quaternion billboardRot = Quaternion.LookRotation(-cam.transform.forward, cam.transform.up);
                nameplateCanvas.transform.rotation = billboardRot;
                if (speechCanvas != null && speechCanvas.activeSelf)
                {
                    speechCanvas.transform.rotation = billboardRot;
                }
            }

            if (hudStatusText != null)
            {
                float dist = Vector3.Distance(transform.position, playerTarget.position);
                string modeStr = currentState.ToString().ToUpper();
                hudStatusText.text = $"[ASTRO-BOT-9]\nMODE: {modeStr} | DIST: {dist:F1}m";

                if (currentState == RobotState.Scanning) hudStatusText.color = new Color(0.3f, 0.9f, 1.0f);
                else if (currentState == RobotState.Emote) hudStatusText.color = new Color(1.0f, 0.85f, 0.2f);
                else hudStatusText.color = new Color(0.2f, 0.95f, 0.5f);
            }
        }

        private float GetTerrainHeightAt(float x, float z)
        {
            // 1. Raycast down to find ground elevation, skipping robot's own colliders
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, 200f, z), Vector3.down, 400f);
            foreach (var hit in hits)
            {
                if (hit.transform != transform && !hit.transform.IsChildOf(transform))
                {
                    return hit.point.y;
                }
            }

            // 2. Query PlanetSurfaceExplorer height formula if active
            if (PlanetSurfaceExplorer.Instance != null && !string.IsNullOrEmpty(PlanetSurfaceExplorer.Instance.CurrentPlanetName))
            {
                return PlanetSurfaceExplorer.GetTerrainHeight(x, z, PlanetSurfaceExplorer.Instance.CurrentPlanetName);
            }

            return 0f;
        }

        private void BuildProceduralRobotMesh()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");

            // Main Sci-Fi Robot Base Materials
            Material bodyMat = new Material(litShader);
            bodyMat.color = new Color(0.85f, 0.88f, 0.92f); // White titanium body
            if (bodyMat.HasProperty("_Glossiness")) bodyMat.SetFloat("_Glossiness", 0.75f);
            if (bodyMat.HasProperty("_Metallic")) bodyMat.SetFloat("_Metallic", 0.85f);

            Material darkMetalMat = new Material(litShader);
            darkMetalMat.color = new Color(0.2f, 0.22f, 0.26f); // Dark carbon metal

            visorMat = new Material(unlitShader);
            visorMat.color = Color.cyan;

            // 1. Torso / Chassis Body (Rounded Cylinder)
            GameObject bodyObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bodyObj.name = "RobotBody";
            bodyObj.transform.SetParent(transform, false);
            bodyObj.transform.localPosition = new Vector3(0, 0.6f, 0);
            bodyObj.transform.localScale = new Vector3(0.7f, 0.45f, 0.7f);
            bodyObj.GetComponent<Renderer>().material = bodyMat;

            // Core Chest Arc Reactor Light
            GameObject chestCore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            chestCore.name = "ChestCore";
            chestCore.transform.SetParent(bodyObj.transform, false);
            chestCore.transform.localPosition = new Vector3(0, 0.1f, 0.52f);
            chestCore.transform.localScale = new Vector3(0.35f, 0.35f, 0.2f);
            chestCore.GetComponent<Renderer>().material = visorMat;
            SafeDestroy(chestCore.GetComponent<Collider>());

            // 2. Robot Head Dome
            GameObject headObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headObj.name = "RobotHead";
            headObj.transform.SetParent(transform, false);
            headObj.transform.localPosition = new Vector3(0, 0.95f, 0);
            headObj.transform.localScale = new Vector3(0.6f, 0.55f, 0.6f);
            headObj.GetComponent<Renderer>().material = bodyMat;
            headTransform = headObj.transform;

            // Visor Screen / Glowing LED Eyes
            GameObject visorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visorObj.name = "RobotVisor";
            visorObj.transform.SetParent(headObj.transform, false);
            visorObj.transform.localPosition = new Vector3(0, 0.05f, 0.42f);
            visorObj.transform.localScale = new Vector3(0.75f, 0.3f, 0.25f);
            visorObj.GetComponent<Renderer>().material = visorMat;
            visorTransform = visorObj.transform;
            SafeDestroy(visorObj.GetComponent<Collider>());

            // 3. Antenna & Beacon Light
            GameObject antObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            antObj.name = "RobotAntenna";
            antObj.transform.SetParent(headObj.transform, false);
            antObj.transform.localPosition = new Vector3(0.2f, 0.55f, -0.1f);
            antObj.transform.localScale = new Vector3(0.05f, 0.35f, 0.05f);
            antObj.GetComponent<Renderer>().material = darkMetalMat;
            antennaTransform = antObj.transform;
            SafeDestroy(antObj.GetComponent<Collider>());

            GameObject antTip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            antTip.name = "AntennaTipLight";
            antTip.transform.SetParent(antObj.transform, false);
            antTip.transform.localPosition = new Vector3(0, 1.1f, 0);
            antTip.transform.localScale = new Vector3(2.0f, 0.3f, 2.0f);
            antTip.GetComponent<Renderer>().material = visorMat;
            antennaLight = antTip.AddComponent<Light>();
            antennaLight.type = LightType.Point;
            antennaLight.color = Color.cyan;
            antennaLight.range = 3f;
            antennaLight.intensity = 1.5f;
            SafeDestroy(antTip.GetComponent<Collider>());

            // 4. Scanner Hologram Laser Beam Projector
            GameObject scannerObj = new GameObject("ScannerBeam");
            scannerObj.transform.SetParent(headObj.transform, false);
            scannerObj.transform.localPosition = new Vector3(0, -0.1f, 0.5f);
            scannerObj.transform.localRotation = Quaternion.Euler(35f, 0, 0);
            scannerBeamTransform = scannerObj.transform;

            scannerLight = scannerObj.AddComponent<Light>();
            scannerLight.type = LightType.Spot;
            scannerLight.color = new Color(0.3f, 0.85f, 1.0f);
            scannerLight.range = 12f;
            scannerLight.spotAngle = 50f;
            scannerLight.intensity = 4.0f;
            scannerLight.enabled = false;

            // 5. Quad Mechanical Legs / Wheels Stance
            Vector3[] legOffsets = new Vector3[]
            {
                new Vector3(-0.42f, 0.2f, 0.35f),   // Front Left
                new Vector3(0.42f, 0.2f, 0.35f),    // Front Right
                new Vector3(-0.42f, 0.2f, -0.35f),  // Back Left
                new Vector3(0.42f, 0.2f, -0.35f)   // Back Right
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject legPivot = new GameObject($"RobotLeg_{i}");
                legPivot.transform.SetParent(transform, false);
                legPivot.transform.localPosition = legOffsets[i];
                legOriginalPos[i] = legOffsets[i];
                legs[i] = legPivot.transform;

                // Leg Pod Pod Mesh
                GameObject legMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                legMesh.transform.SetParent(legPivot.transform, false);
                legMesh.transform.localPosition = new Vector3(0, -0.2f, 0);
                legMesh.transform.localScale = new Vector3(0.18f, 0.25f, 0.18f);
                legMesh.GetComponent<Renderer>().material = darkMetalMat;

                // Leg Wheel Foot
                GameObject footWheel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                footWheel.transform.SetParent(legPivot.transform, false);
                footWheel.transform.localPosition = new Vector3(0, -0.42f, 0);
                footWheel.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
                footWheel.GetComponent<Renderer>().material = bodyMat;
            }
        }

        private void BuildRobotNameplate()
        {
            nameplateCanvas = new GameObject("RobotNameplateCanvas");
            nameplateCanvas.transform.SetParent(transform, false);
            nameplateCanvas.transform.localPosition = new Vector3(0, 1.85f, 0);

            Canvas canvas = nameplateCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = nameplateCanvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, 100f);
            nameplateCanvas.transform.localScale = Vector3.one * 0.007f;

            GameObject textObj = new GameObject("NameplateText");
            textObj.transform.SetParent(nameplateCanvas.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            hudStatusText = textObj.AddComponent<Text>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            hudStatusText.font = font;
            hudStatusText.fontSize = 24;
            hudStatusText.alignment = TextAnchor.MiddleCenter;
            hudStatusText.fontStyle = FontStyle.Bold;
            hudStatusText.color = new Color(0.2f, 0.95f, 0.5f);
            hudStatusText.text = "[ASTRO-BOT-9]\nMODE: FOLLOWING";
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        private bool WasFPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            try { return Input.GetKeyDown(KeyCode.F); } catch { return false; }
#else
            return false;
#endif
        }

        private bool WasRPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            try { return Input.GetKeyDown(KeyCode.R); } catch { return false; }
#else
            return false;
#endif
        }

        private AudioClip CreateMetallicStepClip(float pitch)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * 0.05f);
            float[] samples = new float[sampleCount];

            System.Random rand = new System.Random();
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 110f);
                float metallicRing = Mathf.Sin(2f * Mathf.PI * 440f * pitch * t) * 0.4f + Mathf.Sin(2f * Mathf.PI * 880f * pitch * t) * 0.2f;
                float noise = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.2f;
                samples[i] = (metallicRing + noise) * envelope * 0.35f;
            }

            AudioClip clip = AudioClip.Create($"RobotStep_{pitch}", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void BuildSpeechBubbleUI()
        {
            speechCanvas = new GameObject("RobotSpeechCanvas");
            speechCanvas.transform.SetParent(transform, false);
            speechCanvas.transform.localPosition = new Vector3(0, 2.35f, 0);

            Canvas canvas = speechCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = speechCanvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(340f, 90f);
            speechCanvas.transform.localScale = Vector3.one * 0.007f;

            GameObject panel = new GameObject("SpeechPanel");
            panel.transform.SetParent(speechCanvas.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            speechBgImage = panel.AddComponent<Image>();
            speechBgImage.color = new Color(0.04f, 0.12f, 0.22f, 0.90f);

            GameObject textObj = new GameObject("SpeechText");
            textObj.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.05f);
            textRect.anchorMax = new Vector2(0.95f, 0.95f);
            textRect.sizeDelta = Vector2.zero;

            speechText = textObj.AddComponent<Text>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            speechText.font = font;
            speechText.fontSize = 20;
            speechText.alignment = TextAnchor.MiddleCenter;
            speechText.fontStyle = FontStyle.Bold;
            speechText.color = new Color(0.3f, 0.95f, 1.0f);
            speechText.text = "";

            speechCanvas.SetActive(false);
        }

        public void Speak(string textLine)
        {
            if (speechCoroutine != null) StopCoroutine(speechCoroutine);
            speechCoroutine = StartCoroutine(SpeakRoutine(textLine));
        }

        private IEnumerator SpeakRoutine(string textLine)
        {
            // Speak Spoken English voice out loud via Windows Native TTS
            SpeakEnglishNative(textLine);

            // Play accompanying sci-fi robot chime
            if (robotAudioSource != null)
            {
                AudioClip voiceClip = CreateRobotSpeechAudioClip(textLine.GetHashCode());
                robotAudioSource.PlayOneShot(voiceClip, 0.35f);
            }

            yield return null;
        }

        public static void SpeakEnglishNative(string textToSpeak)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        System.Type sapiType = System.Type.GetTypeFromProgID("SAPI.SpVoice");
                        if (sapiType != null)
                        {
                            object voice = System.Activator.CreateInstance(sapiType);
                            // Set speech rate to -1 (slow, clear spoken English for students)
                            sapiType.InvokeMember("Rate", System.Reflection.BindingFlags.SetProperty, null, voice, new object[] { -1 });
                            // Speak English text out loud asynchronously
                            sapiType.InvokeMember("Speak", System.Reflection.BindingFlags.InvokeMethod, null, voice, new object[] { textToSpeak, 1 });
                        }
                    }
                    catch
                    {
                        // Fall back to built-in procedural speech synthesizer
                    }
                });
            }
            catch { }
#endif
        }

        private AudioClip CreateRobotSpeechAudioClip(int phraseSeed)
        {
            int sampleRate = 44100;
            float duration = 0.70f; // Slower, clearer speech duration
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            System.Random rand = new System.Random(phraseSeed);
            int syllableCount = 5; // Distinct, well-spaced speech syllables
            float syllableLen = duration / syllableCount;

            for (int s = 0; s < syllableCount; s++)
            {
                int startSample = (int)(s * syllableLen * sampleRate);
                int endSample = (int)((s + 1) * syllableLen * sampleRate);
                float freq = 360f + (s % 3) * 90f;

                for (int i = startSample; i < endSample && i < sampleCount; i++)
                {
                    float localT = (float)(i - startSample) / sampleRate;
                    float progress = localT / syllableLen;

                    // Smooth syllable envelope with brief pause between syllables for clarity
                    float env = Mathf.Sin(progress * Mathf.PI) * (progress < 0.85f ? 1.0f : (1.0f - progress) * 5.0f);

                    float tone1 = Mathf.Sin(2f * Mathf.PI * freq * localT);
                    float tone2 = Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * localT) * 0.3f;
                    float chirp = (tone1 + tone2) * 0.5f;

                    samples[i] = chirp * Mathf.Max(0f, env) * 0.45f;
                }
            }

            AudioClip clip = AudioClip.Create($"RobotVoice_{phraseSeed}", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
